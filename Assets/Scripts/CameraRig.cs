using UnityEngine;

// 메인 카메라 연출. 평소엔 시작 위치·크기를 유지하고,
// 스테이션에 들어가거나 포탄을 들면 플레이어 쪽으로 확대, 대포 조준 중엔 조준선과 마차 오른쪽이 보이게 맞춘다.
// 충격(Shake)은 흔들림과 함께 타이밍 핀 흔들림 세기(ShakeAmount)로도 쓰인다.
[RequireComponent(typeof(Camera))]
public sealed class CameraRig : MonoBehaviour
{
    private const float SafeFraction = 0.9f;
    private const float CarryZoom = 3f;
    private const float StationZoom = 4f;
    private const float AimZoom = 2f;
    private const float GunnerCartViewportX = 0.1f;
    private const float AimEndPadding = 0.25f;
    // 웹판 v10 앞길 시야: 일반 주행·마부석은 말·마차 묶음 중심을 화면 너비 55%에 두고, 마부석은 가로·세로 15% 더 넓게 본다.
    private const float ConvoyScreenX = 0.55f;
    private const float DriverViewRange = 1.15f;

    [Tooltip("말·마차 묶음의 가로 중심(월드 x). 말 앞끝~마차 뒤끝의 가운데 (웹판 convoyCenterX)")]
    [SerializeField] private float convoyCenterX = -0.75f;
    [SerializeField] private PlayerTiming player;
    [SerializeField] private CartStations stations;
    [SerializeField] private CannonAim cannonAim;
    [SerializeField, Min(0.1f)] private float smoothness = 8f;

    private readonly Vector3[] uiCorners = new Vector3[4];
    [SerializeField] private NetworkPlayer secondSeat;
    [SerializeField] private bool secondary;
    [SerializeField] private bool matchWebFraming;
    private bool wasCellar;
    private Camera cam;
    private Vector3 homePosition;
    private float homeSize;
    private Vector3 lastShakeOffset;
    private float shakeRemaining;
    private float shakeDuration = 0.01f;
    private float shakeStrength;

    public void Bind(NetworkPlayer actor) { player=actor.Timing; cannonAim=actor.Cannon; }

    public Vector3 ShakeOffset => lastShakeOffset;
    public bool CameraActive => cam != null && cam.enabled;
    public float ShakeAmount => shakeRemaining > 0f ? shakeRemaining / shakeDuration : 0f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        homePosition = transform.position;
        homeSize = cam.orthographicSize;
    }

    public void Shake(float strength, float duration)
    {
        shakeStrength = Mathf.Max(shakeStrength, Mathf.Max(0f, strength));
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeRemaining = shakeDuration;
    }

    private static bool NeedsTaskView(NetworkPlayer actor)
    {
        if (actor.Timing.HasStationAccess && actor.Timing.IsAt(StationAction.Driver)) return false;
        return actor.Timing.HasStationAccess || actor.Carrying.Value || actor.Holding.Value || actor.Timing.IsCannonFocus;
    }
    private void LateUpdate()
    {
        if (NetworkPlayer.LocalPlayer == null) return;
        var primary = NetworkPlayer.LocalPlayer;
        bool split = LobbySession.LocalSeats == 2 && secondSeat != null && secondSeat.Assigned.Value && (primary.InCellar != secondSeat.InCellar || NeedsTaskView(primary) || NeedsTaskView(secondSeat));
        if (secondary) cam.enabled = split;
        if (secondary && !split) return;
        cam.rect = split ? new Rect(secondary ? 0.5f : 0,0,0.5f,1) : new Rect(0,0,1,1);
        var follow = secondary ? secondSeat : primary;
        if (player != follow.Timing) Bind(follow);
        float t = 1f - Mathf.Exp(-smoothness * Time.deltaTime);
        bool aiming = !player.IsAt(StationAction.Driver) && player.IsCannonFocus && cannonAim != null && cannonAim.Muzzle != null;
        bool carrying = follow.Carrying.Value || follow.Holding.Value;
        bool atOwnStation = player.HasStationAccess && !player.IsAt(StationAction.Driver);
        bool zooming = NeedsTaskView(follow);
        carrying &= zooming;
        bool driving = player.HasStationAccess && player.IsAt(StationAction.Driver);
        float targetSize = homeSize / (carrying ? CarryZoom : atOwnStation ? StationZoom : 1f) * (driving ? DriverViewRange : 1f);
        bool inCellar = player.GetComponent<NetworkPlayer>().InCellar;
        Vector3 destination = inCellar ? new Vector3(stations.Cart.transform.position.x, stations.Cart.transform.position.y + NetworkPlayer.CellarOffset, homePosition.z)
            : new Vector3(convoyCenterX - (ConvoyScreenX - 0.5f) * 2f * targetSize * cam.aspect, homePosition.y, homePosition.z);
        if (matchWebFraming)
            MatchWebFraming(follow, driving, carrying, atOwnStation, ref destination, ref targetSize);
        Bounds visible = new Bounds(player.transform.position, Vector3.zero);

        if (aiming)
        {
            Vector3 muzzle = cannonAim.Muzzle.position;
            visible = stations.PursuerEntryBounds;
            visible.Encapsulate(muzzle);
            visible.Encapsulate(cannonAim.LandingBounds);
            visible.Expand(new Vector3(2f * AimEndPadding, 2f * AimEndPadding, 0f));
            float rearX = stations.CartBody.bounds.max.x;
            float width = Mathf.Max((rearX - visible.min.x) / GunnerCartViewportX,
                (visible.max.x - rearX) / (1f - GunnerCartViewportX));
            float requiredSize = Mathf.Max(visible.extents.y / SafeFraction, width / (2f * cam.aspect));
            targetSize = Mathf.Max(matchWebFraming ? targetSize : homeSize / AimZoom, requiredSize);
            float halfWidth = targetSize * cam.aspect;
            destination = new Vector3(rearX + (1f - 2f * GunnerCartViewportX) * halfWidth,
                visible.center.y, homePosition.z);
        }
        else if (zooming)
        {
            IncludeRect(ref visible, player.ActiveTimingRect);
            destination = new Vector3(visible.center.x, visible.center.y, homePosition.z);
            targetSize = Mathf.Max(targetSize, FitSize(visible.extents.x, visible.extents.y));
        }

        if (wasCellar != follow.InCellar) { t = 1; wasCellar = follow.InCellar; }
        Vector3 basePosition = Vector3.Lerp(transform.position - lastShakeOffset, destination, t);
        lastShakeOffset = NextShakeOffset();
        transform.position = basePosition + lastShakeOffset;

        float size = Mathf.Lerp(cam.orthographicSize, targetSize, t);
        if (zooming && !aiming)
        {
            // 이동 중에도 현재 카메라 위치 기준으로 보여야 할 영역이 잘리지 않게 한다.
            Vector3 center = transform.position;
            float halfWidth = Mathf.Max(Mathf.Abs(visible.min.x - center.x), Mathf.Abs(visible.max.x - center.x));
            float halfHeight = Mathf.Max(Mathf.Abs(visible.min.y - center.y), Mathf.Abs(visible.max.y - center.y));
            size = Mathf.Max(size, FitSize(halfWidth, halfHeight));
        }
        cam.orthographicSize = size;
    }

    private void MatchWebFraming(NetworkPlayer follow, bool driving, bool carrying, bool atStation,
        ref Vector3 destination, ref float targetSize)
    {
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float width = Mathf.Clamp(550f + 300f * aspect, 650f, 1200f);
        float height = width / Mathf.Max(.01f, aspect);
        float paneWidth = width * cam.rect.width;
        float unit = stations.WorldPerWebUnit;
        float zoom = Mathf.Min(1f, height / 480f, paneWidth / 760f);
        if (follow.InCellar)
            zoom = atStation ? Mathf.Min(3.2f, paneWidth / 120f, height / 112f)
                : Mathf.Min(3f, paneWidth / 411f, height / 234f);
        else if (driving)
            zoom /= 1.5f;
        else if (carrying)
            zoom = 3f;
        else if (atStation)
            zoom = 4f;

        if (!follow.InCellar) zoom = Mathf.Max(zoom, paneWidth * ConvoyScreenX / 1450f);
        targetSize = height / (2f * zoom) * unit;
        if (!follow.InCellar)
            destination.x = stations.Cart.transform.position.x - (77f + (ConvoyScreenX - .5f) * paneWidth / zoom) * unit;
        destination.y -= .04f * targetSize;
    }

    private float FitSize(float halfWidth, float halfHeight)
    {
        float aspect = Mathf.Max(cam.aspect, 0.01f);
        return Mathf.Max(halfHeight / SafeFraction, halfWidth / (aspect * SafeFraction));
    }

    private Vector3 NextShakeOffset()
    {
        if (shakeRemaining <= 0f) return Vector3.zero;
        float intensity = shakeStrength * Mathf.Clamp01(shakeRemaining / shakeDuration);
        shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.unscaledDeltaTime);
        if (shakeRemaining == 0f) shakeStrength = 0f;
        return new Vector3(Random.Range(-intensity, intensity), Random.Range(-intensity, intensity), 0f);
    }

    private void IncludeRect(ref Bounds bounds, RectTransform rect)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy) return;
        rect.GetWorldCorners(uiCorners);
        foreach (Vector3 corner in uiCorners) bounds.Encapsulate(corner - lastShakeOffset);
    }
}

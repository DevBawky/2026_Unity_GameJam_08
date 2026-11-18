using UnityEngine;

// 메인 카메라 연출. 평소엔 시작 위치·크기를 유지하고,
// 스테이션에 들어가거나 포탄을 들면 플레이어 쪽으로 확대, 대포 조준 중엔 조준선과 마차 오른쪽이 보이게 맞춘다.
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
    private const float TransitionSeconds = .5f;

    [Tooltip("말·마차 묶음의 가로 중심(월드 x). 말 앞끝~마차 뒤끝의 가운데 (웹판 convoyCenterX)")]
    [SerializeField] private float convoyCenterX = -0.75f;
    [SerializeField] private PlayerTiming player;
    [SerializeField] private CartStations stations;
    [SerializeField] private CannonAim cannonAim;

    private readonly Vector3[] uiCorners = new Vector3[4];
    [SerializeField] private NetworkPlayer secondSeat;
    [SerializeField] private bool secondary;
    [SerializeField] private bool matchWebFraming;
    private (bool Split, bool Cellar, bool Carrying, StationAction? Station, bool Aiming) view;
    private Vector3 transitionPosition, shotPosition;
    private float transitionSize, transitionAge, shotSize;
    private bool wasPostShot;
    private Camera cam;
    private int defaultLayers, cellarBackdropLayer;
    private Vector3 homePosition;
    private float homeSize;
    private Vector3 lastShakeOffset;
    private float shakeRemaining;
    private float shakeDuration = 0.01f;
    private float shakeStrength;

    public void Bind(NetworkPlayer actor) { player=actor.Timing; cannonAim=actor.Cannon; }

    public Vector3 ShakeOffset => lastShakeOffset;
    public bool CameraActive => cam != null && cam.enabled;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        defaultLayers = cam.cullingMask;
        cellarBackdropLayer = LayerMask.GetMask("CellarBackdrop");
        homePosition = transform.position;
        homeSize = cam.orthographicSize;
        transitionPosition = homePosition;
        transitionSize = homeSize;
        transitionAge = TransitionSeconds;
    }

    public void Shake(float strength, float duration)
    {
        shakeStrength = Mathf.Max(shakeStrength, Mathf.Max(0f, strength));
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeRemaining = shakeDuration;
    }

    private bool NeedsTaskView(NetworkPlayer actor)
    {
        if (actor.InCellar && (actor.Carrying.Value || actor.Holding.Value) && !actor.Timing.HasStationAccess) return false;
        if (!matchWebFraming && actor.Timing.HasStationAccess && actor.Timing.IsAt(StationAction.Driver)) return false;
        return actor.Timing.HasStationAccess || actor.Carrying.Value || actor.Holding.Value || actor.Timing.IsCannonFocus;
    }
    private void LateUpdate()
    {
        if (NetworkPlayer.LocalPlayer == null) return;
        var primary = NetworkPlayer.LocalPlayer;
        bool split = LobbySession.LocalSeats == 2 && secondSeat != null && secondSeat.Assigned.Value
            && (primary.InCellar != secondSeat.InCellar || NeedsTaskView(primary) || NeedsTaskView(secondSeat)
                || stations.IsPostShotGunner(primary) || stations.IsPostShotGunner(secondSeat));
        if (secondary) cam.enabled = split;
        if (secondary && !split) return;
        cam.rect = split ? new Rect(secondary ? 0.5f : 0,0,0.5f,1) : new Rect(0,0,1,1);
        var follow = secondary ? secondSeat : primary;
        int visibleLayers = follow.InCellar ? defaultLayers | cellarBackdropLayer : defaultLayers & ~cellarBackdropLayer;
        if (cam.cullingMask != visibleLayers) cam.cullingMask = visibleLayers;
        if (player != follow.Timing) Bind(follow);
        Vector3 currentPosition = transform.position - lastShakeOffset;
        bool postShot = stations.IsPostShotGunner(follow);
        if (postShot && !wasPostShot)
        {
            shotPosition = currentPosition;
            shotSize = cam.orthographicSize;
        }
        wasPostShot = postShot;
        float shotReturn = postShot ? Mathf.Clamp01(1f - (float)(stations.PostShotEndsAt.Value
            - follow.NetworkManager.ServerTime.Time) / CartStations.PostShotReturnSeconds) : 0f;
        if (postShot && shotReturn <= 0f)
        {
            ApplyView(shotPosition, shotSize);
            return;
        }
        StationAction? station = player.HasStationAccess ? player.CurrentAction : null;
        bool aiming = !postShot && player.IsCannonFocus && cannonAim != null && cannonAim.Muzzle != null;
        bool driving = station == StationAction.Driver;
        bool carrying = !postShot && !driving && (follow.Carrying.Value || follow.Holding.Value);
        bool atOwnStation = station.HasValue && !driving;
        bool zooming = carrying || atOwnStation;
        float targetSize = homeSize / (carrying ? CarryZoom : atOwnStation ? StationZoom : 1f) * (driving ? DriverViewRange : 1f);
        bool inCellar = follow.InCellar;
        Vector3 destination = inCellar ? new Vector3(stations.Cart.transform.position.x, stations.Cart.transform.position.y + NetworkPlayer.CellarOffset, homePosition.z)
            : new Vector3(convoyCenterX - (ConvoyScreenX - 0.5f) * 2f * targetSize * cam.aspect, homePosition.y, homePosition.z);
        Bounds visible = new Bounds(player.transform.position, Vector3.zero);

        if (matchWebFraming)
            MatchWebFraming(follow, driving, carrying, atOwnStation, aiming, ref destination, ref targetSize);
        else if (aiming)
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
            targetSize = Mathf.Max(homeSize / AimZoom, requiredSize);
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

        var nextView = (split, inCellar, carrying, station, aiming);
        if (postShot)
        {
            view = nextView;
            transitionAge = TransitionSeconds;
            float eased = Mathf.SmoothStep(0f, 1f, shotReturn);
            ApplyView(Vector3.Lerp(shotPosition, destination, eased), Mathf.Lerp(shotSize, targetSize, eased));
            return;
        }
        if (view != nextView)
        {
            view = nextView;
            transitionPosition = currentPosition;
            transitionSize = cam.orthographicSize;
            transitionAge = 0f;
        }
        transitionAge += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, transitionAge / TransitionSeconds);
        ApplyView(Vector3.Lerp(transitionPosition, destination, t), Mathf.Lerp(transitionSize, targetSize, t));
    }

    private void ApplyView(Vector3 position, float size)
    {
        lastShakeOffset = NextShakeOffset();
        transform.position = position + lastShakeOffset;
        cam.orthographicSize = size;
    }

    private void MatchWebFraming(NetworkPlayer follow, bool driving, bool carrying, bool atStation,
        bool aiming, ref Vector3 destination, ref float targetSize)
    {
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float width = Mathf.Clamp(550f + 300f * aspect, 650f, 1200f);
        float height = width / Mathf.Max(.01f, aspect);
        float paneWidth = width * cam.rect.width;
        float unit = stations.WorldPerWebUnit;
        float zoom = Mathf.Min(1f, height / 480f, paneWidth / 760f);
        float centerX = stations.CartBody.bounds.center.x;
        float centerY = homePosition.y;
        if (follow.InCellar)
        {
            zoom = atStation ? Mathf.Min(3.2f, paneWidth / 120f, height / 112f)
                : Mathf.Min(3f, paneWidth / 411f, height / 234f);
            centerX = atStation ? follow.transform.position.x : centerX;
            centerY = atStation ? follow.transform.position.y : stations.Cart.transform.position.y + NetworkPlayer.CellarOffset;
        }
        else if (aiming)
        {
            const float viewRight = 1660f;
            const float gunnerWidth = viewRight - (641f - 65f);
            zoom = Mathf.Min(2f, paneWidth / gunnerWidth, height / 492f);
            centerX += (viewRight - 528.5f - paneWidth / (2f * zoom)) * unit;
        }
        else if (carrying)
        {
            var actor = follow.transform.position;
            var rack = follow.Timing.AreaFor(StationAction.ShellDeposit).position;
            zoom = follow.Holding.Value ? 3f : Mathf.Min(3f,
                paneWidth / (Mathf.Abs(actor.x - rack.x) / unit + 112f),
                height / (Mathf.Abs(actor.y - rack.y) / unit + 84f));
            var midpoint = follow.Holding.Value ? actor : (actor + rack) * .5f;
            centerX = midpoint.x;
            centerY = midpoint.y;
        }
        else if (atStation)
        {
            zoom = 4f;
            centerX = follow.transform.position.x;
            centerY = follow.transform.position.y;
        }
        else
        {
            if (driving) zoom /= 1.5f;
            zoom = Mathf.Max(zoom, paneWidth * ConvoyScreenX / 1450f);
            centerX -= (78.5f + (ConvoyScreenX - .5f) * paneWidth / zoom) * unit;
        }
        targetSize = height / (2f * zoom) * unit;
        destination = new Vector3(centerX, centerY - (aiming ? 0f : .04f * targetSize), homePosition.z);
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

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
    private const float CartVisibleFraction = 0.2f;
    private const float AimEndPadding = 0.25f;

    [SerializeField] private PlayerTiming player;
    [SerializeField] private CartStations stations;
    [SerializeField] private CannonAim cannonAim;
    [SerializeField, Min(0.1f)] private float smoothness = 8f;

    private readonly Vector3[] uiCorners = new Vector3[4];
    private Camera cam;
    private Vector3 homePosition;
    private float homeSize;
    private Vector3 lastShakeOffset;
    private float shakeRemaining;
    private float shakeDuration = 0.01f;
    private float shakeStrength;

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

    private void LateUpdate()
    {
        float t = 1f - Mathf.Exp(-smoothness * Time.deltaTime);
        bool aiming = player.IsCannonFocus && cannonAim != null && cannonAim.Muzzle != null;
        bool carrying = stations.IsCarrying || stations.IsHoldingShell;
        bool zooming = aiming || player.IsInside || carrying;
        float targetSize = homeSize / (carrying ? CarryZoom : player.IsInside ? StationZoom : 1f);
        Vector3 destination = homePosition;
        Bounds visible = new Bounds(player.transform.position, Vector3.zero);

        if (aiming)
        {
            Vector3 muzzle = cannonAim.Muzzle.position;
            Vector3 endpoint = muzzle + (Vector3)cannonAim.Direction * cannonAim.LineLength;
            Vector3 padding = new Vector3(AimEndPadding, AimEndPadding);
            visible = new Bounds(muzzle, Vector3.zero);
            visible.Encapsulate(endpoint + padding);
            visible.Encapsulate(endpoint - padding);
            SpriteRenderer body = stations.CartBody;
            if (body != null && body.enabled && body.gameObject.activeInHierarchy)
            {
                Bounds cartBounds = body.bounds;
                float cartVisibleLeft = Mathf.Lerp(cartBounds.max.x, cartBounds.min.x, CartVisibleFraction);
                visible.Encapsulate(new Vector3(cartVisibleLeft, cartBounds.min.y, muzzle.z));
                visible.Encapsulate(new Vector3(cartBounds.max.x, cartBounds.max.y, muzzle.z));
            }
            targetSize = homeSize / AimZoom;
        }

        if (zooming)
        {
            IncludeRect(ref visible, player.ActiveTimingRect);
            if (player.IsAt(StationAction.RoyalFamily))
            {
                IncludeRect(ref visible, stations.PrinceAnxietyUI);
                IncludeRect(ref visible, stations.PrincessAnxietyUI);
            }
            destination = new Vector3(visible.center.x, visible.center.y, homePosition.z);
            targetSize = Mathf.Max(targetSize, FitSize(visible.extents.x, visible.extents.y));
        }

        Vector3 basePosition = Vector3.Lerp(transform.position - lastShakeOffset, destination, t);
        lastShakeOffset = NextShakeOffset();
        transform.position = basePosition + lastShakeOffset;

        float size = Mathf.Lerp(cam.orthographicSize, targetSize, t);
        if (zooming)
        {
            // 이동 중에도 현재 카메라 위치 기준으로 보여야 할 영역이 잘리지 않게 한다.
            Vector3 center = transform.position;
            float halfWidth = Mathf.Max(Mathf.Abs(visible.min.x - center.x), Mathf.Abs(visible.max.x - center.x));
            float halfHeight = Mathf.Max(Mathf.Abs(visible.min.y - center.y), Mathf.Abs(visible.max.y - center.y));
            size = Mathf.Max(size, FitSize(halfWidth, halfHeight));
        }
        cam.orthographicSize = size;
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
        foreach (Vector3 corner in uiCorners) bounds.Encapsulate(corner);
    }
}

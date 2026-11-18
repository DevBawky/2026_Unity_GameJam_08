using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerTiming : MonoBehaviour
{
    [SerializeField] private RectTransform interactionAreas;
    [SerializeField] private GameObject timingPanel;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform target;
    [SerializeField] private RectTransform pin;
    [SerializeField] private CircularTimingGraphic circularGraphic;
    [SerializeField] private Camera gameCamera;
    [SerializeField, Min(0.01f)] private float pinSpeed = 3f;
    [SerializeField, Min(0.1f)] private float zoomSize = 2f;
    [SerializeField, Min(0.1f)] private float cameraSmoothness = 8f;

    private CartStations stations;
    private CannonAim cannonAim;
    public bool IsCannonFocus => (inside && currentSettings != null && currentSettings.Action == StationAction.Cannon) || (stations != null && stations.IsFiring);
    public bool CanInteract => inside && currentSettings != null && currentSettings.Action != StationAction.ShellDeposit;
    private bool actionRequested;
    private bool inside;
    private RectTransform currentArea;
    private TimingArea currentSettings;
    private float circularTargetCenter;
    private float circularPinAngle;
    private float pinDirection = 1f;
    private Vector3 originalCameraPosition;
    private float originalCameraSize;
    private float originalFieldOfView;

    private void Awake()
    {
        stations = GetComponent<CartStations>();
        cannonAim = GetComponent<CannonAim>();
        if (gameCamera == null) gameCamera = Camera.main;
        if (gameCamera != null)
        {
            originalCameraPosition = gameCamera.transform.position;
            originalCameraSize = gameCamera.orthographicSize;
            originalFieldOfView = gameCamera.fieldOfView;
        }
        if (timingPanel != null) timingPanel.SetActive(false);
    }

    private void Update()
    {
        RectTransform area = FindArea();
        if (area != currentArea)
        {
            currentArea = area;
            currentSettings = area != null ? area.GetComponent<TimingArea>() : null;
            inside = area != null;
            bool usesTiming = inside && (currentSettings == null || currentSettings.UsesTiming);
            if (timingPanel != null) timingPanel.SetActive(usesTiming);
            if (inside && currentSettings != null && stations != null) stations.Enter(currentSettings.Action);
            if (usesTiming) ResetTiming();
        }

        bool pressed = actionRequested || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);
        actionRequested = false;
        if (!inside) return;
        if (currentSettings != null && !currentSettings.UsesTiming)
        {
            if (currentSettings.Action == StationAction.Cannon && pressed && stations != null) stations.Ignite();
            return;
        }
        if (background == null || target == null || pin == null) return;
        bool circular = currentSettings != null && currentSettings.Pattern == TimingPattern.Circular;
        if (circular)
        {
            if (circularGraphic == null) return;
            circularPinAngle = Mathf.Repeat(circularPinAngle - currentSettings.CircularSpeed * Time.deltaTime, 360f);
            circularGraphic.SetAngles(circularTargetCenter, currentSettings.CircularTargetAngle, circularPinAngle);
        }
        else MovePin();

        if (pressed)
        {
            bool success;
            if (circular)
                success = Mathf.Abs(Mathf.DeltaAngle(circularPinAngle, circularTargetCenter)) <= currentSettings.CircularTargetAngle * 0.5f;
            else
            {
                float pinCenter = pin.localPosition.x + pin.rect.center.x;
                float targetCenter = target.localPosition.x + target.rect.center.x;
                success = Mathf.Abs(pinCenter - targetCenter) <= target.rect.width * 0.5f;
            }
            Debug.Log(success ? "성공" : "실패", this);
            if (success && currentSettings != null && stations != null) stations.Succeed(currentSettings.Action);
        }
    }

    public void TriggerInteraction() { if (CanInteract) actionRequested = true; }

    private RectTransform FindArea()
    {
        if (interactionAreas == null) return null;
        foreach (Transform child in interactionAreas)
        {
            if (!child.gameObject.activeInHierarchy || !(child is RectTransform area)) continue;
            Vector3 local = area.InverseTransformPoint(transform.position);
            if (area.rect.Contains(new Vector2(local.x, local.y))) return area;
        }
        return null;
    }

    private void ResetTiming()
    {
        if (background == null || target == null || pin == null) return;
        bool circular = currentSettings != null && currentSettings.Pattern == TimingPattern.Circular;
        background.gameObject.SetActive(!circular);
        if (circularGraphic != null) circularGraphic.gameObject.SetActive(circular);
        if (circular)
        {
            if (circularGraphic == null) return;
            circularGraphic.gameObject.SetActive(true);
            circularTargetCenter = Random.Range(0f, 360f);
            circularPinAngle = 180f;
            circularGraphic.SetAngles(circularTargetCenter, currentSettings.CircularTargetAngle, circularPinAngle);
            return;
        }
        float halfTarget = target.rect.width * 0.5f;
        float min = background.rect.xMin + halfTarget;
        float max = background.rect.xMax - halfTarget;
        SetCenterX(target, min <= max ? Random.Range(min, max) : background.rect.center.x);
        SetCenterX(pin, background.rect.xMin + pin.rect.width * 0.5f);
        pinDirection = 1f;
    }

    private void MovePin()
    {
        float min = background.rect.xMin + pin.rect.width * 0.5f;
        float max = background.rect.xMax - pin.rect.width * 0.5f;
        if (max <= min) { SetCenterX(pin, background.rect.center.x); return; }
        float center = pin.localPosition.x + pin.rect.center.x;
        float distance = Mathf.Repeat((pinDirection > 0 ? center - min : 2f * (max - min) - (center - min)) + pinSpeed * Time.deltaTime, 2f * (max - min));
        pinDirection = distance < max - min ? 1f : -1f;
        SetCenterX(pin, min + (distance < max - min ? distance : 2f * (max - min) - distance));
    }

    private static void SetCenterX(RectTransform rect, float x)
    {
        Vector3 position = rect.localPosition;
        position.x = x - rect.rect.center.x;
        rect.localPosition = position;
    }

    private void LateUpdate()
    {
        if (gameCamera == null) return;
        float t = 1f - Mathf.Exp(-cameraSmoothness * Time.deltaTime);
        bool aiming = IsCannonFocus && cannonAim != null && cannonAim.Muzzle != null;
        float targetSize = aiming ? originalCameraSize : inside ? zoomSize : originalCameraSize;
        Vector3 destination = inside ? new Vector3(transform.position.x, transform.position.y, originalCameraPosition.z) : originalCameraPosition;
        if (aiming)
        {
            Vector3 muzzle = cannonAim.Muzzle.position;
            Vector2 aimOffset = cannonAim.Direction * cannonAim.LineLength;
            // Keep the whole line visible; a horizontal aim leaves 20% of the screen beyond its end.
            targetSize = Mathf.Max(aimOffset.x / (1.28f * gameCamera.aspect), Mathf.Abs(aimOffset.y) / 1.6f, 1.5f);
            destination = new Vector3(muzzle.x + targetSize * gameCamera.aspect * 0.68f, muzzle.y + aimOffset.y * 0.5f, originalCameraPosition.z);
        }
        gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, destination, t);
        if (gameCamera.orthographic)
            gameCamera.orthographicSize = Mathf.Lerp(gameCamera.orthographicSize, targetSize, t);
        else
            gameCamera.fieldOfView = Mathf.Lerp(gameCamera.fieldOfView, inside ? Mathf.Min(originalFieldOfView, 30f) : originalFieldOfView, t);
    }
}

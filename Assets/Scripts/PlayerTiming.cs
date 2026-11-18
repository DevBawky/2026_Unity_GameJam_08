using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 상호작용. 상호작용 영역(TimingArea) 안에 들어가면 타이밍 미니게임을 띄우고 성공·실패를 CartStations에 알린다.
// 포탄을 들고 충격을 받으면 버티기 판정(원형 타이밍)을 띄운다.
// 카메라 연출은 CameraRig, 화면 문구·경고 패널은 StageHud가 맡는다.
public sealed class PlayerTiming : MonoBehaviour
{
    private const float HoldPinSpeed = 180f;
    private const float HoldTargetAngle = 60f;
    private const float PinStartAngle = 180f;

    [SerializeField] private RectTransform interactionAreas;
    [SerializeField] private RectTransform repairRect;
    [SerializeField] private GameObject timingPanel;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform target;
    [SerializeField] private RectTransform pin;
    [SerializeField] private CircularTimingGraphic circularGraphic;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField, Min(0.01f)] private float pinSpeed = 3f;
    [Tooltip("충격 직후 타이밍 핀이 흔들리는 최대 각도")]
    [SerializeField, Min(0f)] private float timingJitterDegrees = 10f;

    private CartStations stations;
    private RectTransform currentArea;
    private TimingArea currentSettings;
    private bool inside;
    private bool actionRequested;
    private bool wasHoldingShell;
    private float circularTargetCenter;
    private float circularPinAngle;
    private float pinDirection = 1f;

    public bool IsCannonFocus => IsAt(StationAction.Cannon) || stations.IsFiring;
    public bool CanInteract => stations.IsHoldingShell
        || (inside && currentSettings != null && currentSettings.Action != StationAction.ShellDeposit && stations.CanUse(currentSettings.Action));
    public bool IsInside => inside;
    public bool IsAt(StationAction action) => inside && currentSettings != null && currentSettings.Action == action;
    public StationAction? CurrentAction => inside && currentSettings != null ? currentSettings.Action : null;
    public RectTransform ActiveTimingRect => timingPanel == null || !timingPanel.activeInHierarchy ? null
        : circularGraphic != null && circularGraphic.gameObject.activeInHierarchy ? circularGraphic.rectTransform : background;

    private void Awake()
    {
        stations = GetComponent<CartStations>();
        if (timingPanel != null) timingPanel.SetActive(false);
    }

    public void TriggerInteraction()
    {
        if (CanInteract) actionRequested = true;
    }

    public void SetWheelRepairActive(bool active)
    {
        if (repairRect != null) repairRect.gameObject.SetActive(active);
    }

    private void Update()
    {
        UpdateCurrentArea();
        if (stations.Failed || stations.ChoosingUpgrade)
        {
            actionRequested = false;
            return;
        }

        bool holdingShell = stations.IsHoldingShell;
        if (holdingShell != wasHoldingShell)
        {
            wasHoldingShell = holdingShell;
            if (timingPanel != null) timingPanel.SetActive(holdingShell || (inside && currentSettings != null && currentSettings.UsesTiming));
            if (holdingShell) ResetHoldTiming();
            else if (inside && currentSettings != null && currentSettings.UsesTiming) ResetTiming();
        }
        if (!holdingShell && IsAt(StationAction.ShellDeposit)) stations.Enter(StationAction.ShellDeposit);
        if (timingPanel != null && !holdingShell)
            timingPanel.SetActive(inside && currentSettings != null && currentSettings.UsesTiming && stations.CanUse(currentSettings.Action));

        bool pressed = ConsumePress();
        if (holdingShell)
        {
            UpdateHoldTiming(pressed);
            return;
        }
        if (!inside) return;
        if (currentSettings != null && !stations.CanUse(currentSettings.Action)) return;
        if (currentSettings != null && !currentSettings.UsesTiming)
        {
            if (currentSettings.Action == StationAction.Cannon && pressed) stations.Ignite();
            return;
        }
        UpdateStationTiming(pressed);
    }

    private void UpdateCurrentArea()
    {
        RectTransform area = FindArea();
        if (area == currentArea && !(inside && area == null)) return;
        currentArea = area;
        currentSettings = area != null ? area.GetComponent<TimingArea>() : null;
        inside = area != null;
        bool usesTiming = inside && (currentSettings == null || currentSettings.UsesTiming);
        if (timingPanel != null) timingPanel.SetActive(usesTiming);
        if (inside && currentSettings != null) stations.Enter(currentSettings.Action);
        if (usesTiming) ResetTiming();
    }

    private bool ConsumePress()
    {
        Gamepad pad = Gamepad.current;
        bool pressed = actionRequested
            || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame));
        actionRequested = false;
        return pressed;
    }

    private void UpdateHoldTiming(bool pressed)
    {
        if (circularGraphic == null) return;
        circularPinAngle = Mathf.Repeat(circularPinAngle - HoldPinSpeed * Time.deltaTime, 360f);
        float visiblePin = ShakenPinAngle();
        float holdAngle = Mathf.Min(360f, HoldTargetAngle + stations.TimingBonusDegrees);
        circularGraphic.SetAngles(circularTargetCenter, holdAngle, visiblePin);
        if (pressed) stations.ResolveShellHold(Mathf.Abs(Mathf.DeltaAngle(visiblePin, circularTargetCenter)) <= holdAngle * 0.5f);
    }

    private void UpdateStationTiming(bool pressed)
    {
        if (background == null || target == null || pin == null) return;
        bool circular = currentSettings != null && currentSettings.UsesTiming;
        float displayedPin = circularPinAngle;
        if (circular)
        {
            if (circularGraphic == null) return;
            circularPinAngle = Mathf.Repeat(circularPinAngle - currentSettings.CircularSpeed * Time.deltaTime, 360f);
            displayedPin = ShakenPinAngle();
            circularGraphic.SetAngles(circularTargetCenter, CurrentTargetAngle(), displayedPin);
        }
        else MovePin();

        if (!pressed) return;
        bool success;
        if (circular)
            success = Mathf.Abs(Mathf.DeltaAngle(displayedPin, circularTargetCenter)) <= CurrentTargetAngle() * 0.5f;
        else
        {
            float pinCenter = pin.localPosition.x + pin.rect.center.x;
            float targetCenter = target.localPosition.x + target.rect.center.x;
            success = Mathf.Abs(pinCenter - targetCenter) <= target.rect.width * 0.5f;
        }
        if (currentSettings == null) return;
        if (success) stations.Succeed(currentSettings.Action);
        else stations.Fail(currentSettings.Action);
    }

    private RectTransform FindArea()
    {
        if (interactionAreas == null) return null;
        if (repairRect != null && stations.IsInsideDetachedTire(transform.position)) return repairRect;
        foreach (Transform child in interactionAreas)
        {
            if (!child.gameObject.activeInHierarchy || !(child is RectTransform area) || area == repairRect) continue;
            Vector3 local = area.InverseTransformPoint(transform.position);
            if (area.rect.Contains(new Vector2(local.x, local.y))) return area;
        }
        return null;
    }

    private float CurrentTargetAngle() => Mathf.Min(360f, currentSettings.CircularTargetAngle + stations.TimingBonusDegrees);

    private float ShakenPinAngle() => circularPinAngle + Random.Range(-timingJitterDegrees, timingJitterDegrees) * cameraRig.ShakeAmount;

    private void ResetHoldTiming()
    {
        if (background != null) background.gameObject.SetActive(false);
        if (circularGraphic == null) return;
        circularGraphic.gameObject.SetActive(true);
        circularTargetCenter = Random.Range(0f, 360f);
        circularPinAngle = PinStartAngle;
        circularGraphic.SetAngles(circularTargetCenter, HoldTargetAngle, circularPinAngle);
    }

    private void ResetTiming()
    {
        if (background == null || target == null || pin == null) return;
        bool circular = currentSettings != null && currentSettings.UsesTiming;
        background.gameObject.SetActive(!circular);
        if (circularGraphic != null) circularGraphic.gameObject.SetActive(circular);
        if (circular)
        {
            if (circularGraphic == null) return;
            circularTargetCenter = Random.Range(0f, 360f);
            circularPinAngle = PinStartAngle;
            circularGraphic.SetAngles(circularTargetCenter, CurrentTargetAngle(), circularPinAngle);
            return;
        }
        float halfTarget = target.rect.width * 0.5f;
        float min = background.rect.xMin + halfTarget;
        float max = background.rect.xMax - halfTarget;
        SetCenterX(target, min <= max ? Random.Range(min, max) : background.rect.center.x);
        SetCenterX(pin, background.rect.xMin + pin.rect.width * 0.5f);
        pinDirection = 1f;
    }

    // 바 타이밍: 핀이 양 끝을 왕복한다.
    private void MovePin()
    {
        float min = background.rect.xMin + pin.rect.width * 0.5f;
        float max = background.rect.xMax - pin.rect.width * 0.5f;
        if (max <= min)
        {
            SetCenterX(pin, background.rect.center.x);
            return;
        }
        float center = pin.localPosition.x + pin.rect.center.x;
        float span = max - min;
        float distance = Mathf.Repeat((pinDirection > 0 ? center - min : 2f * span - (center - min)) + pinSpeed * Time.deltaTime, 2f * span);
        pinDirection = distance < span ? 1f : -1f;
        SetCenterX(pin, min + (distance < span ? distance : 2f * span - distance));
    }

    private static void SetCenterX(RectTransform rect, float x)
    {
        Vector3 position = rect.localPosition;
        position.x = x - rect.rect.center.x;
        rect.localPosition = position;
    }
}

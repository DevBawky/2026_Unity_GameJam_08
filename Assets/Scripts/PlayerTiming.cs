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

    [SerializeField] private CartStations stations;
    private NetworkPlayer actor;
    private RectTransform currentArea;
    private TimingArea currentSettings;
    private bool inside;
    private bool actionRequested;
    private int lastReservationArea = -2;
    private bool wasHoldingShell;
    private float circularTargetCenter;
    private float circularPinAngle;
    private float pinDirection = 1f;

    public bool IsCannonFocus => (IsAt(StationAction.Cannon) && stations.HoldsStation(StationAction.Cannon,actor)) || (stations.IsFiring && stations.Gunner.Value==actor.OwnerClientId);
    public bool CanInteract => actor.Holding.Value
        || (inside && currentSettings != null && currentSettings.Action != StationAction.ShellDeposit && stations.CanUse(currentSettings.Action,actor));
    public bool IsInside => inside;
    public bool HasStationAccess => inside && currentSettings != null && stations.HoldsStation(currentSettings.Action,actor);
    public bool IsAt(StationAction action) => inside && currentSettings != null && currentSettings.Action == action;
    public StationAction? CurrentAction => inside && currentSettings != null ? currentSettings.Action : null;
    public RectTransform ActiveTimingRect => timingPanel == null || !timingPanel.activeInHierarchy ? null
        : circularGraphic != null && circularGraphic.gameObject.activeInHierarchy ? circularGraphic.rectTransform : background;

    private void Awake()
    {
        actor = GetComponent<NetworkPlayer>();
        if (timingPanel != null) timingPanel.SetActive(false);
    }

    public void TriggerInteraction()
    {
        if (actor != null && actor.Local && CanInteract) actionRequested = true;
    }

    public void SetWheelRepairActive(bool active)
    {
        if (repairRect != null) repairRect.gameObject.SetActive(active);
    }

    private void Update()
    {
        if (actor == null || !actor.IsSpawned || !actor.Assigned.Value) { timingPanel.SetActive(false); return; }
        if (!actor.IsOwner) { ApplyRemote(); return; }
        SetWheelRepairActive(stations.WheelDetached);
        UpdateCurrentArea();
        int requested = inside && currentSettings != null ? (int)currentSettings.Action : -1;
        if (requested != lastReservationArea || (requested >= 0 && !stations.HoldsStation((StationAction)requested,actor)))
            actor.RequestStation(requested);
        lastReservationArea = requested;
        if (!CartStations.Running)
        {
            actionRequested = false;
            timingPanel.SetActive(false);
            return;
        }

        bool holdingShell = actor.Holding.Value;
        if (holdingShell != wasHoldingShell)
        {
            wasHoldingShell = holdingShell;
            if (timingPanel != null) timingPanel.SetActive(holdingShell || (inside && currentSettings != null && currentSettings.UsesTiming));
            if (holdingShell) ResetHoldTiming();
            else if (inside && currentSettings != null && currentSettings.UsesTiming) ResetTiming();
        }
        if (!holdingShell && actor.Carrying.Value && IsAt(StationAction.ShellDeposit)
            && stations.CanUse(StationAction.ShellDeposit,actor)) actor.Request(StationAction.ShellDeposit,true);
        if (timingPanel != null && !holdingShell)
            timingPanel.SetActive(inside && currentSettings != null && currentSettings.UsesTiming && stations.CanUse(currentSettings.Action,actor));

        bool pressed = ConsumePress();
        if (holdingShell)
        {
            UpdateHoldTiming(pressed);
            return;
        }
        if (!inside) return;
        if (currentSettings != null && !stations.CanUse(currentSettings.Action,actor)) return;
        if (currentSettings != null && !currentSettings.UsesTiming)
        {
            if (currentSettings.Action == StationAction.Cannon && pressed) actor.Request(StationAction.Cannon,true);
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
        if (inside && currentSettings != null && currentSettings.Action==StationAction.ShellDeposit) actor.Request(currentSettings.Action,true);
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
        if (pressed) actor.Request(StationAction.ShellPile,Mathf.Abs(Mathf.DeltaAngle(visiblePin, circularTargetCenter)) <= holdAngle * 0.5f,true);
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
        actor.Request(currentSettings.Action,success);
    }

    private RectTransform FindArea()
    {
        if (interactionAreas == null) return null;
        if (repairRect != null && stations.IsInsideDetachedTire(transform.position)) return repairRect;
        for (int i = 0; i < interactionAreas.childCount; i++)
        {
            Transform child = interactionAreas.GetChild(i);
            if (!child.gameObject.activeInHierarchy || !(child is RectTransform area) || area == repairRect) continue;
            Vector3 local = area.InverseTransformPoint(transform.position);
            if (area.rect.Contains(new Vector2(local.x, local.y))) return area;
        }
        return null;
    }

    public bool IsInActionArea(StationAction action)
    {
        if (action==StationAction.WheelRepair) return stations.IsInsideDetachedTire(transform.position);
        var area=FindArea();
        return area != null && area.GetComponent<TimingArea>().Action==action;
    }
    private void LateUpdate()
    {
        if (actor==null || !actor.Local) return;
        bool visible=CartStations.Running && timingPanel.activeSelf;
        int action=actor.Holding.Value ? -2 : currentSettings != null ? (int)currentSettings.Action : -1;
        float speed=actor.Holding.Value ? HoldPinSpeed : currentSettings != null ? currentSettings.CircularSpeed : 0f;
        float width=actor.Holding.Value ? Mathf.Min(360f,HoldTargetAngle+stations.TimingBonusDegrees) : currentSettings != null ? CurrentTargetAngle() : 0f;
        actor.PublishTask(action,visible,circularTargetCenter,width,circularPinAngle,speed);
    }
    private void ApplyRemote()
    {
        var task=actor.TaskState.Value;
        bool ownsTask = task.Action == -2 ? actor.Holding.Value
            : task.Action >= 0 && stations.HoldsStation((StationAction)task.Action,actor);
        bool show=task.Visible && CartStations.Running && ownsTask;
        timingPanel.SetActive(show);
        if (!show || circularGraphic==null) return;
        background.gameObject.SetActive(false);
        circularGraphic.gameObject.SetActive(true);
        float angle=Mathf.Repeat(task.Angle-(float)(actor.NetworkManager.ServerTime.Time-task.Time)*task.Speed,360f);
        circularGraphic.SetAngles(task.Target,task.Width,angle);
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

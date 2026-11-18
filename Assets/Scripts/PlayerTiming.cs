using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 상호작용. 상호작용 영역(TimingArea) 안에 들어가면 타이밍 미니게임을 띄우고 성공·실패를 CartStations에 알린다.
// 포탄을 들고 충격을 받으면 버티기 판정(원형 타이밍)을 띄운다.
// 카메라 연출은 CameraRig, 화면 문구·경고 패널은 StageHud가 맡는다.
[DefaultExecutionOrder(25)]
public sealed class PlayerTiming : MonoBehaviour
{
    private const float CatchTargetDegrees = .57f * 360f;
    private const float CatchWidthDegrees = .235f * 360f;
    private Vector3 panelOrigin;

    [SerializeField] private RectTransform interactionAreas;
    [SerializeField] private RectTransform repairRect;
    [SerializeField] private GameObject timingPanel;
    [SerializeField] private RectTransform background;
    [SerializeField] private CircularTimingGraphic circularGraphic;
    [SerializeField] private bool timingRingAtPlayer;
    private float resultUntil;
    private bool cookingImpactResult;
    public string ResultText { get; private set; }
    public string VisibleResult => Time.unscaledTime < resultUntil
        && (!cookingImpactResult || HasStationAccess && IsAt(StationAction.Kitchen)) ? ResultText : null;
    public void ShowResult(bool loaded) => ShowResult(loaded ? "장전 완료 · 포수 점화 가능" : "장전 실패 · 포탄 1발 손실");

    internal void ShowCookingImpact(int percentLost) =>
        ShowResult($"마차 흔들림 · 제작 진행도 {percentLost}%p 감소", cookingImpact: true);

    private void ShowResult(string text, bool cookingImpact = false)
    {
        ResultText = text;
        cookingImpactResult = cookingImpact;
        resultUntil = Time.unscaledTime + 1.6f;
    }

    [SerializeField] private CartStations stations;
    private NetworkPlayer actor;
    private RectTransform currentArea;
    private TimingArea currentSettings;
    public StationAction? NearbyRoyal { get; private set; }
    public StationAction? NearbyStation { get; private set; }
    private bool inside;
    private bool actionRequested;
    private RectTransform ignoredArea;
    public void ExitStation()
    {
        ignoredArea = currentArea;
        actor.RequestStation(-1);
        UpdateCurrentArea();
    }
    private int lastReservationArea = -2;
    private bool wasHoldingShell;
    private float circularTargetCenter;
    private float circularPinAngle;

    public bool IsCannonFocus => (IsAt(StationAction.Cannon) && stations.HoldsStation(StationAction.Cannon,actor)) || (stations.IsFiring && stations.Gunner.Value==actor.NetworkObjectId);
    public bool CanInteract => actor.Holding.Value
        || (NearbyRoyal is StationAction royal && stations.CanUse(royal, actor))
        || (inside && currentSettings != null && stations.CanUse(currentSettings.Action,actor));
    public bool IsInside => inside;
    public bool HasStationAccess => inside && currentSettings != null && stations.HoldsStation(currentSettings.Action,actor);
    public bool IsAt(StationAction action) => inside && currentSettings != null && currentSettings.Action == action;
    public StationAction? CurrentAction => inside && currentSettings != null ? currentSettings.Action : null;
    public RectTransform ActiveTimingRect => timingPanel == null || !timingPanel.activeInHierarchy ? null
        : circularGraphic != null && circularGraphic.gameObject.activeInHierarchy ? circularGraphic.rectTransform : background;
    public bool TimingReady
    {
        get
        {
            if (ActiveTimingRect == null || (!actor.Holding.Value && currentSettings == null)) return false;
            float width = actor.Holding.Value ? CatchWidth() : TargetWidth(currentSettings.CircularTargetAngle);
            return Mathf.Abs(Mathf.DeltaAngle(circularPinAngle, circularTargetCenter)) <= width * .5f;
        }
    }

    private void Awake()
    {
        actor = GetComponent<NetworkPlayer>();
        if (timingPanel != null) panelOrigin = timingPanel.transform.localPosition;
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
        if (!actor.Local) { ApplyRemote(); return; }
        SetWheelRepairActive(stations.WheelDetached);
        if (actor.SecondLocal ? Keyboard.current?.rightShiftKey.wasPressedThisFrame == true : Keyboard.current?.qKey.wasPressedThisFrame == true || Gamepad.current?.buttonEast.wasPressedThisFrame == true) ExitStation();
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
        if (timingPanel != null)
            timingPanel.SetActive(holdingShell || (inside && currentSettings != null && currentSettings.UsesTiming && stations.CanUse(currentSettings.Action,actor)));

        bool pressed = ConsumePress();
        if (holdingShell)
        {
            UpdateHoldTiming(pressed);
            return;
        }
        if (NearbyRoyal is StationAction royal)
        {
            if (pressed && stations.CanUse(royal, actor)) actor.Request(royal);
            return;
        }
        if (!inside) return;
        if (currentSettings != null && !stations.CanUse(currentSettings.Action,actor)) return;
        if (currentSettings != null && !currentSettings.UsesTiming)
        {
            if (pressed) actor.Request(currentSettings.Action);
            return;
        }
        UpdateStationTiming(pressed);
    }

    private void UpdateCurrentArea()
    {
        RectTransform area = FindArea();
        TimingArea settings = area != null ? area.GetComponent<TimingArea>() : null;
        NearbyStation = settings != null ? settings.Action : (StationAction?)null;
        NearbyRoyal = settings != null && TimingArea.IsRoyal(settings.Action) ? settings.Action : (StationAction?)null;
        if (NearbyRoyal.HasValue || (settings != null && (int)settings.Action == actor.CompletedStation.Value)) area = null;
        if (ignoredArea != area) ignoredArea = null;
        if (area == ignoredArea) area = null;
        if (area != null && !stations.HasWork(settings.Action, actor)
            && !(settings.Action == StationAction.Cannon && stations.IsFiring && stations.Gunner.Value == actor.NetworkObjectId))
            area = null;
        if (area == currentArea && !(inside && area == null)) return;
        currentArea = area;
        currentSettings = area != null ? area.GetComponent<TimingArea>() : null;
        inside = area != null;
        bool usesTiming = inside && (currentSettings == null || currentSettings.UsesTiming);
        if (timingPanel != null) timingPanel.SetActive(usesTiming);
        if (usesTiming) ResetTiming();
    }

    private bool ConsumePress()
    {
        if (actor.SecondLocal) return Keyboard.current?.enterKey.wasPressedThisFrame == true;
        Gamepad pad = Gamepad.current;
        bool pressed = actionRequested
            || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame));
        actionRequested = false;
        return pressed;
    }

    public static float TargetCenter(StationAction action, bool hold, int repairStep)
    {
        if (hold) return CatchTargetDegrees;
        if (action == StationAction.WheelRepair) return 360f * (repairStep == 0 ? .46f : .82f);
        return 360f * (action == StationAction.Reload ? .68f : .54f);
    }

    private static float PinAt(double time, float speed) =>
        Mathf.Repeat(180f - (float)((System.Math.Floor(time * 30d) / 30d * speed) % 360d), 360f);

    private float TargetWidth(float baseWidth) =>
        Mathf.Min(360f, baseWidth + stations.TimingBonusDegrees) * stations.TimingWidthRate;

    private float CatchWidth() =>
        Mathf.Min(.34f * 360f, CatchWidthDegrees + stations.TimingBonusDegrees) * stations.TimingWidthRate;

    private float CatchPinAt(double time)
    {
        float duration = CartRules.Active.shellHoldSeconds;
        return 360f * Mathf.Clamp01(1f - (float)(actor.HoldDeadline.Value - time) / duration);
    }

    public bool TimingSuccess(StationAction action, bool hold, double time)
    {
        if (hold) return Mathf.Abs(CatchPinAt(time) - CatchTargetDegrees) <= CatchWidth() * .5f;
        var area = FindArea();
        var settings = area != null ? area.GetComponent<TimingArea>() : null;
        if (settings == null || settings.Action != action || !settings.UsesTiming) return false;
        float angle = PinAt(time, settings.CircularSpeed);
        float center = TargetCenter(action, false, stations.WheelRepairSteps);
        return Mathf.Abs(Mathf.DeltaAngle(angle, center)) <= TargetWidth(settings.CircularTargetAngle) * .5f;
    }

    private void DrawRing(StationAction action, bool hold)
    {
        circularTargetCenter = TargetCenter(action, hold, stations.WheelRepairSteps);
        double time = actor.NetworkManager.ServerTime.Time;
        circularPinAngle = hold ? CatchPinAt(time) : PinAt(time, currentSettings.CircularSpeed);
        circularGraphic.SetAngles(circularTargetCenter, hold ? CatchWidth() : TargetWidth(currentSettings.CircularTargetAngle), circularPinAngle);
    }

    private void UpdateHoldTiming(bool pressed)
    {
        if (circularGraphic == null) return;
        DrawRing(StationAction.ShellPile, true);
        if (pressed) actor.Request(StationAction.ShellPile, true);
    }

    private void UpdateStationTiming(bool pressed)
    {
        if (currentSettings == null || circularGraphic == null) return;
        DrawRing(currentSettings.Action, false);
        if (pressed) actor.Request(currentSettings.Action);
    }

    private RectTransform FindArea()
    {
        if (interactionAreas == null) return null;
        if (actor.InCellar && repairRect != null && stations.IsInsideDetachedTire(transform.position - Vector3.up * NetworkPlayer.CellarOffset)) return repairRect;
        RectTransform nearestRoyal = null;
        float royalDistance = float.PositiveInfinity;
        for (int i = 0; i < interactionAreas.childCount; i++)
        {
            Transform child = interactionAreas.GetChild(i);
            if (!child.gameObject.activeInHierarchy || !(child is RectTransform area) || area == repairRect) continue;
            if (!area.TryGetComponent<TimingArea>(out var station) || station.Action == StationAction.Stairs) continue;
            Vector3 local = area.InverseTransformPoint(transform.position);
            if (!area.rect.Contains(new Vector2(local.x, local.y))) continue;
            if (!TimingArea.IsRoyal(station.Action))
            {
                if (nearestRoyal == null) return area;
                continue;
            }
            float distance = (area.position - transform.position).sqrMagnitude;
            if (distance < royalDistance) { nearestRoyal = area; royalDistance = distance; }
        }
        return nearestRoyal;
    }

    public RectTransform AreaFor(StationAction action)
    {
        if (action == StationAction.WheelRepair) return repairRect;
        foreach (RectTransform area in interactionAreas)
            if (area.TryGetComponent<TimingArea>(out var station) && station.Action == action) return area;
        return null;
    }

    public bool InsideCompletedArea(StationAction action)
    {
        if (action == StationAction.WheelRepair) return IsInActionArea(action);
        var area = AreaFor(action);
        return area != null && area.rect.Contains(area.InverseTransformPoint(transform.position));
    }

    public bool IsInActionArea(StationAction action)
    {
        if (action==StationAction.WheelRepair) return actor.InCellar && stations.IsInsideDetachedTire(transform.position - Vector3.up * NetworkPlayer.CellarOffset);
        var area=FindArea();
        return area != null && area.GetComponent<TimingArea>().Action==action;
    }
    private void LateUpdate()
    {
        if (timingPanel != null && timingPanel.activeInHierarchy && actor != null && actor.IsSpawned)
        {
            timingPanel.transform.position = timingPanel.transform.parent.TransformPoint(panelOrigin) + stations.RingOffset(actor);
            if (timingRingAtPlayer && circularGraphic != null)
                circularGraphic.rectTransform.position = transform.position + stations.RingOffset(actor);
        }
        if (actor==null || !actor.Local) return;
        bool visible=CartStations.Running && timingPanel.activeSelf;
        int action=actor.Holding.Value ? -2 : currentSettings != null ? (int)currentSettings.Action : -1;
        float speed=actor.Holding.Value || currentSettings == null ? 0f : currentSettings.CircularSpeed;
        float width=actor.Holding.Value ? CatchWidth() : currentSettings != null ? TargetWidth(currentSettings.CircularTargetAngle) : 0f;
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
        double time = actor.NetworkManager.ServerTime.Time;
        float angle = task.Action == -2 ? CatchPinAt(time) : Mathf.Repeat(task.Angle-(float)(time-task.Time)*task.Speed,360f);
        circularGraphic.SetAngles(task.Target,task.Width,angle);
    }

    private void ResetHoldTiming()
    {
        if (background != null) background.gameObject.SetActive(false);
        if (circularGraphic == null) return;
        circularGraphic.gameObject.SetActive(true);
        DrawRing(StationAction.ShellPile, true);
    }

    private void ResetTiming()
    {
        if (background != null) background.gameObject.SetActive(false);
        if (circularGraphic == null || currentSettings == null) return;
        circularGraphic.gameObject.SetActive(currentSettings.UsesTiming);
        if (currentSettings.UsesTiming) DrawRing(currentSettings.Action, false);
    }


}

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
    private const float CookingLossAnimationSeconds = .22f;
    private float cookingLossStarted, cookingBeforeLoss, cookingAfterLoss;
    public string ResultText { get; private set; }
    public string VisibleResult => Time.unscaledTime < resultUntil
        && (!cookingImpactResult || HasStationAccess && IsAt(StationAction.Kitchen)) ? ResultText : null;
    public void ShowResult(bool loaded) => ShowResult(LocalizationTable.T(loaded ? "장전 완료 · 포수 점화 가능" : "장전 실패 · 포탄 1발 손실"));

    internal void ShowShellReturned() => ShowResult(LocalizationTable.T("장전 실패 · 포탄이 적재함에 남았다"));

    internal void ShowCookingImpact(float before, float after)
    {
        cookingLossStarted = Time.unscaledTime;
        cookingBeforeLoss = before;
        cookingAfterLoss = after;
        int percentLost = Mathf.RoundToInt((before - after) * 100f);
        ShowResult(LocalizationTable.F("마차 흔들림 · 제작 진행도 {0}%p 감소", percentLost), cookingImpact: true);
    }

    internal float CookingProgressForHud(float progress)
    {
        if (!cookingImpactResult || VisibleResult == null) return progress;
        float age = Time.unscaledTime - cookingLossStarted;
        if (SettingsPanel.ReducedMotion) return age < CookingLossAnimationSeconds ? cookingAfterLoss : progress;
        if (age < CookingLossAnimationSeconds)
            return Mathf.Lerp(cookingBeforeLoss, cookingAfterLoss, age / CookingLossAnimationSeconds);
        return Mathf.Lerp(cookingAfterLoss, progress, (age - CookingLossAnimationSeconds) / CookingLossAnimationSeconds);
    }

    private void ShowResult(string text, bool cookingImpact = false)
    {
        ResultText = text;
        cookingImpactResult = cookingImpact;
        resultUntil = Time.unscaledTime + 1.6f;
    }

    [SerializeField] private CartStations stations;
    private NetworkPlayer actor;
    private WheelRepairMarker repairMarker;
    private RectTransform currentArea;
    private TimingArea currentSettings;
    public StationAction? NearbyRoyal { get; private set; }
    public StationAction? NearbyStation { get; private set; }
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

    public bool IsCannonFocus => IsAt(StationAction.Cannon) && HasStationAccess;
    public bool CanInteract => actor.Holding.Value
        || (NearbyRoyal is StationAction royal && stations.CanUse(royal, actor))
        || (CurrentAction is StationAction action && stations.CanUse(action, actor));
    public bool HasStationAccess => CurrentAction is StationAction action && stations.HoldsStation(action, actor);
    public bool IsAt(StationAction action) => CurrentAction is StationAction current && TimingArea.CannonRole(current) == action;
    public StationAction? CurrentAction { get; private set; }
    private bool UsesTiming => CurrentAction is StationAction action && TimingArea.UsesTimingFor(action);
    private StationAction ActionAt(TimingArea area) => TimingArea.IsCannon(area.Action) ? stations.CannonActionAt(TimingArea.IsUpper(area.Action)) : area.Action;
    private Bounds BodyBounds => actor.BodyArtwork != null && actor.BodyArtwork.sprite != null ? actor.BodyArtwork.bounds : default;
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
        if (interactionAreas != null) repairMarker = interactionAreas.GetComponent<WheelRepairMarker>();
        if (timingPanel != null) panelOrigin = timingPanel.transform.localPosition;
        if (timingPanel != null) timingPanel.SetActive(false);
    }

    public void TriggerInteraction()
    {
        if (actor != null && actor.Local && actor.UsesMobileControls && CartStations.Running && CanInteract)
            actionRequested = true;
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
        if (!actor.UsesMobileControls && (actor.SecondLocal ? Keyboard.current?.rightShiftKey.wasPressedThisFrame == true
            : Keyboard.current?.qKey.wasPressedThisFrame == true || Gamepad.current?.buttonEast.wasPressedThisFrame == true))
            ExitStation();
        UpdateCurrentArea();
        int requested = currentSettings != null ? (int)currentSettings.Action : -1;
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
            if (timingPanel != null) timingPanel.SetActive(holdingShell || UsesTiming);
            if (holdingShell) ResetHoldTiming();
            else if (UsesTiming) ResetTiming();
        }
        if (timingPanel != null)
            timingPanel.SetActive(holdingShell || (UsesTiming && stations.CanUse(CurrentAction.Value, actor)));

        bool pressed = ConsumePress();
        if (holdingShell)
        {
            UpdateHoldTiming(pressed);
            return;
        }
        if (NearbyRoyal is StationAction royal)
        {
            if (pressed && stations.AvailableCare(royal, actor) == CareAction.Attend && actor.CanUseStairShelf()) actor.RequestStairShelf();
            else if (pressed && stations.CanUse(royal, actor)) actor.Request(royal);
            return;
        }
        if (pressed && CurrentAction == null && actor.CanUseStairShelf())
        {
            actor.RequestStairShelf();
            return;
        }
        if (CurrentAction is not StationAction action || !stations.CanUse(action, actor)) return;
        if (!UsesTiming)
        {
            if (pressed) actor.Request(action);
            return;
        }
        UpdateStationTiming(pressed);
    }

    private void UpdateCurrentArea()
    {
        RectTransform area = FindArea();
        TimingArea settings = area != null ? area.GetComponent<TimingArea>() : null;
        NearbyStation = settings != null ? ActionAt(settings) : null;
        NearbyRoyal = settings != null && TimingArea.IsRoyal(settings.Action) ? settings.Action : (StationAction?)null;
        if (NearbyRoyal.HasValue || (settings != null && (int)settings.Action == actor.CompletedStation.Value)) area = null;
        if (ignoredArea != area) ignoredArea = null;
        if (area == ignoredArea) area = null;
        if (area != null && !(TimingArea.IsCannon(settings.Action) ? actor.HandsFree : stations.HasWork(settings.Action, actor)))
            area = null;
        StationAction? nextAction = area != null ? ActionAt(settings) : null;
        if (area == currentArea && nextAction == CurrentAction) return;
        currentArea = area;
        currentSettings = area != null ? settings : null;
        CurrentAction = nextAction;
        if (timingPanel != null) timingPanel.SetActive(UsesTiming);
        if (UsesTiming) ResetTiming();
    }

    private bool ConsumePress()
    {
        bool touchPressed = actionRequested;
        actionRequested = false;
        if (actor.UsesMobileControls) return touchPressed;
        if (actor.SecondLocal) return Keyboard.current?.enterKey.wasPressedThisFrame == true;
        var pad = Gamepad.current;
        return Keyboard.current?.spaceKey.wasPressedThisFrame == true || pad?.buttonSouth.wasPressedThisFrame == true
            || pad?.rightTrigger.wasPressedThisFrame == true;
    }

    public static float TargetCenter(StationAction action, bool hold, int repairStep)
    {
        if (hold) return CatchTargetDegrees;
        if (action == StationAction.WheelRepair) return 360f * (repairStep == 0 ? .46f : .82f);
        return 360f * (TimingArea.CannonRole(action) == StationAction.Reload ? .68f : .54f);
    }

    private static float PinAt(double time, float speed) =>
        Mathf.Repeat(180f - (float)((System.Math.Floor(time * 30d) / 30d * speed) % 360d), 360f);

    private float TargetWidth(float baseWidth) =>
        Mathf.Min(360f, baseWidth + stations.TimingBonusDegrees + stations.ShopTimingBonus(CurrentAction, stations.WheelRepairSteps)) * stations.TimingWidthRate;

    private float CatchWidth() =>
        Mathf.Min(.34f * 360f + stations.ShopCatchBonus, CatchWidthDegrees + stations.TimingBonusDegrees + stations.ShopCatchBonus) * stations.TimingWidthRate;

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
        if (settings == null || ActionAt(settings) != action || !TimingArea.UsesTimingFor(action)) return false;
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
        DrawRing(CurrentAction.Value, false);
        if (pressed) actor.Request(CurrentAction.Value);
    }

    private RectTransform FindArea()
    {
        if (interactionAreas == null) return null;
        Bounds body = BodyBounds;
        if (OverlapsWheelRepair(body)) return repairRect;
        RectTransform workArea = null;
        RectTransform nearestRoyal = null;
        RectTransform nearestUsableRoyal = null;
        float royalDistance = float.PositiveInfinity;
        float usableRoyalDistance = float.PositiveInfinity;
        for (int i = 0; i < interactionAreas.childCount; i++)
        {
            Transform child = interactionAreas.GetChild(i);
            if (!child.gameObject.activeInHierarchy || !(child is RectTransform area) || area == repairRect) continue;
            if (!area.TryGetComponent<TimingArea>(out var station) || station.Action == StationAction.Stairs) continue;
            bool royal = TimingArea.IsRoyal(station.Action);
            if (royal && !stations.WithinRoyalInteraction(station.Action, actor)) continue;
            if (TimingArea.IsCannon(station.Action))
            {
                if (actor.InCellar || !OverlapsCannon(station, body)) continue;
                if (stations.HoldsStation(station.Action, actor)) return area;
                if (workArea == null) workArea = area;
                continue;
            }
            if (!royal && !OverlapsStation(station, body)) continue;
            if (!royal)
            {
                if (workArea == null) workArea = area;
                continue;
            }
            var rider = station.Action == StationAction.Princess ? stations.Princess.Value : stations.Prince.Value;
            float distance = (stations.Cart.transform.TransformPoint(rider.Position) - transform.position).sqrMagnitude;
            if (distance < royalDistance) { nearestRoyal = area; royalDistance = distance; }
            if (distance < usableRoyalDistance && stations.AvailableCare(station.Action, actor) is not (CareAction.None or CareAction.Attend))
            {
                nearestUsableRoyal = area;
                usableRoyalDistance = distance;
            }
        }
        if (nearestUsableRoyal != null) return nearestUsableRoyal;
        if (workArea != null) return workArea;
        if (stations.AttentionTarget(actor) is StationAction attention) return AreaFor(attention);
        return nearestRoyal;
    }

    public RectTransform AreaFor(StationAction action)
    {
        if (action == StationAction.WheelRepair) return repairRect;
        if (TimingArea.IsCannon(action)) action = TimingArea.CannonStation(action);
        foreach (RectTransform area in interactionAreas)
            if (area.TryGetComponent<TimingArea>(out var station) && station.Action == action) return area;
        return null;
    }

    public bool InsideCompletedArea(StationAction action)
    {
        if (action == StationAction.WheelRepair) return OverlapsWheelRepair(BodyBounds);
        var area = AreaFor(action);
        return area != null && area.TryGetComponent<TimingArea>(out var station) && OverlapsStation(station, BodyBounds);
    }

    public bool IsInActionArea(StationAction action)
    {
        if (TimingArea.IsCannon(action))
        {
            var cannon = AreaFor(action);
            return !actor.InCellar && cannon != null && cannon.gameObject.activeInHierarchy
                && cannon.TryGetComponent<TimingArea>(out var station) && OverlapsCannon(station, BodyBounds);
        }
        if (action == StationAction.WheelRepair) return OverlapsWheelRepair(BodyBounds);
        var area=FindArea();
        return area != null && area.GetComponent<TimingArea>().Action==action;
    }
    private bool OverlapsWheelRepair(Bounds body) =>
        actor.InCellar && repairRect != null && repairMarker != null && repairMarker.OverlapsPlayer(stations, body);

    private bool OverlapsCannon(TimingArea station, Bounds body)
    {
        // 긴 손잡이 솔: 포신 청소 차례면 조금 더 멀리서도 닿는다.
        float reach = TimingArea.CannonRole(stations.CannonActionAt(TimingArea.IsUpper(station.Action))) == StationAction.CannonCleaning && stations.Owns(ShopPerk.LongBrush) ? CartStations.LongBrushReach : 1f;
        return station.OverlapsPlayer(body, reach);
    }

    private bool OverlapsStation(TimingArea station, Bounds body)
    {
        float reach = station.Action == StationAction.ShellDeposit && stations.Owns(ShopPerk.WideRack)
            ? CartStations.WideRackScale : 1f;
        if (station.Action == StationAction.ShellDeposit && station.transform is RectTransform rack)
        {
            Vector2 point = (Vector2)rack.InverseTransformPoint(transform.position) - rack.rect.center;
            Vector2 radius = rack.rect.size * (.5f * reach);
            return new Vector2(point.x / radius.x, point.y / radius.y).sqrMagnitude <= 1f;
        }
        return station.OverlapsPlayer(body, reach);
    }

    // 장전 회수판용: 성공 구간을 살짝 벗어났는지.
    public bool NearMiss(StationAction action, double time)
    {
        var area = FindArea();
        var settings = area != null ? area.GetComponent<TimingArea>() : null;
        if (settings == null || ActionAt(settings) != action || !TimingArea.UsesTimingFor(action)) return false;
        float angle = PinAt(time, settings.CircularSpeed);
        float center = TargetCenter(action, false, stations.WheelRepairSteps);
        return Mathf.Abs(Mathf.DeltaAngle(angle, center)) <= TargetWidth(settings.CircularTargetAngle) * .5f + CartStations.NearMissDegrees;
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
        int action=actor.Holding.Value ? -2 : (int)(CurrentAction ?? (StationAction)(-1));
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
        circularGraphic.gameObject.SetActive(UsesTiming);
        if (UsesTiming) DrawRing(CurrentAction.Value, false);
    }


}

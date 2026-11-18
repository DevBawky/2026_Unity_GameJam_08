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
    [SerializeField, Min(0.1f)] private float cameraSmoothness = 8f;

    private CartStations stations;
    private CannonAim cannonAim;
    public bool IsCannonFocus => (inside && currentSettings != null && currentSettings.Action == StationAction.Cannon) || (stations != null && stations.IsFiring);
    public bool CanInteract => (stations != null && stations.IsHoldingShell) || (inside && currentSettings != null && currentSettings.Action != StationAction.ShellDeposit && (stations == null || stations.CanUse(currentSettings.Action)));
    private bool actionRequested;
    private bool wasHoldingShell;
    [Header("Scene UI")]
    [SerializeField] private TMPro.TMP_Text stationStatus;
    [SerializeField] private TMPro.TMP_Text statusAlert;
    [SerializeField] private GameObject statusPanel;
    [SerializeField, Range(0f, 1f)] private float anxietyWarningLevel = 0.5f;
    [SerializeField, Range(0f, 1f)] private float durabilityWarningLevel = 0.3f;
    [SerializeField, Min(0f)] private float headingWarningDegrees = 8f;
    [SerializeField, Min(0f)] private float horseDifferenceWarning = 0.5f;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private UnityEngine.UI.Button[] upgradeButtons;
    [SerializeField] private TMPro.TMP_Text[] upgradeLabels;
    [SerializeField] private RectTransform repairRect;
    [SerializeField] private UnityEngine.UI.Image healthFill;
    [SerializeField] private UnityEngine.UI.Image repairFill;
    [SerializeField] private GameObject repairProgress;
    [SerializeField] private RectTransform distanceMarker;
    [SerializeField] private TMPro.TMP_Text distanceLabel;
    private float shakeRemaining;
    private float shakeDuration;
    private float shakeStrength;
    [SerializeField, Min(0f)] private float timingJitterDegrees = 10f;
    private Vector3 lastShakeOffset;
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
        if (restartButton != null && restartButton.TryGetComponent(out UnityEngine.UI.Button retry))
            retry.onClick.AddListener(RestartGame);
        if (upgradeButtons != null)
            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                int choice = i;
                if (upgradeButtons[i] != null) upgradeButtons[i].onClick.AddListener(() => ChooseUpgrade(choice));
            }
    }

    private void Update()
    {
        RectTransform area = FindArea();
        if (area != currentArea || (inside && area == null))
        {
            currentArea = area;
            currentSettings = area != null ? area.GetComponent<TimingArea>() : null;
            inside = area != null;
            bool usesTiming = inside && (currentSettings == null || currentSettings.UsesTiming);
            if (timingPanel != null) timingPanel.SetActive(usesTiming);
            if (inside && currentSettings != null && stations != null) stations.Enter(currentSettings.Action);
            if (usesTiming) ResetTiming();
        }

        UpdateStationStatus();
        UpdateStatusAlert();
        if (stations != null && (stations.Failed || stations.ChoosingUpgrade)) { actionRequested = false; return; }
        bool holdingShell = stations != null && stations.IsHoldingShell;
        if (holdingShell != wasHoldingShell)
        {
            wasHoldingShell = holdingShell;
            if (timingPanel != null) timingPanel.SetActive(holdingShell || (inside && currentSettings != null && currentSettings.UsesTiming));
            if (holdingShell) ResetHoldTiming();
            else if (inside && currentSettings != null && currentSettings.UsesTiming) ResetTiming();
        }
        if (!holdingShell && inside && currentSettings != null && stations != null && currentSettings.Action == StationAction.ShellDeposit)
            stations.Enter(StationAction.ShellDeposit);
        if (timingPanel != null && !holdingShell)
            timingPanel.SetActive(inside && currentSettings != null && currentSettings.UsesTiming && (stations == null || stations.CanUse(currentSettings.Action)));
        Gamepad pad = Gamepad.current;
        bool pressed = actionRequested || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame));
        actionRequested = false;
        if (holdingShell)
        {
            if (circularGraphic == null) return;
            circularPinAngle = Mathf.Repeat(circularPinAngle - 180f * Time.deltaTime, 360f);
            float visiblePin = ShakenPinAngle();
            float holdAngle = Mathf.Min(360f, 60f + stations.TimingBonusDegrees);
            circularGraphic.SetAngles(circularTargetCenter, holdAngle, visiblePin);
            if (pressed) stations.ResolveShellHold(Mathf.Abs(Mathf.DeltaAngle(visiblePin, circularTargetCenter)) <= holdAngle * 0.5f);
            return;
        }
        if (!inside) return;
        if (currentSettings != null && stations != null && !stations.CanUse(currentSettings.Action)) return;
        if (currentSettings != null && !currentSettings.UsesTiming)
        {
            if (currentSettings.Action == StationAction.Cannon && pressed && stations != null) stations.Ignite();
            return;
        }
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

        if (pressed)
        {
            bool success;
            if (circular)
                success = Mathf.Abs(Mathf.DeltaAngle(displayedPin, circularTargetCenter)) <= CurrentTargetAngle() * 0.5f;
            else
            {
                float pinCenter = pin.localPosition.x + pin.rect.center.x;
                float targetCenter = target.localPosition.x + target.rect.center.x;
                success = Mathf.Abs(pinCenter - targetCenter) <= target.rect.width * 0.5f;
            }
            Debug.Log(success ? "성공" : "실패", this);
            if (currentSettings != null && stations != null)
            {
                if (success) stations.Succeed(currentSettings.Action);
                else stations.Fail(currentSettings.Action);
            }
        }
    }

    public void TriggerInteraction() { if (CanInteract) actionRequested = true; }

    private RectTransform FindArea()
    {
        if (interactionAreas == null) return null;
        if (repairRect != null && stations != null && stations.IsInsideDetachedTire(transform.position)) return repairRect;
        foreach (Transform child in interactionAreas)
        {
            if (!child.gameObject.activeInHierarchy || !(child is RectTransform area) || area == repairRect) continue;
            Vector3 local = area.InverseTransformPoint(transform.position);
            if (area.rect.Contains(new Vector2(local.x, local.y))) return area;
        }
        return null;
    }

    private float CurrentTargetAngle() => Mathf.Min(360f, currentSettings.CircularTargetAngle + (stations != null ? stations.TimingBonusDegrees : 0f));

    private float ShakenPinAngle()
    {
        return circularPinAngle + (shakeRemaining > 0f ? Random.Range(-timingJitterDegrees, timingJitterDegrees) * shakeRemaining / shakeDuration : 0f);
    }

    private void ResetHoldTiming()
    {
        if (background != null) background.gameObject.SetActive(false);
        if (circularGraphic == null) return;
        circularGraphic.gameObject.SetActive(true);
        circularTargetCenter = Random.Range(0f, 360f);
        circularPinAngle = 180f;
        circularGraphic.SetAngles(circularTargetCenter, 60f, circularPinAngle);
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
            circularGraphic.gameObject.SetActive(true);
            circularTargetCenter = Random.Range(0f, 360f);
            circularPinAngle = 180f;
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

    private void UpdateProgressIndicators()
    {
        if (stations == null) return;
        if (healthFill != null) healthFill.fillAmount = Mathf.Clamp01(stations.Durability / 100f);
        if (repairProgress != null) repairProgress.SetActive(stations.WheelDetached && !stations.Failed && !stations.ChoosingUpgrade);
        if (repairFill != null) repairFill.fillAmount = Mathf.Clamp01(stations.WheelRepairSteps / 2f);
        if (distanceMarker != null)
        {
            float progress = Mathf.Clamp01(stations.StageDistance / Mathf.Max(1f, stations.StageGoal));
            distanceMarker.anchorMin = distanceMarker.anchorMax = new Vector2(progress, 0.5f);
            distanceMarker.anchoredPosition = Vector2.zero;
            if (distanceLabel != null) distanceLabel.text = $"{progress * 100f:0}%";
        }
    }

    private void RestartGame() { if (stations != null) stations.Restart(); }
    private void ChooseUpgrade(int choice) { if (stations != null) stations.ChooseUpgrade(choice); }

    private void UpdateStationStatus()
    {
        if (stationStatus == null || stations == null) return;
        UpdateProgressIndicators();
        if (restartButton != null) restartButton.SetActive(stations.Failed);
        if (upgradePanel != null) upgradePanel.SetActive(stations.ChoosingUpgrade);
        if (stations.Failed) { stationStatus.text = stations.FailureReason; return; }
        if (stations.ChoosingUpgrade)
        {
            stationStatus.text = $"STAGE {stations.StageNumber} COMPLETE - CHOOSE AN UPGRADE";
            upgradeLabels[0].text = $"1 / A: CUSHION AXLE  Lv {stations.CushionAxleLevel} -> {stations.CushionAxleLevel + 1}  (impact x 0.78)";
            upgradeLabels[1].text = $"2 / X: TRAINED HORSES  Lv {stations.HorseLevel} -> {stations.HorseLevel + 1}  (+3 speed)";
            upgradeLabels[2].text = $"3 / Y: PRECISION SIGHT  Lv {stations.PrecisionLevel} -> {stations.PrecisionLevel + 1}  (+2.7% timing)";
            return;
        }
        string stageName = StageNames[(stations.StageNumber - 1) % StageNames.Length];
        stationStatus.text = stageName;
        if (!inside || currentSettings == null) return;
        switch (currentSettings.Action)
        {
            case StationAction.ShellDeposit:
                stationStatus.text += $"     STORED SHELLS: {stations.DepositedShells}";
                break;
            case StationAction.Reload:
                if (stations.IsLoaded) stationStatus.text += $"     CANNON LOADED - FIRE FIRST     STORED: {stations.DepositedShells}";
                else if (!stations.IsDeposited) stationStatus.text += "     STORAGE EMPTY - BRING A SHELL";
                else stationStatus.text += $"     STORED SHELLS: {stations.DepositedShells}";
                break;
        }
    }

    private void UpdateStatusAlert()
    {
        if (statusAlert == null || stations == null) return;
        var message = new System.Text.StringBuilder();
        if (stations.Failed)
            message.Append(stations.FailureReason);
        else if (!stations.ChoosingUpgrade)
        {
            if (stations.WheelDetached && stations.DetachedTire != null)
            {
                string number = stations.DetachedTire.name.Replace("Tire ", "");
                message.Append(number).Append(stations.WheelRepairSteps == 0 ? "번 타이어 고장 - 수리 필요" : "번 타이어 축 핀 고정 필요");
            }
            if (stations.PrinceAnxiety >= anxietyWarningLevel)
                AppendAlert(message, stations.PrinceEscaping ? "왕자 탈출 시도 - 달래기 필요" : "왕자 불안도 높음");
            if (stations.PrincessAnxiety >= anxietyWarningLevel)
                AppendAlert(message, stations.PrincessEscaping ? "공주 탈출 시도 - 달래기 필요" : "공주 불안도 높음");
            if (stations.Durability <= durabilityWarningLevel * 100f)
                AppendAlert(message, "마차 내구도 위험");
            CartMotion cart = stations.Cart;
            if (cart != null)
            {
                float difference = cart.RightHorseSpeed - cart.LeftHorseSpeed;
                if (cart.Heading >= headingWarningDegrees)
                    AppendAlert(message, difference < -horseDifferenceWarning ? "마차 방향 위쪽으로 조정 중" : "마차 아래쪽 치우침 - 왼말 재촉");
                else if (cart.Heading <= -headingWarningDegrees)
                    AppendAlert(message, difference > horseDifferenceWarning ? "마차 방향 아래쪽으로 조정 중" : "마차 위쪽 치우침 - 오른말 재촉");
                else if (difference >= horseDifferenceWarning)
                    AppendAlert(message, "오른말 빠름 - 왼말 재촉");
                else if (difference <= -horseDifferenceWarning)
                    AppendAlert(message, "왼말 빠름 - 오른말 재촉");
            }
        }
        string next = message.ToString();
        if (statusAlert.text != next) statusAlert.text = next;
        if (statusPanel != null && statusPanel.activeSelf != (message.Length > 0))
            statusPanel.SetActive(message.Length > 0);
    }

    private static void AppendAlert(System.Text.StringBuilder message, string text)
    {
        if (message.Length > 0) message.Append('\n');
        message.Append(text);
    }

    public void SetWheelRepairActive(bool active)
    {
        if (repairRect != null) repairRect.gameObject.SetActive(active);
    }

    public void ShakeCamera(float strength, float duration)
    {
        shakeStrength = Mathf.Max(shakeStrength, Mathf.Max(0f, strength));
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeRemaining = shakeDuration;
    }

    private static readonly string[] StageNames = { "THORNWOOD PASS", "ASHEN ROAD", "TWILIGHT BORDER" };
    private readonly Vector3[] uiCorners = new Vector3[4];

    private void IncludeRect(ref Bounds bounds, RectTransform rect)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy) return;
        rect.GetWorldCorners(uiCorners);
        foreach (Vector3 corner in uiCorners) bounds.Encapsulate(corner);
    }

    private void LateUpdate()
    {
        if (gameCamera == null) return;
        float t = 1f - Mathf.Exp(-cameraSmoothness * Time.deltaTime);
        bool aiming = IsCannonFocus && cannonAim != null && cannonAim.Muzzle != null;
        bool royalFocus = inside && currentSettings != null && currentSettings.Action == StationAction.RoyalFamily;
        bool zooming = aiming || inside || (stations != null && (stations.IsCarrying || stations.IsHoldingShell));
        float zoom = stations != null && (stations.IsCarrying || stations.IsHoldingShell) ? 3f : inside ? 4f : 1f;
        float targetSize = originalCameraSize / zoom;
        Vector3 destination = zooming ? new Vector3(transform.position.x, transform.position.y, originalCameraPosition.z) : originalCameraPosition;
        Bounds visibleBounds = new Bounds(transform.position, Vector3.zero);

        if (aiming)
        {
            Vector3 muzzle = cannonAim.Muzzle.position;
            Vector3 endpoint = muzzle + (Vector3)cannonAim.Direction * cannonAim.LineLength;
            visibleBounds = new Bounds(muzzle, Vector3.zero);
            visibleBounds.Encapsulate(endpoint + new Vector3(0.25f, 0.25f));
            visibleBounds.Encapsulate(endpoint - new Vector3(0.25f, 0.25f));
            if (stations != null && stations.CartBody != null && stations.CartBody.enabled && stations.CartBody.gameObject.activeInHierarchy)
            {
                // Keep the rightmost 20% of the cart body, including its full height, in view.
                Bounds cartBounds = stations.CartBody.bounds;
                float cartVisibleLeft = Mathf.Lerp(cartBounds.max.x, cartBounds.min.x, 0.2f);
                visibleBounds.Encapsulate(new Vector3(cartVisibleLeft, cartBounds.min.y, muzzle.z));
                visibleBounds.Encapsulate(new Vector3(cartBounds.max.x, cartBounds.max.y, muzzle.z));
            }
            targetSize = originalCameraSize / 2f;
        }
        if (zooming)
        {
            // Read the live layout every frame, including while the cannon is focused.
            if (timingPanel != null && timingPanel.activeInHierarchy)
            {
                RectTransform timingRect = circularGraphic != null && circularGraphic.gameObject.activeInHierarchy
                    ? circularGraphic.rectTransform : background;
                IncludeRect(ref visibleBounds, timingRect);
            }
            if (royalFocus && stations != null)
            {
                IncludeRect(ref visibleBounds, stations.PrinceAnxietyUI);
                IncludeRect(ref visibleBounds, stations.PrincessAnxietyUI);
            }
        }

        if (zooming && gameCamera.orthographic)
        {
            float aspect = Mathf.Max(gameCamera.aspect, 0.01f);
            const float safeFraction = 0.9f;
            destination = new Vector3(visibleBounds.center.x, visibleBounds.center.y, originalCameraPosition.z);
            targetSize = Mathf.Max(targetSize, visibleBounds.extents.y / safeFraction,
                visibleBounds.extents.x / (aspect * safeFraction));
        }
        Vector3 basePosition = gameCamera.transform.position - lastShakeOffset;
        basePosition = Vector3.Lerp(basePosition, destination, t);
        lastShakeOffset = Vector3.zero;
        if (shakeRemaining > 0f)
        {
            float intensity = shakeStrength * Mathf.Clamp01(shakeRemaining / shakeDuration);
            lastShakeOffset = new Vector3(Random.Range(-intensity, intensity), Random.Range(-intensity, intensity), 0f);
            shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.unscaledDeltaTime);
            if (shakeRemaining == 0f) shakeStrength = 0f;
        }
        gameCamera.transform.position = basePosition + lastShakeOffset;
        if (gameCamera.orthographic)
        {
            float size = Mathf.Lerp(gameCamera.orthographicSize, targetSize, t);
            if (zooming)
            {
                // During the transition, also fit the current bounds around the current camera.
                Vector3 center = gameCamera.transform.position;
                float halfHeight = Mathf.Max(Mathf.Abs(visibleBounds.min.y - center.y), Mathf.Abs(visibleBounds.max.y - center.y));
                float halfWidth = Mathf.Max(Mathf.Abs(visibleBounds.min.x - center.x), Mathf.Abs(visibleBounds.max.x - center.x));
                size = Mathf.Max(size, halfHeight / 0.9f, halfWidth / (Mathf.Max(gameCamera.aspect, 0.01f) * 0.9f));
            }
            gameCamera.orthographicSize = size;
        }
        else
            gameCamera.fieldOfView = Mathf.Lerp(gameCamera.fieldOfView, inside ? Mathf.Min(originalFieldOfView, 30f) : originalFieldOfView, t);
    }
}

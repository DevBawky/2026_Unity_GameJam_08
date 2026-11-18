using TMPro;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// 화면 HUD. 스테이지 이름·상태 문구, 위험 경고 패널, 내구도·바퀴 수리·진행 거리 표시, 재시작 버튼, 업그레이드 선택 창.
// 상태 문구와 경고는 내용이 바뀐 프레임에만 텍스트를 다시 넣는다.
public sealed class StageHud : MonoBehaviour
{
    private static readonly string[] StageNames = { "THORNWOOD PASS", "ASHEN ROAD", "TWILIGHT BORDER" };

    [SerializeField] private CartStations stations;
    [SerializeField] private PlayerTiming player;
    [SerializeField] private TMP_Text stationStatus;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TMP_Text[] upgradeLabels;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image repairFill;
    [SerializeField] private GameObject repairProgress;
    [SerializeField] private RectTransform distanceMarker;
    [SerializeField] private TMP_Text distanceLabel;

    [Header("Warning panel")]
    [SerializeField] private TMP_Text statusAlert;
    [SerializeField] private GameObject statusPanel;
    [SerializeField, Range(0f, 1f)] private float anxietyWarningLevel = 0.5f;
    [SerializeField, Range(0f, 1f)] private float durabilityWarningLevel = 0.3f;
    [SerializeField, Min(0f)] private float headingWarningDegrees = 8f;
    [SerializeField, Min(0f)] private float horseDifferenceWarning = 0.5f;

    private readonly StringBuilder alert = new();
    private SpriteRenderer alertTire;
    private string alertTireNumber;
    private int lastStatusKey = int.MinValue;
    private int lastProgressPercent = -1;

    private void Awake()
    {
        if (restartButton.TryGetComponent(out Button retry)) retry.onClick.AddListener(stations.Restart);
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            int choice = i;
            upgradeButtons[i].onClick.AddListener(() => stations.ChooseUpgrade(choice));
        }
    }

    private void Update()
    {
        if (NetworkPlayer.LocalPlayer == null || !stations.IsSpawned) return;
        player=NetworkPlayer.LocalPlayer.Timing;
        UpdateIndicators();
        restartButton.SetActive(stations.Failed && stations.IsHost);
        upgradePanel.SetActive(stations.ChoosingUpgrade && stations.IsHost);
        UpdateAlert();

        int key = HashCode.Combine(stations.Failed, stations.ChoosingUpgrade, stations.StageNumber,
            player.CurrentAction, stations.DepositedShells, stations.IsLoaded);
        if (key == lastStatusKey) return;
        lastStatusKey = key;
        UpdateStatusText();
    }

    private void UpdateIndicators()
    {
        healthFill.fillAmount = Mathf.Clamp01(stations.Durability / CartStations.MaxDurability);
        repairProgress.SetActive(stations.WheelDetached && !stations.Failed && !stations.ChoosingUpgrade);
        repairFill.fillAmount = Mathf.Clamp01(stations.WheelRepairSteps / (float)CartStations.WheelRepairStepsNeeded);
        float progress = Mathf.Clamp01(stations.StageDistance / Mathf.Max(1f, stations.StageGoal));
        distanceMarker.anchorMin = distanceMarker.anchorMax = new Vector2(progress, 0.5f);
        distanceMarker.anchoredPosition = Vector2.zero;
        int percent = Mathf.RoundToInt(progress * 100f);
        if (distanceLabel != null && percent != lastProgressPercent)
        {
            lastProgressPercent = percent;
            distanceLabel.text = $"{percent}%";
        }
    }

    private void UpdateStatusText()
    {
        if (stations.Failed)
        {
            stationStatus.text = stations.FailureReason;
            return;
        }
        if (stations.ChoosingUpgrade)
        {
            stationStatus.text = $"STAGE {stations.StageNumber} COMPLETE - CHOOSE AN UPGRADE";
            string[] lines =
            {
                $"1 / A: CUSHION AXLE  Lv {stations.CushionAxleLevel} -> {stations.CushionAxleLevel + 1}  (impact x {stations.CushionImpactFactor:0.##})",
                $"2 / X: TRAINED HORSES  Lv {stations.HorseLevel} -> {stations.HorseLevel + 1}  (+{stations.SpeedPerHorseLevel:0.#} speed)",
                $"3 / Y: PRECISION SIGHT  Lv {stations.PrecisionLevel} -> {stations.PrecisionLevel + 1}  (+{stations.PrecisionPerLevel * 100f:0.#}% timing)",
            };
            for (int i = 0; i < upgradeLabels.Length && i < lines.Length; i++) upgradeLabels[i].text = lines[i];
            return;
        }

        string text = StageNames[(stations.StageNumber - 1) % StageNames.Length];
        switch (player.CurrentAction)
        {
            case StationAction.ShellDeposit:
                text += $"     STORED SHELLS: {stations.DepositedShells}";
                break;
            case StationAction.Reload:
                if (stations.IsLoaded) text += $"     CANNON LOADED - FIRE FIRST     STORED: {stations.DepositedShells}";
                else if (!stations.IsDeposited) text += "     STORAGE EMPTY - BRING A SHELL";
                else text += $"     STORED SHELLS: {stations.DepositedShells}";
                break;
        }
        stationStatus.text = text;
    }

    private void UpdateAlert()
    {
        if (statusAlert == null) return;
        alert.Clear();
        // 실패 문구는 stationStatus가 보여 주므로 경고 패널은 진행 중일 때만 쓴다.
        if (CartStations.Running) BuildWarnings();

        if (!alert.Equals(statusAlert.text.AsSpan())) statusAlert.text = alert.ToString();
        bool show = alert.Length > 0;
        if (statusPanel != null && statusPanel.activeSelf != show) statusPanel.SetActive(show);
    }

    private void BuildWarnings()
    {
        SpriteRenderer tire = stations.DetachedTire;
        if (stations.WheelDetached && tire != null)
        {
            // name 접근과 Replace는 매번 문자열을 만들어서 타이어가 바뀔 때만 구한다.
            if (tire != alertTire)
            {
                alertTire = tire;
                alertTireNumber = tire.name.Replace("Tire ", "");
            }
            alert.Append(alertTireNumber).Append(stations.WheelRepairSteps == 0 ? "번 타이어 고장 - 수리 필요" : "번 타이어 축 핀 고정 필요");
        }
        if (stations.PrinceAnxiety >= anxietyWarningLevel)
            AppendLine(stations.PrinceEscaping ? "왕자 탈출 시도 - 달래기 필요" : "왕자 불안도 높음");
        if (stations.PrincessAnxiety >= anxietyWarningLevel)
            AppendLine(stations.PrincessEscaping ? "공주 탈출 시도 - 달래기 필요" : "공주 불안도 높음");
        if (stations.Durability <= durabilityWarningLevel * CartStations.MaxDurability)
            AppendLine("마차 내구도 위험");

        CartMotion cart = stations.Cart;
        float difference = cart.RightHorseSpeed - cart.LeftHorseSpeed;
        if (cart.Heading >= headingWarningDegrees)
            AppendLine(difference < -horseDifferenceWarning ? "마차 방향 위쪽으로 조정 중" : "마차 아래쪽 치우침 - 왼말 재촉");
        else if (cart.Heading <= -headingWarningDegrees)
            AppendLine(difference > horseDifferenceWarning ? "마차 방향 아래쪽으로 조정 중" : "마차 위쪽 치우침 - 오른말 재촉");
        else if (difference >= horseDifferenceWarning)
            AppendLine("오른말 빠름 - 왼말 재촉");
        else if (difference <= -horseDifferenceWarning)
            AppendLine("왼말 빠름 - 오른말 재촉");
    }

    private void AppendLine(string text)
    {
        if (alert.Length > 0) alert.Append('\n');
        alert.Append(text);
    }
}

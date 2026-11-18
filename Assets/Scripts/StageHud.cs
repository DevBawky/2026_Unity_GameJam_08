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
    private PlayerTiming player;
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
    [SerializeField] private Image distanceFill;

    [Header("Warning panel")]
    [SerializeField] private TMP_Text statusAlert;
    [SerializeField] private GameObject statusPanel;
    [SerializeField, Range(0f, 1f)] private float durabilityWarningLevel = 0.3f;

    private readonly StringBuilder alert = new();
    private SpriteRenderer alertTire;
    private string alertTireNumber;
    private int lastStatusKey = int.MinValue;
    private int lastWave = -1;
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
        restartButton.SetActive((stations.Failed || stations.ChapterComplete) && stations.IsHost);
        upgradePanel.SetActive(stations.ChoosingUpgrade);
        UpdateAlert();
        if (stations.ChoosingUpgrade || stations.ChapterComplete) { UpdateStatusText(); return; }

        int key = HashCode.Combine(stations.Failed, stations.ChoosingUpgrade, stations.StageNumber,
            player.CurrentAction, stations.DepositedShells, stations.IsLoaded);
        if (key == lastStatusKey) return;
        lastStatusKey = key;
        UpdateStatusText();
    }

    private void UpdateIndicators()
    {
        healthFill.fillAmount = Mathf.Clamp01(stations.Durability / stations.Capacity);
        repairProgress.SetActive(stations.WheelDetached && !stations.Failed && !stations.ChoosingUpgrade);
        repairFill.fillAmount = Mathf.Clamp01(stations.WheelRepairSteps / (float)CartStations.WheelRepairStepsNeeded);
        float progress = Mathf.Clamp01(stations.StageDistance / Mathf.Max(1f, stations.StageGoal));
        if (distanceFill != null) distanceFill.fillAmount = progress;
        distanceMarker.anchorMin = distanceMarker.anchorMax = new Vector2(progress, 0.5f);
        distanceMarker.anchoredPosition = Vector2.zero;
        int percent = stations.Endless.Value ? Mathf.FloorToInt(stations.StageDistance) : Mathf.CeilToInt(Mathf.Max(0,stations.StageGoal-stations.StageDistance));
        if (distanceLabel != null && (percent != lastProgressPercent || lastWave != stations.Waves.Value))
        {
            lastProgressPercent = percent;
            lastWave = stations.Waves.Value;
            distanceLabel.text = $"{percent} m   ·   W{stations.Waves.Value}";
        }
    }

    private void UpdateStatusText()
    {
        if (stations.Failed)
        {
            stationStatus.text = stations.FailureReason;
            return;
        }
        if (stations.ChapterComplete || stations.ChoosingUpgrade)
        {
            bool reporting=stations.Report.Value<0;
            stationStatus.text=$"{(stations.ChapterComplete ? "챕터 완료" : reporting ? "마왕에게 보고" : stations.KingReply.Value.ToString())}  ·  왕자 {stations.PrincePoints.Value}점  공주 {stations.PrincessPoints.Value}점  ·  {stations.Affection.Value} AP";
            for(int i=0;i<upgradeButtons.Length;i++)
            {
                bool visible=!reporting || i<3;
                upgradeButtons[i].gameObject.SetActive(visible);
                if(!visible) continue;
                upgradeLabels[i].text=reporting ? CartStations.Reports[i] : i==CartStations.Departure ? "다음 구간 출발" : $"{CartRules.Active.GetCard(i).title}  {CartRules.Active.GetCard(i).price} AP";
                upgradeButtons[i].interactable=reporting ? stations.IsHost : i==CartStations.Departure || stations.CanBuy(i);
            }
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
        if (stations.Durability <= durabilityWarningLevel * CartStations.MaxDurability)
            AppendLine("마차 내구도 위험");

        CartMotion cart = stations.Cart;
        if (Mathf.Approximately(Mathf.Abs(cart.Lane), cart.LateralLimit))
            AppendLine(cart.Lane > 0 ? "위쪽 끝 차선" : "아래쪽 끝 차선");
    }

    private void AppendLine(string text)
    {
        if (alert.Length > 0) alert.Append('\n');
        alert.Append(text);
    }
}

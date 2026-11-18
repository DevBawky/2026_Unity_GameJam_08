using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

// 화면 HUD. 스테이지 상태, 내구도, 진행 거리, 재시작 버튼과 정비 선택을 표시한다.
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
    [SerializeField] private RectTransform distanceMarker;
    [SerializeField] private TMP_Text distanceLabel;
    [SerializeField] private Image distanceFill;

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
            string headline=stations.ChapterComplete ? "챕터 완료" : reporting ? "마왕에게 보고" : stations.KingReply.Value.ToString();
            stationStatus.text=$"{LocalizationTable.T(headline)}  ·  {LocalizationTable.F("왕자 {0}점  공주 {1}점", stations.PrincePoints.Value, stations.PrincessPoints.Value)}  ·  {stations.Affection.Value} AP";
            for(int i=0;i<upgradeButtons.Length;i++)
            {
                bool visible=!reporting || i<3;
                upgradeButtons[i].gameObject.SetActive(visible);
                if(!visible) continue;
                upgradeLabels[i].text=reporting ? LocalizationTable.T(CartStations.Reports[i]) : i==CartStations.Departure ? LocalizationTable.T("다음 구간 출발") : $"{LocalizationTable.T(CartRules.Active.GetCard(i).title)}  {CartRules.Active.GetCard(i).price} AP";
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

}

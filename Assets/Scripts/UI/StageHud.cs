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
    [SerializeField] private TMP_Text cartStatusLabel;
    [SerializeField] private RectTransform distanceMarker;
    [SerializeField] private TMP_Text distanceLabel;
    [SerializeField] private Image distanceFill;

    private int lastStatusKey = int.MinValue;
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
        bool shopOpen = stations.IsSpawned && stations.ChoosingUpgrade && stations.ResultsVisible;
        if (upgradePanel.activeSelf != shopOpen) upgradePanel.SetActive(shopOpen);
        if (NetworkPlayer.LocalPlayer == null || !stations.IsSpawned) return;
        player=NetworkPlayer.LocalPlayer.Timing;
        UpdateIndicators();
        restartButton.SetActive(stations.Failed && stations.IsHost && stations.ResultsVisible);
        if (stations.Failed && !stations.ResultsVisible) return;
        if (stations.ChapterComplete) return;
        if (shopOpen)
        {
            // 정비 화면 문구·버튼은 보고·구매·점수가 바뀔 때만 다시 만든다.
            int shopKey = HashCode.Combine(true, stations.Report.Value, stations.ShopRound.Value,
                stations.Affection.Value, stations.PrincePoints.Value, stations.PrincessPoints.Value, stations.IsHost,
                stations.KingReply.Value.GetHashCode());
            if (shopKey == lastStatusKey) return;
            lastStatusKey = shopKey;
            UpdateStatusText();
            return;
        }

        int key = HashCode.Combine(stations.Failed, stations.ChoosingUpgrade, stations.StageNumber,
            player.CurrentAction, stations.DepositedShells, stations.IsLoaded);
        if (key == lastStatusKey) return;
        lastStatusKey = key;
        UpdateStatusText();
    }

    private void UpdateIndicators()
    {
        healthFill.fillAmount = Mathf.Clamp01(stations.Durability / stations.Capacity);
        if (cartStatusLabel != null)
        {
            int defense = Mathf.RoundToInt(Mathf.Clamp01(1f - stations.ImpactMultiplier) * 100f);
            string status = LocalizationTable.F("내구도 {0}/{1} · 충격 방어 {2}%", Mathf.CeilToInt(stations.Durability),
                Mathf.RoundToInt(stations.Capacity), defense);
            if (cartStatusLabel.text != status) cartStatusLabel.SetText(status);
        }
        float distanceLeft = Mathf.Max(0f, stations.StageGoal - stations.StageDistance);
        float remaining = Mathf.Clamp01(distanceLeft / Mathf.Max(1f, stations.StageGoal));
        if (distanceFill != null) distanceFill.fillAmount = remaining;
        distanceMarker.anchorMin = distanceMarker.anchorMax = new Vector2(remaining, 0.5f);
        distanceMarker.anchoredPosition = Vector2.zero;
        if (distanceLabel != null)
        {
            string text = LocalizationTable.F("남은 거리 {0:N0} m", Mathf.CeilToInt(distanceLeft));
            if (distanceLabel.text != text) distanceLabel.SetText(text);
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
            bool reporting=stations.Report.Value<0;
            string headline=reporting ? "마왕에게 보고" : stations.KingReply.Value.ToString();
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

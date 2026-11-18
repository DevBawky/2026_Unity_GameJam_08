using System;
using UnityEngine;
using UnityEngine.UI;

// 화면 HUD. 스테이지 이름·상태 문구, 내구도·바퀴 수리·진행 거리 표시, 재시작 버튼, 업그레이드 선택 창.
// 상태 문구는 표시할 값이 바뀐 프레임에만 다시 만든다.
public sealed class StageHud : MonoBehaviour
{
    private static readonly string[] StageNames = { "THORNWOOD PASS", "ASHEN ROAD", "TWILIGHT BORDER" };

    [SerializeField] private CartStations stations;
    [SerializeField] private PlayerTiming player;
    [SerializeField] private Text stationStatus;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private Text[] upgradeLabels;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image repairFill;
    [SerializeField] private GameObject repairProgress;
    [SerializeField] private RectTransform distanceMarker;

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
        healthFill.fillAmount = Mathf.Clamp01(stations.Durability / CartStations.MaxDurability);
        repairProgress.SetActive(stations.WheelDetached && !stations.Failed && !stations.ChoosingUpgrade);
        repairFill.fillAmount = Mathf.Clamp01(stations.WheelRepairSteps / (float)CartStations.WheelRepairStepsNeeded);
        float progress = Mathf.Clamp01(stations.StageDistance / Mathf.Max(1f, stations.StageGoal));
        distanceMarker.anchorMin = distanceMarker.anchorMax = new Vector2(progress, 0.5f);
        distanceMarker.anchoredPosition = Vector2.zero;
        restartButton.SetActive(stations.Failed);
        upgradePanel.SetActive(stations.ChoosingUpgrade);

        int key = HashCode.Combine(stations.Failed, stations.ChoosingUpgrade, stations.StageNumber,
            player.CurrentAction, stations.DepositedShells, stations.IsLoaded);
        if (key == lastStatusKey) return;
        lastStatusKey = key;
        UpdateStatusText();
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
}

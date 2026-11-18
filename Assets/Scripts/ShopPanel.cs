using TMPro;
using UnityEngine;

// 상점(그레이박스): 스테이지 목표에 도달해 보고까지 끝나면 위·아래 선반에 상품 3개씩을 보여 준다.
// 추후 변경점: 상품 슬롯은 아직 구매와 연결하지 않았다. 출발 버튼만 기존 정비 흐름(Departure)으로 보낸다.
public sealed class ShopPanel : MonoBehaviour
{
    [SerializeField] private CartStations stations;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text apLabel;

    private int shownAp = -1;

    private void Awake() => panel.SetActive(false);

    private void Update()
    {
        bool open = stations.IsSpawned && stations.ChoosingUpgrade && stations.Report.Value >= 0;
        if (panel.activeSelf != open) panel.SetActive(open);
        if (!open) return;
        int ap = stations.Affection.Value;
        if (ap == shownAp) return;
        shownAp = ap;
        apLabel.text = LocalizationTable.F("보유 {0} AP", ap);
    }

    public void Depart() => stations.ChooseUpgrade(CartStations.Departure);
}

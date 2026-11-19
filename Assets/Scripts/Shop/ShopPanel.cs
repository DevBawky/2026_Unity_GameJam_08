using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

// 상점: 스테이지 목표에 도달해 보고까지 끝나면 위·아래 선반에 상품 3개씩(무작위 6개)을 보여 준다.
// 칸을 눌러 고르면 아래에 설명이 뜨고, 구매 버튼으로 공동 AP를 써서 산다(서버가 확인).
public sealed class ShopPanel : MonoBehaviour
{
    [SerializeField] private CartStations stations;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text apLabel, descriptionLabel;
    [SerializeField] private ShopSlotView[] slots;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyLabel;

    private int selected = -1;
    private int shownKey;

    private void Awake()
    {
        panel.SetActive(false);
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            slots[i].Button.onClick.AddListener(() => Select(index));
            slots[i].HeartButton.onClick.AddListener(() => NetworkPlayer.LocalPlayer?.SendShopHeart(index));
        }
    }

    private void Update()
    {
        bool open = stations.IsSpawned && stations.ChoosingUpgrade && stations.Report.Value >= 0;
        if (panel.activeSelf != open)
        {
            panel.SetActive(open);
            selected = -1;
            shownKey = 0;
        }
        if (!open) return;

        // 진열·AP·선택이 바뀐 프레임에만 다시 그린다.
        int key = HashCode.Combine(stations.ShopPoints, selected, stations.ShopRound.Value, LocalizationTable.Current);
        for (int i = 0; i < stations.ShopOffer.Count; i++) key = HashCode.Combine(key, stations.ShopOffer[i]);
        for (int i = 0; i < stations.ShopHearts.Count; i++) key = HashCode.Combine(key, stations.ShopHearts[i]);
        if (key == shownKey) return;
        shownKey = key;
        Refresh();
    }

    private void Refresh()
    {
        apLabel.text = LocalizationTable.F("보유 {0} AP", stations.ShopPoints);
        bool canBuy = stations.CanBuyShopItems(NetworkPlayer.LocalPlayer);
        for (int i = 0; i < slots.Length; i++)
            slots[i].Show(ItemAt(i), i == selected, i < stations.ShopHearts.Count ? stations.ShopHearts[i] : 0, !canBuy);

        var item = ItemAt(selected);
        if (item == null) selected = -1;
        descriptionLabel.text = item != null ? LocalizationTable.T(item.description) : LocalizationTable.T("상품을 골라 주세요.");
        buyButton.interactable = item != null && canBuy && stations.ShopPoints >= item.price;
        buyLabel.text = LocalizationTable.T(!canBuy ? "1등만 구매" : item != null && stations.ShopPoints < item.price ? "AP 부족" : "구매");
    }

    private ShopItem ItemAt(int slot) =>
        slot >= 0 && slot < stations.ShopOffer.Count && stations.Catalog != null ? stations.Catalog.Get(stations.ShopOffer[slot]) : null;

    private void Select(int slot)
    {
        selected = slot;
        shownKey = 0;
    }

    public void Buy()
    {
        if (selected >= 0) NetworkPlayer.LocalPlayer?.SendShopBuy(selected);
    }

    public void Depart() => stations.ChooseUpgrade(CartStations.Departure);
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상점 선반의 칸 하나: 아이콘, 그 밑에 이름과 가격. 누르면 ShopPanel이 선택한다.
public sealed class ShopSlotView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background, icon;
    [SerializeField] private TMP_Text nameLabel, priceLabel;
    [SerializeField] private Color normalColor = new(.95f, .95f, .95f), selectedColor = new(1f, .86f, .5f), soldColor = new(.7f, .7f, .7f);

    private Sprite placeholderIcon;

    public Button Button => button;

    private void Awake() => placeholderIcon = icon.sprite;

    public void Show(ShopItem item, bool selected)
    {
        bool sold = item == null;
        background.color = sold ? soldColor : selected ? selectedColor : normalColor;
        icon.sprite = !sold && item.icon != null ? item.icon : placeholderIcon;
        icon.color = !sold && item.icon != null ? Color.white : new Color(.6f, .6f, .6f);
        nameLabel.text = sold ? LocalizationTable.T("판매 완료") : LocalizationTable.T(item.displayName);
        priceLabel.text = sold ? "" : LocalizationTable.F("{0} AP", item.price);
        button.interactable = !sold;
    }
}

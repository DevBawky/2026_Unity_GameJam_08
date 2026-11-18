using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상점 선반의 칸 하나: 아이콘, 그 밑에 이름과 가격. 누르면 ShopPanel이 선택한다.
// 오른쪽 위 하트는 구매자가 아닌 사람이 누르고, 옆 숫자는 누른 횟수다. 하트 묶음은 오른쪽 위에 붙은 가로 레이아웃이라 숫자가 길어지면 하트가 왼쪽으로 밀린다.
public sealed class ShopSlotView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background, icon;
    [SerializeField] private TMP_Text nameLabel, priceLabel;
    [SerializeField] private Color normalColor = new(.95f, .95f, .95f), selectedColor = new(1f, .86f, .5f), soldColor = new(.7f, .7f, .7f);
    [SerializeField] private Button heartButton;
    [SerializeField] private Image heartImage;
    [SerializeField] private TMP_Text heartCount;
    [SerializeField] private Sprite emptyHeart, filledHeart;

    private Sprite placeholderIcon;

    public Button Button => button;
    public Button HeartButton => heartButton;

    private void Awake() => placeholderIcon = icon.sprite;

    public void Show(ShopItem item, bool selected, int hearts, bool canHeart)
    {
        bool sold = item == null;
        background.color = sold ? soldColor : selected ? selectedColor : normalColor;
        icon.sprite = !sold && item.icon != null ? item.icon : placeholderIcon;
        icon.color = !sold && item.icon != null ? Color.white : new Color(.6f, .6f, .6f);
        nameLabel.text = sold ? LocalizationTable.T("판매 완료") : LocalizationTable.T(item.displayName);
        priceLabel.text = sold ? "" : LocalizationTable.F("{0} AP", item.price);
        button.interactable = !sold;
        heartImage.sprite = hearts > 0 ? filledHeart : emptyHeart;
        heartCount.text = hearts.ToString();
        heartButton.interactable = canHeart && !sold;
    }
}

using TMPro;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public sealed class CannonComboView : MonoBehaviour
{
    [SerializeField] private RectTransform card;
    [SerializeField] private TMP_Text count;
    private Canvas canvas;
    private int shown;

    private void Awake() => canvas = GetComponent<Canvas>();

    private void LateUpdate()
    {
        int combo = CartStations.Running ? CartStations.Instance.CannonCombo.Value : 0;
        card.gameObject.SetActive(combo >= 2);
        if (combo < 2) return;
        if (shown != combo)
        {
            shown = combo;
            count.SetText("×{0}", combo);
        }
        Rect safe = GameplayHudLayout.WebSafeArea;
        float width = Mathf.Min(245f, safe.width * .2f, safe.height * .38f);
        Vector2 size = new(width, width * 59f / 71f);
        Vector2 center = new(safe.xMax - size.x * .5f, safe.yMax - Mathf.Clamp(safe.height * .13f, 44f, 86f) - size.y * .5f);
        GameplayHudLayout.PlacePixels(card, center, size, canvas);
        count.fontSize = width * .23f / canvas.scaleFactor;
    }
}

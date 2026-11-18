using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CannonPadView : MonoBehaviour
{
    [SerializeField] private Image marker;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Sprite cannon, reload;
    private StationAction? shown;
    private void LateUpdate()
    {
        var game = CartStations.Instance;
        if (game == null || shown == game.CannonAction) return;
        shown = game.CannonAction;
        marker.sprite = shown == StationAction.Reload ? reload : cannon;
        label.SetText(shown == StationAction.Reload ? "장전" : shown == StationAction.Cannon ? "포수" : "포신 청소");
    }
}

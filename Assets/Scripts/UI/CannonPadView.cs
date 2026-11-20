using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 대포 발판 표시: 대포 상태(청소·장전·포수)에 맞춰 아이콘과 라벨을 바꾼다.
public sealed class CannonPadView : MonoBehaviour
{
    [SerializeField] private Image marker;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Sprite cannon, reload;
    [SerializeField] private bool upper;
    private StationAction? shown;
    private void LateUpdate()
    {
        var game = CartStations.Instance;
        if (game == null) return;
        var action = TimingArea.CannonRole(game.CannonActionAt(upper));
        if (shown == action) return;
        shown = action;
        marker.sprite = shown == StationAction.Reload ? reload : cannon;
        label.SetText(LocalizationTable.T(shown == StationAction.Reload ? "장전" : shown == StationAction.Cannon ? "포수" : "포신 청소"));
    }
}

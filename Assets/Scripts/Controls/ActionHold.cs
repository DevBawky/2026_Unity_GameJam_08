using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ActionHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private NetworkPlayer heldBy;
    private int pointerId;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!GameplayControlMode.IsMobile || heldBy != null || eventData.button != PointerEventData.InputButton.Left)
            return;
        if (TryGetComponent<UnityEngine.UI.Button>(out var button) && !button.IsInteractable()) return;
        var local = NetworkPlayer.LocalPlayer;
        if (local == null || !local.Local) return;
        heldBy = local;
        pointerId = eventData.pointerId;
        heldBy.TouchHeld = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId && eventData.button == PointerEventData.InputButton.Left)
            Release();
    }

    private void OnDisable() => Release();

    private void Release()
    {
        if (heldBy != null) heldBy.TouchHeld = false;
        heldBy = null;
    }
}

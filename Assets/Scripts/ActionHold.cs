using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ActionHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData e) { if(NetworkPlayer.LocalPlayer!=null) NetworkPlayer.LocalPlayer.TouchHeld=true; }
    public void OnPointerUp(PointerEventData e) => Release();
    private void OnDisable() => Release();
    private void Release() { if(NetworkPlayer.LocalPlayer!=null) NetworkPlayer.LocalPlayer.TouchHeld=false; }
}

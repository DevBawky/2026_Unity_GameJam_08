using UnityEngine;

public sealed class MobileInteractionUI : MonoBehaviour
{
    [SerializeField] private PlayerTiming timing;
    [SerializeField] private MobileJoystick movementJoystick;
    [SerializeField] private GameObject actionButton;

    [SerializeField] private GameObject exitButton;
    public void ExitStation() => NetworkPlayer.LocalPlayer?.Timing.ExitStation();
    public void TriggerInteraction()
    {
        if (NetworkPlayer.LocalPlayer!=null) NetworkPlayer.LocalPlayer.Timing.TriggerInteraction();
    }

    private void Update()
    {
        var local=NetworkPlayer.LocalPlayer;
        if (local==null) { actionButton.SetActive(false); exitButton.SetActive(false); return; }
        timing=local.Timing;
        bool mobile = movementJoystick != null && movementJoystick.IsMobileMode;
        if (movementJoystick != null)
            movementJoystick.SetInputActive(!local.Holding.Value);
        if (exitButton != null) exitButton.SetActive(mobile && timing.HasStationAccess);
        if (actionButton != null) actionButton.SetActive(mobile && timing != null && timing.CanInteract);
    }
}

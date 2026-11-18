using UnityEngine;
using UnityEngine.UI;

public sealed class MobileInteractionUI : MonoBehaviour
{
    [SerializeField] private PlayerTiming timing;
    [SerializeField] private MobileJoystick movementJoystick;
    [SerializeField] private GameObject actionButton;

    [SerializeField] private GameObject exitButton;
    [SerializeField] private MobileJoystick aimJoystick;
    [SerializeField] private Button actionControl;
    [SerializeField] private TMPro.TMP_Text actionLabel;
    public void ExitStation() => NetworkPlayer.LocalPlayer?.Timing.ExitStation();
    public void TriggerInteraction()
    {
        if (NetworkPlayer.LocalPlayer!=null) NetworkPlayer.LocalPlayer.Timing.TriggerInteraction();
    }

    private void Update()
    {
        var local=NetworkPlayer.LocalPlayer;
        if (local==null) { actionButton.SetActive(false); if (exitButton != null) exitButton.SetActive(false); return; }
        timing=local.Timing;
        bool mobile = movementJoystick != null && movementJoystick.IsMobileMode;
        if (actionControl != null)
        {
            bool aiming = timing.IsCannonFocus;
            aimJoystick.gameObject.SetActive(aiming);
            if (aiming && aimJoystick.IsMobileMode != mobile) aimJoystick.SetMobileMode(mobile);
            aimJoystick.SetInputActive(aiming);
            movementJoystick.SetInputActive(!aiming && !local.Holding.Value);
            movementJoystick.ShowDesktopPreview(!aiming);
            exitButton.SetActive(timing.HasStationAccess);
            actionButton.SetActive(true);
            actionControl.interactable = CartStations.Running && timing.CanInteract;
            var guide = CartStations.Running ? WorkGuide.For(local, CartStations.Instance) : default;
            actionLabel.text = guide.Action ?? LocalizationTable.T("행동");
            actionLabel.color = actionControl.interactable ? new Color32(64, 45, 37, 255) : new Color32(245, 240, 224, 255);
            var colors = actionControl.colors;
            if (guide.Color.a > 0f && colors.normalColor != guide.Color)
            {
                colors.normalColor = colors.selectedColor = guide.Color;
                actionControl.colors = colors;
            }
            return;
        }
        if (movementJoystick != null)
            movementJoystick.SetInputActive(!local.Holding.Value);
        if (exitButton != null) exitButton.SetActive(mobile && timing.HasStationAccess);
        if (actionButton != null) actionButton.SetActive(mobile && timing != null && timing.CanInteract);
    }
}

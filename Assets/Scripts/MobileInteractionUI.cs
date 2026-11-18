using UnityEngine;

public sealed class MobileInteractionUI : MonoBehaviour
{
    [SerializeField] private PlayerTiming timing;
    [SerializeField] private MobileJoystick movementJoystick;
    [SerializeField] private GameObject actionButton;
    [SerializeField] private MobileJoystick aimJoystick;

    private void OnEnable() { if (aimJoystick != null) aimJoystick.Released += OnAimReleased; }
    private void OnDisable() { if (aimJoystick != null) aimJoystick.Released -= OnAimReleased; }
    private void OnAimReleased()
    {
        if (timing != null && !timing.IsCannonFocus) timing.TriggerInteraction();
    }
    public void TriggerInteraction()
    {
        if (NetworkPlayer.LocalPlayer!=null) NetworkPlayer.LocalPlayer.Timing.TriggerInteraction();
    }

    private void Update()
    {
        var local=NetworkPlayer.LocalPlayer;
        if (local==null) { actionButton.SetActive(false); aimJoystick.gameObject.SetActive(false); return; }
        timing=local.Timing;
        bool mobile = movementJoystick != null && movementJoystick.IsMobileMode;
        if (actionButton != null) actionButton.SetActive(mobile && timing != null && timing.CanInteract);
        if (aimJoystick != null)
        {
            bool aiming = mobile && timing != null && (timing.IsCannonFocus || timing.CanInteract);
            if (aimJoystick.gameObject.activeSelf != aiming) aimJoystick.gameObject.SetActive(aiming);
            if (aiming && aimJoystick.IsMobileMode != mobile) aimJoystick.SetMobileMode(mobile);
        }
    }
}

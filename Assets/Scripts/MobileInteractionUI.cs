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

    private void Update()
    {
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

using UnityEngine;

[DefaultExecutionOrder(-20)]
public sealed class MobileInteractionUI : MonoBehaviour
{
    [SerializeField] private MobileJoystick movementJoystick;
    [SerializeField] private GameObject actionButton;
    [SerializeField] private GameObject exitButton;
    [SerializeField] private MobileJoystick aimJoystick;
    [SerializeField] private UnityEngine.UI.Button actionControl;
    [SerializeField] private TMPro.TMP_Text actionLabel;
    [SerializeField] private GameObject cannonCleaningArt;

    public void ExitStation() => NetworkPlayer.LocalPlayer?.Timing.ExitStation();

    public void TriggerInteraction() => NetworkPlayer.LocalPlayer?.Timing.TriggerInteraction();

#if UNITY_EDITOR
    private void Update() => GameplayControlMode.UpdateEditorMode();
#endif

    private void LateUpdate()
    {
        var local = NetworkPlayer.LocalPlayer;
        bool show = GameplayControlMode.IsMobile && local != null && local.Local && CartStations.Running;
        var timing = show ? local.Timing : null;
        bool aiming = show && timing.IsCannonFocus;

        if (movementJoystick != null)
        {
            movementJoystick.gameObject.SetActive(show && !aiming);
            movementJoystick.SetInputActive(show && !local.Holding.Value);
        }
        if (aimJoystick != null) aimJoystick.gameObject.SetActive(aiming);
        if (exitButton != null) exitButton.SetActive(show && timing.HasStationAccess);
        if (actionButton != null) actionButton.SetActive(show);
        if (!show) return;

        bool canInteract = timing.CanInteract;
        var guide = WorkGuide.For(local, CartStations.Instance);
        if (actionControl != null) actionControl.interactable = canInteract;
        bool cleaning = timing.HasStationAccess && timing.CurrentAction == StationAction.CannonCleaning;
        if (cannonCleaningArt != null && cannonCleaningArt.activeSelf != cleaning)
            cannonCleaningArt.SetActive(cleaning);
        if (actionLabel != null)
        {
            actionLabel.text = guide.Action ?? LocalizationTable.T("행동");
            actionLabel.color = canInteract && guide.Color.a > 0f ? guide.Color : new Color32(245, 240, 224, 255);
        }
    }
}

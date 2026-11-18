using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class CannonAim : MonoBehaviour
{
    [SerializeField] private Transform muzzle;
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private SpriteRenderer crosshair;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private MobileJoystick aimJoystick;
    [SerializeField, Min(1f)] private float lineLength = 8f;
    private PlayerTiming timing;
    private Vector2 direction = Vector2.right;
    public Vector2 Direction => direction;
    public Transform Muzzle => muzzle;
    public float LineLength => lineLength;

    private void Awake() { timing = GetComponent<PlayerTiming>(); if (gameCamera == null) gameCamera = Camera.main; }
    private void LateUpdate()
    {
        bool aiming = timing != null && timing.IsCannonFocus && muzzle != null;
        if (aimLine != null) aimLine.enabled = aiming;
        if (crosshair != null) crosshair.enabled = aiming;
        if (!aiming) return;
        if (aimJoystick != null && aimJoystick.gameObject.activeInHierarchy && aimJoystick.Direction.sqrMagnitude > 0.04f)
            SetDirection(aimJoystick.Direction);
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            Vector3 screen = Mouse.current.position.ReadValue();
            screen.z = Mathf.Abs(muzzle.position.z - gameCamera.transform.position.z);
            SetDirection(gameCamera.ScreenToWorldPoint(screen) - muzzle.position);
        }
        Vector3 origin = muzzle.position;
        Vector3 endpoint = origin + (Vector3)direction * lineLength;
        if (aimLine != null)
        {
            aimLine.SetPosition(0, origin);
            aimLine.SetPosition(1, origin + (Vector3)direction * (lineLength * 0.95f));
        }
        if (crosshair != null) crosshair.transform.position = endpoint;
    }
    private void SetDirection(Vector2 value)
    {
        if (value.sqrMagnitude < 0.001f) return;
        // Aim from the cart towards the ground on the right.
        value.x = Mathf.Max(value.x, 0.05f);
        direction = value.normalized;
    }
}

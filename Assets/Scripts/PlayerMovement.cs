using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField] private MobileJoystick joystick;

    private CartStations stations;
    private void Awake() { stations = GetComponent<CartStations>(); }

    private void Update()
    {
        if (stations != null && stations.IsHoldingShell) return;
        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (joystick == null || !joystick.IsMobileMode))
        {
            input.x += (keyboard.dKey.isPressed ? 1f : 0f)
                     - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y += (keyboard.wKey.isPressed ? 1f : 0f)
                     - (keyboard.sKey.isPressed ? 1f : 0f);
        }

        if (Gamepad.current != null) input += Gamepad.current.leftStick.ReadValue();
        input = Vector2.ClampMagnitude(input, 1f);
        transform.position += new Vector3(input.x, input.y, 0f) * (moveSpeed * (stations != null && stations.IsCarrying ? 0.3f : 1f) * Time.deltaTime);
    }
}

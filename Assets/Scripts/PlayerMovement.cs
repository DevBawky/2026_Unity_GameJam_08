using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField] private MobileJoystick joystick;

    private void Update()
    {
        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (joystick == null || !joystick.IsMobileMode))
        {
            input.x += (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                     - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            input.y += (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                     - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        }

        input = Vector2.ClampMagnitude(input, 1f);
        transform.position += new Vector3(input.x, input.y, 0f) * (moveSpeed * Time.deltaTime);
    }
}

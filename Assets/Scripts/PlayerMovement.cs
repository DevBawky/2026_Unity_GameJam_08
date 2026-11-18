using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 이동. 모바일 조이스틱·WASD·게임패드. 포탄을 들면 느려지고, 버티기 판정 중엔 멈춘다.
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [Tooltip("포탄을 들고 있을 때 이동 속도 배율")]
    [SerializeField, Range(0f, 1f)] private float carrySpeedMultiplier = 0.3f;
    [SerializeField] private MobileJoystick joystick;

    private CartStations stations;

    private void Awake() => stations = GetComponent<CartStations>();

    private void Update()
    {
        if (stations.IsHoldingShell) return;
        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (joystick == null || !joystick.IsMobileMode))
        {
            input.x += (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y += (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        }
        if (Gamepad.current != null) input += Gamepad.current.leftStick.ReadValue();

        input = Vector2.ClampMagnitude(input, 1f);
        float speed = moveSpeed * (stations.IsCarrying ? carrySpeedMultiplier : 1f);
        transform.position += new Vector3(input.x, input.y, 0f) * (speed * Time.deltaTime);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 이동. 모바일 조이스틱·WASD·게임패드. 마차 몸체 밖으로는 못 나간다.
// 포탄을 들면 느려지고, 버티기 판정 중엔 멈춘다.
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [Tooltip("포탄을 들고 있을 때 이동 속도 배율")]
    [SerializeField, Range(0f, 1f)] private float carrySpeedMultiplier = 0.3f;
    [SerializeField] private MobileJoystick joystick;

    [SerializeField] private CartStations stations;
    private NetworkPlayer actor;

    private void Awake() => actor = GetComponent<NetworkPlayer>();

    private void Update()
    {
        if (actor == null || !actor.Local || !CartStations.Running || actor.Holding.Value) return;
        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (joystick == null || !joystick.IsMobileMode))
        {
            input.x += (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y += (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        }
        if (Gamepad.current != null) input += Gamepad.current.leftStick.ReadValue();

        input = Vector2.ClampMagnitude(input, 1f);
        float speed = moveSpeed * (actor.Carrying.Value ? carrySpeedMultiplier : 1f);
        Vector3 desired = transform.position + new Vector3(input.x, input.y, 0f) * (speed * Time.deltaTime);
        transform.position = ClampToCartBody(desired);
    }

    private Vector3 ClampToCartBody(Vector3 desired)
    {
        SpriteRenderer body = stations.CartBody;
        if (body == null || body.sprite == null) return desired;

        Transform bodyTransform = body.transform;
        Bounds bodyBounds = body.sprite.bounds;
        Vector3 local = bodyTransform.InverseTransformPoint(desired);
        local.x = Mathf.Clamp(local.x, bodyBounds.min.x, bodyBounds.max.x);
        local.y = Mathf.Clamp(local.y, bodyBounds.min.y, bodyBounds.max.y);
        Vector3 result = bodyTransform.TransformPoint(local);
        result.z = desired.z;
        return result;
    }
}

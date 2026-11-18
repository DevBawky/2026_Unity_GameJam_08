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
    [SerializeField] private RectTransform deckStairs, cellarStairs;
    private bool stairLocked;

    private void CrossStairs()
    {
        var stairs = actor.InCellar ? cellarStairs : deckStairs;
        if (stairs == null) return;
        bool onStairs = stairs.rect.Contains(stairs.InverseTransformPoint(transform.position));
        if (onStairs && !stairLocked && !stations.IsCarryingChild(actor))
        {
            var destination = actor.InCellar ? deckStairs : cellarStairs;
            transform.position += destination.position - stairs.position;
        }
        stairLocked = onStairs;
    }

    private void Awake() => actor = GetComponent<NetworkPlayer>();

    private void Update()
    {
        if (actor == null || !actor.Local || !CartStations.Running || actor.Holding.Value) return;
        CrossStairs();

        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (joystick == null || !joystick.IsMobileMode))
        {
            input.x += (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y += (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        }
        if (actor.SecondLocal) input = keyboard == null ? Vector2.zero : new Vector2((keyboard.rightArrowKey.isPressed?1:0)-(keyboard.leftArrowKey.isPressed?1:0),(keyboard.upArrowKey.isPressed?1:0)-(keyboard.downArrowKey.isPressed?1:0));
        else if (Gamepad.current != null) input += Gamepad.current.leftStick.ReadValue();
        // At the two aiming seats the movement stick controls aim. Other work areas are walk-through.
        if (actor.Timing.HasStationAccess && !actor.Carrying.Value
            && (actor.Timing.IsAt(StationAction.Driver) || actor.Timing.IsAt(StationAction.Cannon)))
        {
            if (actor.Timing.IsAt(StationAction.Driver) && input.sqrMagnitude>0.01f) actor.DriverAim.Value = Vector2.ClampMagnitude(input,1f);
            return;
        }

        input = Vector2.ClampMagnitude(input, 1f);
        float speed = moveSpeed * (actor.Carrying.Value ? carrySpeedMultiplier * stations.PorterMultiplier : actor.SpareWheel.Value ? 2f/3f : 1f);
        Vector3 desired = transform.position + new Vector3(input.x, input.y, 0f) * (speed * Time.deltaTime);
        if (!actor.InCellar && input.sqrMagnitude > 0f)
        {
            desired = SlidePastChild(desired, stations.Prince.Value);
            desired = SlidePastChild(desired, stations.Princess.Value);
        }
        transform.position = ClampToCartBody(desired);
    }

    private Vector3 SlidePastChild(Vector3 desired, RiderState child)
    {
        if (child.Pose != RiderPose.Wandering) return desired;
        Vector2 center = stations.Cart.transform.TransformPoint(child.Position);
        Vector2 previous = (Vector2)transform.position - center;
        Vector2 next = (Vector2)desired - center;
        const float clearance = 0.35f;
        if (next.sqrMagnitude >= clearance * clearance || next.sqrMagnitude >= previous.sqrMagnitude) return desired;
        Vector2 normal = previous.normalized;
        Vector2 motion = desired - transform.position;
        motion -= normal * Mathf.Min(0f, Vector2.Dot(motion, normal));
        return transform.position + (Vector3)motion;
    }

    private Vector3 ClampToCartBody(Vector3 desired)
    {
        SpriteRenderer body = stations.CartBody;
        if (body == null || body.sprite == null) return desired;

        Transform bodyTransform = body.transform;
        Bounds bodyBounds = body.sprite.bounds;
        Vector3 offset = actor.InCellar ? Vector3.up * NetworkPlayer.CellarOffset : Vector3.zero;
        Vector3 local = bodyTransform.InverseTransformPoint(desired - offset);
        local.x = Mathf.Clamp(local.x, bodyBounds.min.x, bodyBounds.max.x);
        local.y = Mathf.Clamp(local.y, bodyBounds.min.y, bodyBounds.max.y);
        Vector3 result = bodyTransform.TransformPoint(local) + offset;
        result.z = desired.z;
        return result;
    }
}

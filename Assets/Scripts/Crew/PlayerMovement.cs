using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 이동. 모바일 조이스틱·WASD·게임패드. 마차 몸체 밖으로는 못 나간다.
// 포탄을 들면 느려지고, 버티기 판정 중엔 멈춘다.
[DefaultExecutionOrder(-10)]
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
        Vector2 point = (Vector2)stairs.InverseTransformPoint(transform.position) - stairs.rect.center;
        Vector2 radius = stairs.rect.size * .5f;
        bool onStairs = new Vector2(point.x / radius.x, point.y / radius.y).sqrMagnitude <= 1f;
        if (onStairs && !stairLocked)
        {
            var destination = actor.InCellar ? deckStairs : cellarStairs;
            transform.position += destination.position - stairs.position;
        }
        stairLocked = onStairs;
    }

    private void Awake() => actor = GetComponent<NetworkPlayer>();

    private void Update()
    {
        if (actor == null || !actor.Local || !CartStations.Running) return;
        if (actor.Holding.Value || stations.HoldsStation(StationAction.Cannon, actor))
        {
            transform.position = ClampToCartBody(transform.position);
            return;
        }
        CrossStairs();

        Vector2 input = ReadInput();
        // The driver steers along the road using the vertical axis.
        if (actor.Timing.HasStationAccess && actor.Timing.IsAt(StationAction.Driver))
        {
            // 비상 제동 손잡이: E(2P는 오른쪽 Ctrl), 게임패드 X/□.
            var keyboard = Keyboard.current;
            bool brake = actor.SecondLocal ? keyboard?.rightCtrlKey.wasPressedThisFrame == true
                : keyboard?.eKey.wasPressedThisFrame == true || Gamepad.current?.buttonWest.wasPressedThisFrame == true;
            if (brake && stations.Owns(ShopPerk.EmergencyBrake)) actor.PullBrake();
            actor.DriverSteer.Value = Mathf.Clamp(input.y, -1f, 1f);
            transform.position = ClampToCartBody(transform.position);
            return;
        }
        if (actor.DriverSteer.Value != 0f) actor.DriverSteer.Value = 0f;

        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 desired = transform.position + new Vector3(input.x, input.y, 0f) * (WalkSpeed * Time.deltaTime);
        if (!actor.InCellar && input.sqrMagnitude > 0f)
        {
            desired = SlidePastChild(desired, stations.Prince.Value);
            desired = SlidePastChild(desired, stations.Princess.Value);
        }
        transform.position = ClampToCartBody(desired);
    }

    private Vector2 ReadInput()
    {
        if (actor.UsesMobileControls) return joystick != null ? joystick.Direction : Vector2.zero;

        var keyboard = Keyboard.current;
        if (actor.SecondLocal)
            return keyboard == null ? Vector2.zero : new Vector2(
                (keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed ? 1f : 0f),
                (keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed ? 1f : 0f));

        Vector2 input = Gamepad.current?.leftStick.ReadValue() ?? Vector2.zero;
        if (keyboard != null)
        {
            input.x += (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y += (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        }
        return input;
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

    public Vector2 StairPoint(bool cellar) => (cellar ? cellarStairs : deckStairs).position;

    private float WalkSpeed => moveSpeed * stations.WalkRate * (actor.Carrying.Value
        ? carrySpeedMultiplier * stations.PorterMultiplier
        : actor.SpareWheel.Value ? (stations.Owns(ShopPerk.LightSpareWheel) ? CartStations.LightSpareWheelRate : 110f/165f)
        : stations.IsCarryingChild(actor) ? 125f/165f
        : stations.CheerActive ? CartStations.CheerSpeedRate : 1f);

    public float BotTimeToDeckPoint(Vector2 point)
    {
        Vector2 pos = transform.position;
        float distance = actor.InCellar ? Vector2.Distance(pos, StairPoint(true)) +
            Vector2.Distance(StairPoint(false), point) : Vector2.Distance(pos, point);
        return distance / Mathf.Max(.01f, WalkSpeed);
    }

    public void WalkBot(Vector2 destination, bool cellar, float dt)
    {
        bool cross = actor.InCellar != cellar;
        if (cross) destination = StairPoint(actor.InCellar);
        Vector2 pos = transform.position;
        var next = Vector2.MoveTowards(pos, destination, WalkSpeed * dt);
        transform.position = ClampToCartBody(new Vector3(next.x, next.y, transform.position.z));
        if (cross && Vector2.Distance(next, destination) < .08f)
            transform.position += (Vector3)(StairPoint(cellar) - StairPoint(!cellar));
    }

    private Vector3 ClampToCartBody(Vector3 desired)
    {
        if (!actor.InCellar && stations.DeckArtwork != null)
        {
            Bounds deck = stations.DeckMovementBounds;
            desired.x = Mathf.Clamp(desired.x, deck.min.x, deck.max.x);
            desired.y = Mathf.Clamp(desired.y, deck.min.y, deck.max.y);
            return desired;
        }

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

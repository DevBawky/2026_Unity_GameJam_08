using UnityEngine;
using Unity.Netcode;

// 마차 속도·이동 거리·방향.
// 마차는 화면 x에 고정이고, 배경(WorldScroller)과 장애물(ObstacleSpawner)이 ScrollVelocity로 흘러간다.
// 채찍질은 목표 Y를 조정하고 마차가 그 위치를 부드럽게 따라간다.
public sealed class CartMotion : NetworkBehaviour
{
    [SerializeField] private float cruiseSpeed = 13f;
    [Tooltip("말 강화로 올릴 수 있는 최고 속도 = 기본 속도 + 이 값")]
    [SerializeField] private float maxBoostAboveBase = 5f;
    [SerializeField] private float acceleration = 3f;
    [Tooltip("말 강화 1회당 속도 증가")]
    [SerializeField] private float speedGain = 1f;
    public const float WhipSteeringStep = 0.25f;
    [Tooltip("일반 조향 시 목표 Y에 접근하는 비율(초당). 5이면 약 0.46초에 90% 도달합니다.")]
    [UnityEngine.Serialization.FormerlySerializedAs("laneChangeSpeed")]
    [SerializeField, Min(0.01f)] private float steeringResponse = 5f;
    private const float RoadUnitsPerMeter = 8f;

    [Header("Horse visual position")]
    [SerializeField] private Transform leftHorseVisual;
    [SerializeField] private Transform rightHorseVisual;
    [Tooltip("말 속도가 기본보다 1 빠를 때 그림이 앞으로 나가는 거리")]
    [SerializeField, Min(0f)] private float horseAdvancePerSpeed = 0.18f;
    [Tooltip("말 그림이 앞으로 나갈 수 있는 최대 거리")]
    [SerializeField, Min(0f)] private float maximumHorseAdvance = 1f;
    [Tooltip("말 그림이 목표 위치로 움직이는 속도")]
    [SerializeField, Min(0f)] private float horseVisualMoveSpeed = 3f;

    public NetworkVariable<CartMotionState> State = new();
    private float baseSpeed;
    private float maximumHorseSpeed;
    private float obstacleSlowFactor = 1f;
    private float obstacleSlowRemaining;
    private Vector3 leftHorseStart;
    private Vector3 rightHorseStart;

    public float BaseSpeed => baseSpeed;
    public float LateralLimit => CartStations.Instance != null ? 100f * CartStations.Instance.WorldPerWebUnit : 0f;

    public float Speed { get; private set; }
    public float TargetSpeed { get; private set; }
    public float Distance { get; private set; }
    public int LeftStep { get; private set; }
    public int RightStep { get; private set; }
    public float Lane { get; private set; }
    public float SpeedMultiplier { get; set; } = 1f;
    public float LeftHorseSpeed { get; private set; }
    public float RightHorseSpeed { get; private set; }
    public float SpeedPerHorseLevel => CartRules.Active.horses.amount;
    public Vector3 ScrollVelocity => Vector3.right * (Speed * RoadUnitsPerMeter * CartStations.Instance.WorldPerWebUnit);
    public float ScrollDistance { get; private set; }

    private void Awake()
    {
        if (leftHorseVisual != null) leftHorseStart = leftHorseVisual.localPosition;
        if (rightHorseVisual != null) rightHorseStart = rightHorseVisual.localPosition;
        ResetForStage(0);
    }

    public void ResetForStage(int horseLevel)
    {
        var stations = CartStations.Instance;
        float wheels = stations != null && stations.Owns(ShopPerk.LightWheels) ? CartStations.LightWheelsSpeedBonus : 0f;
        baseSpeed = cruiseSpeed + Mathf.Max(0, horseLevel) * SpeedPerHorseLevel + wheels;
        maximumHorseSpeed = Mathf.Min(baseSpeed + maxBoostAboveBase, 19f);
        LeftHorseSpeed = RightHorseSpeed = baseSpeed;
        TargetSpeed = baseSpeed;
        Speed = 0f;
        LeftStep = RightStep = 0;
        Lane = 0f;
        SpeedMultiplier = 1f;
        obstacleSlowFactor = 1f;
        obstacleSlowRemaining = 0f;
    }

    // 장애물 충돌 감속. 이미 더 강한 감속이 걸려 있으면 무시하고, 더 강하면 현재 속도도 그 비율만큼 바로 깎는다(웹판 규칙).
    public void ApplyObstacleSlow(float factor, float seconds)
    {
        if (factor > obstacleSlowFactor) return;
        Speed *= factor / obstacleSlowFactor;
        obstacleSlowRemaining = Mathf.Approximately(factor, obstacleSlowFactor) ? Mathf.Max(obstacleSlowRemaining, seconds) : seconds;
        obstacleSlowFactor = factor;
    }

    // 투석 명중처럼 순간적으로 속도만 깎는 충격.
    public void ApplySpeedHit(float multiplier) => Speed *= multiplier;

    public void BoostHorse(bool right,float strength=1f)
    {
        int struck = right ? RightStep : LeftStep;
        int opposite = right ? LeftStep : RightStep;
        bool returnToCenter = struck == 0 && opposite == 1;
        if (!returnToCenter) struck++;
        opposite = Mathf.Max(0, opposite - 1);
        LeftStep = right ? opposite : struck;
        RightStep = right ? struck : opposite;
        float targetY = Lane + (right ? WhipSteeringStep : -WhipSteeringStep);
        Lane = Mathf.Clamp(targetY, -LateralLimit, LateralLimit);

        if (right) RightHorseSpeed = Mathf.Min(maximumHorseSpeed, RightHorseSpeed + speedGain*strength);
        else LeftHorseSpeed = Mathf.Min(maximumHorseSpeed, LeftHorseSpeed + speedGain*strength);
    }

    // 출발용 당근 묶음: 기본 속도의 일정 비율로 출발한다.
    public void Kickstart(float fraction) => Speed = Mathf.Max(Speed, baseSpeed * fraction);

    public void ResetHorsePace(bool right)
    {
        if (right) RightHorseSpeed = baseSpeed; else LeftHorseSpeed = baseSpeed;
    }
    public void Advance(float dt)
    {
        if (!IsSpawned || !IsServer || !CartStations.Running) return;
        if (obstacleSlowRemaining > 0f)
        {
            obstacleSlowRemaining -= dt;
            if (obstacleSlowRemaining <= 0f) obstacleSlowFactor = 1f;
        }
        float stageSpeedLimit = CartStations.Instance.SpeedLimit;
        var stations = CartStations.Instance;
        int direction = stations.RunawayDirection;
        float cruise = Mathf.Min(stageSpeedLimit, (LeftHorseSpeed + RightHorseSpeed) * .5f);
        if (direction != 0) cruise += baseSpeed * .18f;
        TargetSpeed = cruise * SpeedMultiplier * EnemyRope.CartSpeedMultiplier * obstacleSlowFactor * stations.FatigueRate;
        Speed = Mathf.MoveTowards(Speed, TargetSpeed, acceleration * dt);
        Distance += Speed * dt;
        ScrollDistance += ScrollVelocity.x * dt;
        Vector3 position = transform.position;
        if (direction != 0)
        {
            Lane = direction * LateralLimit;
            position.y = Mathf.MoveTowards(position.y, Lane, CartStations.RunawaySteeringSpeed * stations.WorldPerWebUnit * dt);
        }
        else
        {
            position.y = PredictSteeringY(position.y, Lane, dt);
            if (Mathf.Abs(Lane - position.y) < 0.001f) position.y = Lane;
        }
        transform.position = position;
        UpdateHorseVisual(leftHorseVisual, leftHorseStart, LeftStep, dt);
        UpdateHorseVisual(rightHorseVisual, rightHorseStart, RightStep, dt);
    }

    internal float PredictSteeringY(float y, float targetY, float seconds) =>
        Mathf.Lerp(y, targetY, 1f - Mathf.Exp(-SteeringResponse * seconds));

    private float SteeringResponse => CartStations.Instance != null && CartStations.Instance.Owns(ShopPerk.SteeringReins)
        ? steeringResponse * CartStations.SteeringReinsResponse : steeringResponse;

    private void UpdateHorseVisual(Transform visual, Vector3 start, int step, float dt)
    {
        if (visual == null) return;
        float advance = Mathf.Clamp(step * horseAdvancePerSpeed, 0f, maximumHorseAdvance);
        Vector3 target = start + Vector3.left * advance;
        visual.localPosition = Vector3.MoveTowards(visual.localPosition, target, horseVisualMoveSpeed * dt);
    }
    public override void OnNetworkSpawn()
    {
        State.OnValueChanged += Changed;
        NetworkManager.NetworkTickSystem.Tick += Publish;
        if (IsServer) Publish(); else Apply(State.Value);
    }
    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= Changed;
        NetworkManager.NetworkTickSystem.Tick -= Publish;
    }
    private void Publish()
    {
        if (!IsSpawned || !IsServer) return;
        State.Value = Capture();
    }
    public CartMotionState Capture() => new CartMotionState {
        Speed=Speed, Target=TargetSpeed, Distance=Distance, Left=LeftHorseSpeed, Right=RightHorseSpeed,
        ObstacleSlow=obstacleSlowFactor, SlowRemaining=obstacleSlowRemaining, Multiplier=SpeedMultiplier, Scroll=ScrollDistance, LeftStep=LeftStep, RightStep=RightStep, Lane=Lane
    };

    public void Restore(CartMotionState snapshot,int level)
    {
        ResetForStage(level);
        Apply(snapshot);
        Publish();
    }
    private void Changed(CartMotionState previous,CartMotionState value) { if (!IsServer) Apply(value); }
    private void Apply(CartMotionState value)
    {
        Speed=value.Speed; TargetSpeed=value.Target; Distance=value.Distance;
        LeftStep=value.LeftStep; RightStep=value.RightStep; Lane=value.Lane;
        LeftHorseSpeed=value.Left; RightHorseSpeed=value.Right; SpeedMultiplier=value.Multiplier;
        obstacleSlowRemaining = value.SlowRemaining;
        obstacleSlowFactor = obstacleSlowRemaining > 0 ? value.ObstacleSlow : 1f;
        ScrollDistance=value.Scroll;
    }

}

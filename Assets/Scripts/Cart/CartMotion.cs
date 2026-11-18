using UnityEngine;
using Unity.Netcode;

// 마차 속도·이동 거리·방향.
// 마차는 화면 x에 고정이고, 배경(WorldScroller)과 장애물(ObstacleSpawner)이 ScrollVelocity로 흘러간다.
// 채찍 한 번으로 한 차선만 이동한다. 전진 속도는 차선과 별도로 계산한다.
public sealed class CartMotion : NetworkBehaviour
{
    [SerializeField] private float cruiseSpeed = 13f;
    [Tooltip("말 강화로 올릴 수 있는 최고 속도 = 기본 속도 + 이 값")]
    [SerializeField] private float maxBoostAboveBase = 5f;
    [SerializeField] private float acceleration = 3f;
    [Tooltip("말 강화 1회당 속도 증가")]
    [SerializeField] private float speedGain = 1f;
    [SerializeField, Min(0.01f)] private float laneChangeSpeed = 5f;
    [Tooltip("배경·장애물이 흐르는 속도 = Speed × 이 값")]
    [SerializeField] private float scrollScale = 0.085f;
    private const int OuterLane = 2;

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
    public float BaseScrollSpeed => baseSpeed * scrollScale;
    public float LateralLimit => OuterLane;

    public float Speed { get; private set; }
    public float TargetSpeed { get; private set; }
    public float Distance { get; private set; }
    public int LeftStep { get; private set; }
    public int RightStep { get; private set; }
    public int Lane { get; private set; }
    public float SpeedMultiplier { get; set; } = 1f;
    public float LeftHorseSpeed { get; private set; }
    public float RightHorseSpeed { get; private set; }
    public float SpeedPerHorseLevel => CartRules.Active.horses.amount;
    public Vector3 ScrollVelocity => Vector3.right * (Speed * scrollScale);
    public float ScrollDistance { get; private set; }

    private void Awake()
    {
        if (leftHorseVisual != null) leftHorseStart = leftHorseVisual.localPosition;
        if (rightHorseVisual != null) rightHorseStart = rightHorseVisual.localPosition;
        ResetForStage(0);
    }

    public void ResetForStage(int horseLevel)
    {
        baseSpeed = cruiseSpeed + Mathf.Max(0, horseLevel) * SpeedPerHorseLevel;
        maximumHorseSpeed = Mathf.Min(baseSpeed + maxBoostAboveBase, 19f);
        LeftHorseSpeed = RightHorseSpeed = baseSpeed;
        TargetSpeed = baseSpeed;
        Speed = 0f;
        LeftStep = RightStep = Lane = 0;
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
        Lane = Mathf.Clamp(Lane + (right ? 1 : -1), -OuterLane, OuterLane);

        if (right) RightHorseSpeed = Mathf.Min(maximumHorseSpeed, RightHorseSpeed + speedGain*strength);
        else LeftHorseSpeed = Mathf.Min(maximumHorseSpeed, LeftHorseSpeed + speedGain*strength);
    }

    public void SpookHorse(bool right)
    {
        if(right) RightHorseSpeed=baseSpeed*0.5f; else LeftHorseSpeed=baseSpeed*0.5f;
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
        TargetSpeed = Mathf.Min(stageSpeedLimit, (LeftHorseSpeed + RightHorseSpeed) * 0.5f * SpeedMultiplier * EnemyRope.CartSpeedMultiplier) * obstacleSlowFactor;
        Speed = Mathf.MoveTowards(Speed, TargetSpeed, acceleration * dt);
        Distance += Speed * dt;
        ScrollDistance += ScrollVelocity.x * dt;
        Vector3 position = transform.position;
        position.y = Mathf.MoveTowards(position.y, Lane, laneChangeSpeed * dt);
        transform.position = position;
        UpdateHorseVisual(leftHorseVisual, leftHorseStart, LeftStep, dt);
        UpdateHorseVisual(rightHorseVisual, rightHorseStart, RightStep, dt);
    }

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

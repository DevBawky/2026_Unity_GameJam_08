using UnityEngine;
using Unity.Netcode;

// 마차 속도·이동 거리·방향.
// 마차는 화면 x에 고정이고, 배경(WorldScroller)과 장애물(ObstacleSpawner)이 ScrollVelocity로 흘러간다.
// 마부의 수직 입력을 서버가 도로의 횡방향 이동에 적용한다.
public sealed class CartMotion : NetworkBehaviour
{
    [SerializeField] private float cruiseSpeed = 13f;
    private const float RoadUnitsPerMeter = 8f;

    public NetworkVariable<CartMotionState> State = new();
    private float baseSpeed;
    private HorseMood previousMood;
    private float obstacleSlowFactor = 1f;
    private float obstacleSlowRemaining;

    public float BaseSpeed => baseSpeed;
    public float LateralLimit => CartStations.Instance != null ? 100f * CartStations.Instance.WorldPerWebUnit : 0f;

    public float Speed { get; private set; }
    public float TargetSpeed { get; private set; }
    public float Distance { get; private set; }
    public float Lane { get; private set; }
    public float SpeedMultiplier { get; set; } = 1f;
    public float SpeedPerHorseLevel => CartRules.Active.horses.amount;
    public Vector3 ScrollVelocity => Vector3.right * (Speed * RoadUnitsPerMeter * CartStations.Instance.WorldPerWebUnit);
    public float ScrollDistance { get; private set; }

    private void Awake()
    {
        ResetForStage(0);
    }

    public void ResetForStage(int horseLevel)
    {
        var stations = CartStations.Instance;
        float wheels = stations != null && stations.Owns(ShopPerk.LightWheels) ? CartStations.LightWheelsSpeedBonus : 0f;
        baseSpeed = cruiseSpeed + Mathf.Max(0, horseLevel) * SpeedPerHorseLevel + wheels;
        previousMood = HorseMood.Normal;
        TargetSpeed = baseSpeed;
        Speed = 0f;
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

    // 출발용 당근 묶음: 기본 속도의 일정 비율로 출발한다.
    public void Kickstart(float fraction) => Speed = Mathf.Max(Speed, baseSpeed * fraction);

    public void Advance(float dt)
    {
        if (!IsSpawned || !IsServer || !CartStations.Running) return;
        if (obstacleSlowRemaining > 0f)
        {
            obstacleSlowRemaining -= dt;
            if (obstacleSlowRemaining <= 0f) obstacleSlowFactor = 1f;
        }
        var stations = CartStations.Instance;
        HorseMood mood = stations.CurrentHorseMood;
        float cruise = Mathf.Min(stations.SpeedLimit, baseSpeed);
        TargetSpeed = cruise * stations.HorsePace * SpeedMultiplier * EnemyRope.CartSpeedMultiplier * obstacleSlowFactor;
        Speed = mood == previousMood ? Mathf.Lerp(Speed, TargetSpeed, .07f) : TargetSpeed;
        previousMood = mood;
        Distance += Speed * dt;
        ScrollDistance += ScrollVelocity.x * dt;
        Vector3 position = transform.position;
        Lane = Mathf.Clamp(position.y + stations.DriverSteering * CartStations.DriverSteeringSpeed
            * stations.WorldPerWebUnit * dt, -LateralLimit, LateralLimit);
        position.y = Lane;
        transform.position = position;
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
        Speed=Speed, Target=TargetSpeed, Distance=Distance,
        ObstacleSlow=obstacleSlowFactor, SlowRemaining=obstacleSlowRemaining, Multiplier=SpeedMultiplier, Scroll=ScrollDistance, Lane=Lane
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
        Lane=value.Lane; SpeedMultiplier=value.Multiplier;
        obstacleSlowRemaining = value.SlowRemaining;
        obstacleSlowFactor = obstacleSlowRemaining > 0 ? value.ObstacleSlow : 1f;
        ScrollDistance=value.Scroll;
    }

}

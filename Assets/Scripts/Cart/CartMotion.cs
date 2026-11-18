using UnityEngine;
using Unity.Netcode;

// 마차 속도·이동 거리·방향.
// 마차는 화면 x에 고정이고, 배경(WorldScroller)과 장애물(ObstacleSpawner)이 ScrollVelocity로 흘러간다.
// 방향(Heading)은 좌우 말 속도 차이로 꺾이고, 마차는 그 방향으로 길 안에서 위아래로 움직인다.
// 길 가장자리에 닿으면 막히고 방향이 0이 된다. 빨라진 말은 그림이 앞으로 나간다.
public sealed class CartMotion : NetworkBehaviour
{
    [SerializeField] private float cruiseSpeed = 13f;
    [Tooltip("말 업그레이드 1레벨당 기본 속도 증가")]
    [SerializeField] private float speedPerHorseLevel = 3f;
    [Tooltip("말 강화로 올릴 수 있는 최고 속도 = 기본 속도 + 이 값")]
    [SerializeField] private float maxBoostAboveBase = 5f;
    [SerializeField] private float acceleration = 3f;
    [Tooltip("말 강화 1회당 속도 증가")]
    [SerializeField] private float speedGain = 1f;
    [SerializeField, Min(0f)] private float turnRate = 0.75f;
    [SerializeField] private float maxHeading = 25f;
    [Tooltip("배경·장애물이 흐르는 속도 = Speed × 이 값")]
    [SerializeField] private float scrollScale = 5f;
    [Tooltip("위아래 이동 속도 = Speed × sin(Heading) × 이 값")]
    [SerializeField] private float lateralScale = 0.25f;
    [Tooltip("마차 중심이 위아래로 갈 수 있는 한계 (길 반폭 - 마차 반높이)")]
    [SerializeField] private float lateralLimit = 3.1f;

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
    private Vector3 leftHorseStart;
    private Vector3 rightHorseStart;

    public float Speed { get; private set; }
    public float TargetSpeed { get; private set; }
    public float Distance { get; private set; }
    public float Heading { get; private set; }
    public float SpeedMultiplier { get; set; } = 1f;
    public float LeftHorseSpeed { get; private set; }
    public float RightHorseSpeed { get; private set; }
    public float SpeedPerHorseLevel => speedPerHorseLevel;
    public Vector3 ScrollVelocity => Vector3.right * (Mathf.Cos(Heading * Mathf.Deg2Rad) * Speed * scrollScale);
    public float ScrollDistance { get; private set; }

    private void Awake()
    {
        if (leftHorseVisual != null) leftHorseStart = leftHorseVisual.localPosition;
        if (rightHorseVisual != null) rightHorseStart = rightHorseVisual.localPosition;
        ResetForStage(0);
    }

    public void ResetForStage(int horseLevel)
    {
        baseSpeed = cruiseSpeed + Mathf.Max(0, horseLevel) * speedPerHorseLevel;
        maximumHorseSpeed = baseSpeed + maxBoostAboveBase;
        LeftHorseSpeed = RightHorseSpeed = baseSpeed;
        TargetSpeed = baseSpeed;
        Speed = 0f;
        Heading = 0f;
        SpeedMultiplier = 1f;
    }

    public void BoostHorse(bool right)
    {
        if (right) RightHorseSpeed = Mathf.Min(maximumHorseSpeed, RightHorseSpeed + speedGain);
        else LeftHorseSpeed = Mathf.Min(maximumHorseSpeed, LeftHorseSpeed + speedGain);
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || !CartStations.Running) return;
        TargetSpeed = (LeftHorseSpeed + RightHorseSpeed) * 0.5f * SpeedMultiplier * EnemyRope.CartSpeedMultiplier;
        Speed = Mathf.MoveTowards(Speed, TargetSpeed, acceleration * Time.deltaTime);
        Heading = Mathf.Clamp(Heading + (RightHorseSpeed - LeftHorseSpeed) * turnRate * Time.deltaTime, -maxHeading, maxHeading);
        Distance += Speed * Time.deltaTime;
        ScrollDistance += ScrollVelocity.x * Time.deltaTime;
        MoveLaterally();
        UpdateHorseVisual(leftHorseVisual, leftHorseStart, LeftHorseSpeed);
        UpdateHorseVisual(rightHorseVisual, rightHorseStart, RightHorseSpeed);
    }

    // 오른쪽 말이 빠르면 Heading이 +가 되고, 진행 방향(-x) 기준 왼쪽(-y)으로 꺾인다.
    private void MoveLaterally()
    {
        Vector3 position = transform.position;
        position.y -= Mathf.Sin(Heading * Mathf.Deg2Rad) * Speed * lateralScale * Time.deltaTime;
        if (Mathf.Abs(position.y) >= lateralLimit)
        {
            position.y = Mathf.Clamp(position.y, -lateralLimit, lateralLimit);
            Heading = 0f;
        }
        transform.position = position;
    }

    private void UpdateHorseVisual(Transform visual, Vector3 start, float horseSpeed)
    {
        if (visual == null) return;
        float advance = Mathf.Clamp((horseSpeed - baseSpeed) * horseAdvancePerSpeed, 0f, maximumHorseAdvance);
        Vector3 target = start + Vector3.left * advance;
        visual.localPosition = Vector3.MoveTowards(visual.localPosition, target, horseVisualMoveSpeed * Time.deltaTime);
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
        State.Value=new CartMotionState{Speed=Speed,Target=TargetSpeed,Distance=Distance,Heading=Heading,Left=LeftHorseSpeed,Right=RightHorseSpeed,Multiplier=SpeedMultiplier,Scroll=ScrollDistance};
    }
    private void Changed(CartMotionState previous,CartMotionState value) { if (!IsServer) Apply(value); }
    private void Apply(CartMotionState value)
    {
        Speed=value.Speed; TargetSpeed=value.Target; Distance=value.Distance; Heading=value.Heading;
        LeftHorseSpeed=value.Left; RightHorseSpeed=value.Right; SpeedMultiplier=value.Multiplier;
        ScrollDistance=value.Scroll;
    }

}

using UnityEngine;

// 마차 속도·이동 거리·방향.
// 마차는 화면 x에 고정이고, 배경(WorldScroller)과 장애물(ObstacleSpawner)이 ScrollVelocity로 흘러간다.
// 방향(Heading)은 좌우 말 속도 차이로 꺾이고, 마차는 그 방향으로 길 안에서 위아래로 움직인다.
// 길 가장자리에 닿으면 막히고 방향이 0이 된다.
public sealed class CartMotion : MonoBehaviour
{
    [SerializeField] private float cruiseSpeed = 13f;
    [Tooltip("말 업그레이드 1레벨당 기본 속도 증가")]
    [SerializeField] private float speedPerHorseLevel = 3f;
    [Tooltip("말 강화로 올릴 수 있는 최고 속도 = 기본 속도 + 이 값")]
    [SerializeField] private float maxBoostAboveBase = 5f;
    [SerializeField] private float acceleration = 3f;
    [Tooltip("말 강화 1회당 속도 증가")]
    [SerializeField] private float speedGain = 1f;
    [SerializeField] private float turnRate = 3f;
    [SerializeField] private float maxHeading = 25f;
    [Tooltip("배경·장애물이 흐르는 속도 = Speed × 이 값")]
    [SerializeField] private float scrollScale = 5f;
    [Tooltip("위아래 이동 속도 = Speed × sin(Heading) × 이 값")]
    [SerializeField] private float lateralScale = 0.25f;
    [Tooltip("마차 중심이 위아래로 갈 수 있는 한계 (길 반폭 - 마차 반높이)")]
    [SerializeField] private float lateralLimit = 3.1f;

    private float maximumHorseSpeed;

    public float Speed { get; private set; }
    public float TargetSpeed { get; private set; }
    public float Distance { get; private set; }
    public float Heading { get; private set; }
    public float SpeedMultiplier { get; set; } = 1f;
    public float LeftHorseSpeed { get; private set; }
    public float RightHorseSpeed { get; private set; }
    public float SpeedPerHorseLevel => speedPerHorseLevel;
    public Vector3 ScrollVelocity => Vector3.right * (Mathf.Cos(Heading * Mathf.Deg2Rad) * Speed * scrollScale);

    private void Awake() => ResetForStage(0);

    public void ResetForStage(int horseLevel)
    {
        float baseSpeed = cruiseSpeed + Mathf.Max(0, horseLevel) * speedPerHorseLevel;
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
        TargetSpeed = (LeftHorseSpeed + RightHorseSpeed) * 0.5f * SpeedMultiplier * EnemyRope.CartSpeedMultiplier;
        Speed = Mathf.MoveTowards(Speed, TargetSpeed, acceleration * Time.deltaTime);
        Heading = Mathf.Clamp(Heading + (RightHorseSpeed - LeftHorseSpeed) * turnRate * Time.deltaTime, -maxHeading, maxHeading);
        Distance += Speed * Time.deltaTime;
        MoveLaterally();
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
}

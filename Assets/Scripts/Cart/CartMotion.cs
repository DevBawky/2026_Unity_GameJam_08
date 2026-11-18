using UnityEngine;

public class CartMotion : MonoBehaviour
{
    [SerializeField] private float cruiseSpeed = 5f;
    [SerializeField] private float acceleration = 3f;
    [SerializeField] private float speedGain = 1f;
    [SerializeField] private float maximumHorseSpeed = 10f;
    [SerializeField, Min(0f)] private float turnRate = 0.75f;
    [SerializeField] private float maxHeading = 25f;
    [Header("Horse visual position")]
    [SerializeField] private Transform leftHorseVisual;
    [SerializeField] private Transform rightHorseVisual;
    [SerializeField, Min(0f)] private float horseAdvancePerSpeed = 0.18f;
    [SerializeField, Min(0f)] private float maximumHorseAdvance = 1f;
    [SerializeField, Min(0f)] private float horseVisualMoveSpeed = 3f;
    private Vector3 leftHorseStart;
    private Vector3 rightHorseStart;
    public float Speed { get; private set; }
    public float TargetSpeed { get; private set; }
    public float Distance { get; private set; }
    public float Heading { get; private set; }
    public float SpeedMultiplier { get; set; } = 1f;
    public float LeftHorseSpeed { get; private set; }
    public float RightHorseSpeed { get; private set; }
    private void Awake()
    {
        cruiseSpeed = 13f;
        maximumHorseSpeed = Mathf.Max(maximumHorseSpeed, 18f);
        LeftHorseSpeed = RightHorseSpeed = cruiseSpeed;
        TargetSpeed = cruiseSpeed;
        if (leftHorseVisual != null) leftHorseStart = leftHorseVisual.localPosition;
        if (rightHorseVisual != null) rightHorseStart = rightHorseVisual.localPosition;
    }
    public void ResetForStage(int horseLevel)
    {
        float baseSpeed = 13f + Mathf.Max(0, horseLevel) * 3f;
        cruiseSpeed = baseSpeed;
        maximumHorseSpeed = Mathf.Max(maximumHorseSpeed, baseSpeed + 5f);
        LeftHorseSpeed = RightHorseSpeed = baseSpeed;
        TargetSpeed = baseSpeed;
        Speed = 0f;
        Heading = 0f;
        SpeedMultiplier = 1f;
    }
    public void SetTargetSpeed(float speed) { LeftHorseSpeed = RightHorseSpeed = Mathf.Max(0f, speed); }
    public void BoostHorse(bool right)
    {
        if (right) RightHorseSpeed = Mathf.Min(maximumHorseSpeed, RightHorseSpeed + speedGain);
        else LeftHorseSpeed = Mathf.Min(maximumHorseSpeed, LeftHorseSpeed + speedGain);
    }
    private void Update()
    {
        TargetSpeed = (LeftHorseSpeed + RightHorseSpeed) * 0.5f * SpeedMultiplier * EnemyRope.CartSpeedMultiplier; // 마지막 항: 추격조 로프 감속
        Speed = Mathf.MoveTowards(Speed, TargetSpeed, acceleration * Time.deltaTime);
        Heading = Mathf.Clamp(Heading + (RightHorseSpeed - LeftHorseSpeed) * turnRate * Time.deltaTime, -maxHeading, maxHeading);
        Distance += Speed * Time.deltaTime;
        UpdateHorseVisual(leftHorseVisual, leftHorseStart, LeftHorseSpeed);
        UpdateHorseVisual(rightHorseVisual, rightHorseStart, RightHorseSpeed);
    }

    private void UpdateHorseVisual(Transform visual, Vector3 start, float horseSpeed)
    {
        if (visual == null) return;
        float advance = Mathf.Clamp((horseSpeed - cruiseSpeed) * horseAdvancePerSpeed, 0f, maximumHorseAdvance);
        Vector3 target = start + Vector3.left * advance;
        visual.localPosition = Vector3.MoveTowards(visual.localPosition, target, horseVisualMoveSpeed * Time.deltaTime);
    }
}

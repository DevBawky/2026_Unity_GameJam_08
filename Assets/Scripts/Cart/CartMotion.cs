using UnityEngine;

public class CartMotion : MonoBehaviour
{
    [SerializeField] private float cruiseSpeed = 5f;
    [SerializeField] private float acceleration = 3f;
    [SerializeField] private float speedGain = 1f;
    [SerializeField] private float maximumHorseSpeed = 10f;
    [SerializeField] private float turnRate = 3f;
    [SerializeField] private float maxHeading = 25f;
    public float Speed { get; private set; }
    public float TargetSpeed { get; private set; }
    public float Distance { get; private set; }
    public float Heading { get; private set; }
    public float LeftHorseSpeed { get; private set; }
    public float RightHorseSpeed { get; private set; }
    private void Awake() { LeftHorseSpeed = RightHorseSpeed = cruiseSpeed; TargetSpeed = cruiseSpeed; }
    public void SetTargetSpeed(float speed) { LeftHorseSpeed = RightHorseSpeed = Mathf.Max(0f, speed); }
    public void BoostHorse(bool right)
    {
        if (right) RightHorseSpeed = Mathf.Min(maximumHorseSpeed, RightHorseSpeed + speedGain);
        else LeftHorseSpeed = Mathf.Min(maximumHorseSpeed, LeftHorseSpeed + speedGain);
    }
    private void Update()
    {
        TargetSpeed = (LeftHorseSpeed + RightHorseSpeed) * 0.5f;
        Speed = Mathf.MoveTowards(Speed, TargetSpeed, acceleration * Time.deltaTime);
        Heading = Mathf.Clamp(Heading + (RightHorseSpeed - LeftHorseSpeed) * turnRate * Time.deltaTime, -maxHeading, maxHeading);
        Distance += Speed * Time.deltaTime;
    }
}

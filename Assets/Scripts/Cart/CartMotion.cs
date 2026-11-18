using UnityEngine;

// 마차는 화면에 고정, 속도·거리만 소유한다. 배경은 WorldScroller가 Speed를 읽어 움직인다.
// 속도를 바꾸는 쪽(고삐, 말, 피격 등)은 SetTargetSpeed만 호출한다.
public class CartMotion : MonoBehaviour
{
    [SerializeField] float cruiseSpeed = 5f;   // m/s (1 unit = 1 m)
    [SerializeField] float acceleration = 3f;  // m/s²

    public float Speed { get; private set; }
    public float TargetSpeed { get; private set; }
    public float Distance { get; private set; }

    void Awake() => TargetSpeed = cruiseSpeed;

    public void SetTargetSpeed(float speed) => TargetSpeed = Mathf.Max(0f, speed);

    void Update()
    {
        Speed = Mathf.MoveTowards(Speed, TargetSpeed, acceleration * Time.deltaTime);
        Distance += Speed * Time.deltaTime;
    }
}

using UnityEngine;

// 도로 장애물 프리팹(바위·통나무·사슴)에 붙는다. 충돌 크기는 SpriteRenderer 경계, 피해·감속은 인스펙터 값.
[RequireComponent(typeof(SpriteRenderer))]
public sealed class Obstacle : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damage = 6f;
    [Tooltip("부딪힌 순간 속도와 목표 속도에 곱하는 값 (작을수록 크게 느려짐)")]
    [SerializeField, Range(0.1f, 1f)] private float slowFactor = 0.7f;
    [Tooltip("감속이 유지되는 시간(초)")]
    [SerializeField, Min(0f)] private float slowSeconds = 1f;

    public float Damage => damage;
    public float SlowFactor => slowFactor;
    public float SlowSeconds => slowSeconds;
    public Vector2 Size { get; private set; }

    private void Awake() => Size = GetComponent<SpriteRenderer>().bounds.size;
}

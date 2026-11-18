using UnityEngine;

// 장애물 프리팹(바위·통나무)에 붙는다. 충돌 크기는 SpriteRenderer 경계, 피해량은 인스펙터 값.
[RequireComponent(typeof(SpriteRenderer))]
public sealed class Obstacle : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damage = 6f;

    public float Damage => damage;
    public Vector2 Size { get; private set; }

    private void Awake() => Size = GetComponent<SpriteRenderer>().bounds.size;
}

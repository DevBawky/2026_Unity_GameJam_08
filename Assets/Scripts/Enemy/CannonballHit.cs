using UnityEngine;

// 대포탄 프리팹에 붙는다. 몬스터에 닿으면 피해를 주고 사라진다.
// 이동은 기존 Projectile이 담당하고, 여기서는 충돌만 처리한다.
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CannonballHit : MonoBehaviour
{
    [Tooltip("몬스터에게 주는 피해. 몬스터 HP 이상이면 한 방에 처치")]
    [Min(0)] public float damage = 1f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Enemy enemy)) return;
        enemy.TakeDamage(damage);
        Destroy(gameObject);
    }
}

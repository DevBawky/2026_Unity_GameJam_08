using UnityEngine;

// 대포탄 이동. 발사 방향으로 직선 비행하고 일정 시간 뒤 사라진다. 몬스터 충돌은 CannonballHit이 맡는다.
public sealed class Projectile : MonoBehaviour
{
    private const float Lifetime = 5f;

    private Vector3 velocity;

    public void Launch(Vector3 direction, float speed)
    {
        velocity = direction.normalized * speed;
        Destroy(gameObject, Lifetime);
    }

    private void Update() => transform.position += velocity * Time.deltaTime;
}

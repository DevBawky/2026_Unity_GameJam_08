using UnityEngine;
using Unity.Netcode;

// 대포탄 이동. 발사 방향으로 직선 비행하고 일정 시간 뒤 사라진다. 몬스터 충돌은 CannonballHit이 맡는다.
public sealed class Projectile : NetworkBehaviour
{
    private const float Lifetime = 5f;

    private Vector3 velocity;
    private float remaining;

    public void Launch(Vector3 direction, float speed)
    {
        velocity = direction.normalized * speed;
        remaining=Lifetime;
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || !CartStations.Running) return;
        transform.position += velocity*Time.deltaTime;
        remaining-=Time.deltaTime;
        if (remaining<=0f) NetworkObject.Despawn(true);
    }
}

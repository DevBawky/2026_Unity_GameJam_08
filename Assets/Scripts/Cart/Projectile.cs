using UnityEngine;
using Unity.Netcode;

// 대포탄 이동. 발사 방향으로 직선 비행하고 일정 시간 뒤 사라진다. 몬스터 충돌은 CannonballHit이 맡는다.
public sealed class Projectile : NetworkBehaviour
{
    private const float Lifetime = 5f;

    private Vector3 velocity;
    private float remaining;
    private CannonballHit hit;
    private void Awake() => hit = GetComponent<CannonballHit>();

    public void Launch(Vector3 direction, float speed)
    {
        velocity = direction.normalized * speed;
        remaining=Lifetime;
    }

    public override void OnNetworkDespawn() => CartStations.SimulationStep-=Advance;
    private void Advance(float dt)
    {
        if (!IsSpawned || !IsServer || !CartStations.Running) return;
        var step = velocity * dt;
        hit.Sweep(step);
        transform.position += step;
        remaining-=dt;
        if (remaining<=0f) NetworkObject.Despawn(true);
    }
    private readonly NetworkVariable<int> caliber = new();
    private int startCaliber;
    public ShotCheckpoint Capture(Enemy[] enemies) => new() { Position=transform.position, Velocity=velocity,Rotation=transform.rotation,Remaining=remaining,Caliber=caliber.Value,HitEnemies=hit.HitIndices(enemies) };
    public void Restore(ShotCheckpoint data,Enemy[] enemies)
    {
        velocity=data.Velocity;remaining=data.Remaining;hit.RestoreHits(data.HitEnemies,enemies);
    }
    public void SetCaliber(int level) => startCaliber = level;
    public override void OnNetworkSpawn()
    {
        if (IsServer) { caliber.Value = startCaliber; CartStations.SimulationStep+=Advance; }
        transform.localScale *= 1f + CartRules.Active.barrel.amount * caliber.Value;
    }
}

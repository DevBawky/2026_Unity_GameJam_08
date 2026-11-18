using Unity.Netcode;
using UnityEngine;

// 대포 점화·도화선·발사.
public sealed partial class CartStations
{
    public void Ignite(NetworkPlayer actor)
    {
        if (!IsServer || !Running || !loaded || fuseLit || shellPrefab == null || cannonMuzzle == null) return;
        gunner = actor;
        Gunner.Value = actor.NetworkObjectId;
        StartFuse();
    }
    public NetworkVariable<double> LastShockTime = new(-100);
    private float fuseRemaining;
    private void StartFuse(float initial = -1f)
    {
        fuseLit = true;
        fuseRemaining = initial < 0f ? CartRules.Active.fuseSeconds : initial;
        FuseDeadline.Value = NetworkManager.ServerTime.Time + fuseRemaining;
    }

    private void AdvanceFuse()
    {
        if (!fuseLit) return;
        fuseRemaining -= Step;
        if (fuseRemaining > 0.00001f) return;
        var target = CannonAim.AimPoint(gunner != null ? gunner.Aim.Value : Vector2.zero, cannonMuzzle.position,
            this, NetworkManager.ServerTime.Time, out var spread, out _);
        // Isolate shot scatter from child, horse and wave random rolls.
        uint seed = randomState ^ unchecked((uint)(careTicks+1)*1597334677u);
        seed = unchecked(seed*1664525u+1013904223u);
        float angle = 2f*Mathf.PI*(seed/4294967296f);
        seed = unchecked(seed*1664525u+1013904223u);
        float radius = Mathf.Sqrt(seed/4294967296f);
        target += Vector2.Scale(spread, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
        loaded = false;
        fuseLit = false;
        Gunner.Value = ulong.MaxValue;
        CompleteStation(StationAction.Cannon, gunner);
        StationOwners[(int)StationAction.Cannon] = NoStationOwner;
        ApplyImpact();
        AddImpactEffects(recoilAnxietyIncrease, recoilShakeStrength, true);
        Vector2 direction = target - (Vector2)cannonMuzzle.position;
        var rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        GameObject shot = Instantiate(shellPrefab, cannonMuzzle.position, rotation);
        shot.GetComponent<Projectile>().SetCaliber(Barrel.Value);
        shot.GetComponent<NetworkObject>().Spawn(true);
        if (shot.TryGetComponent(out Projectile projectile))
            projectile.Launch(target, 1750f * WorldPerWebUnit);
        Refresh();
    }
}

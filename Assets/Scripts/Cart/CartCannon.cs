using Unity.Netcode;
using UnityEngine;

public struct CannonShotVisual : INetworkSerializeByMemcpy, System.IEquatable<CannonShotVisual>
{
    public double Time;
    public float Angle;
    public readonly bool Equals(CannonShotVisual other) => Time == other.Time && Angle == other.Angle;
}

// 대포 점화·도화선·발사.
public sealed partial class CartStations
{
    public const int CleaningTapsRequired = 12;
    private const int PostShotTicks = 75;
    public const float PostShotReturnSeconds = .5f;
    private int cleaningTaps;
    private int postShotRemaining;
    public int CleaningTaps => cleaningTaps;
    public bool NeedsCleaning => !loaded && cleaningTaps < CleaningTapsRequired;
    public NetworkVariable<double> PostShotEndsAt = new();
    public StationAction CannonAction => NeedsCleaning ? StationAction.CannonCleaning : loaded ? StationAction.Cannon : StationAction.Reload;
    public Transform CannonPivot => cannonMuzzle;
    public readonly NetworkVariable<CannonShotVisual> LastCannonShot = new(new CannonShotVisual { Time = -1d });
    public float CannonBarrelLength => 44f * WorldPerWebUnit * (1f + .17f * Barrel.Value);
    public NetworkPlayer CannonOperator
    {
        get
        {
            foreach (var player in gameScene.Players)
                if (HoldsStation(StationAction.Cannon, player)) return player;
            return null;
        }
    }

    public bool IsPostShotGunner(NetworkPlayer actor) => Running && actor != null
        && PostShotEndsAt.Value > 0d && Gunner.Value == actor.NetworkObjectId;

    private bool CanReload => !NeedsCleaning && depositedShells > 0 && !loaded && !fuseLit;

    // ` 키 바로 적재: 포탄 더미에 남은 탄이 있을 때만 하나를 옮긴다.
    internal void AddStoredShell(NetworkPlayer actor, int stage)
    {
        if (!IsServer || !Running || stage != stageNumber || !actor.Assigned.Value || actor.Bot.Value || shellsInPile <= 0) return;
        shellsInPile--;
        depositedShells++;
        Refresh();
    }

    public void Ignite(NetworkPlayer actor)
    {
        if (!IsServer || !Running || !CanUse(StationAction.Cannon, actor) ||
            !actor.Timing.IsInActionArea(StationAction.Cannon) || shellPrefab == null || cannonMuzzle == null) return;
        EndPostShot();
        gunner = actor;
        gunnerScoreOwnerVersion = actor.Bot.Value ? 0 : actor.ScoreOwnerVersion;
        Gunner.Value = actor.NetworkObjectId;
        StartFuse();
    }
    private float fuseRemaining;
    private void StartFuse(float initial = -1f)
    {
        fuseLit = true;
        fuseRemaining = initial < 0f ? CartRules.Active.fuseSeconds * (Owns(ShopPerk.LongFuse) ? LongFuseRate : 1f) : initial;
        FuseDeadline.Value = NetworkManager.ServerTime.Time + fuseRemaining;
    }

    private void AdvanceFuse()
    {
        if (postShotRemaining > 0 && --postShotRemaining == 0) EndPostShot();
        if (!fuseLit) return;
        fuseRemaining -= Step;
        if (fuseRemaining > 0.00001f) return;
        var shooter = gunner;
        int shooterScoreOwnerVersion = gunnerScoreOwnerVersion;
        var target = CannonAim.AimPoint(shooter != null ? shooter.Aim.Value : Vector2.zero, cannonMuzzle.position,
            this, NetworkManager.ServerTime.Time, out var spread, out _);
        // Isolate shot scatter from child, horse and wave random rolls.
        uint seed = randomState ^ unchecked((uint)(careTicks+1)*1597334677u);
        seed = unchecked(seed*1664525u+1013904223u);
        float angle = 2f*Mathf.PI*(seed/4294967296f);
        seed = unchecked(seed*1664525u+1013904223u);
        float radius = Mathf.Sqrt(seed/4294967296f);
        target += Vector2.Scale(spread, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
        Vector2 direction = (target - (Vector2)cannonMuzzle.position).normalized;
        float angleDegrees = Vector2.SignedAngle(Vector2.right, direction);
        LastCannonShot.Value = new CannonShotVisual { Time = NetworkManager.ServerTime.Time, Angle = angleDegrees };
        loaded = false;
        cleaningTaps = 0;
        fuseLit = false;
        FuseDeadline.Value = 0d;
        if (gunner != null && !gunner.Bot.Value && HoldsStation(StationAction.Cannon, gunner)) StartPostShot(Owns(ShopPerk.ReleaseLever) ? ReleaseLeverTicks : PostShotTicks);
        else EndPostShot();
        float recoil = Owns(ShopPerk.RecoilMount) ? RecoilMountRate : 1f;
        AddImpactEffects(recoilAnxietyIncrease * recoil, recoilShakeStrength * recoil, CannonShock * recoil, .25f * recoil, cannon: true);
        var rotation = Quaternion.Euler(0f, 0f, angleDegrees);
        var projectile = Instantiate(shellPrefab, cannonMuzzle.position + (Vector3)direction * CannonBarrelLength, rotation).GetComponent<Projectile>();
        projectile.SetCaliber(Barrel.Value);
        projectile.SetShooter(shooter, shooterScoreOwnerVersion);
        projectile.NetworkObject.Spawn(true);
        projectile.Launch(target, 1750f * WorldPerWebUnit);
        Refresh();
    }

    private void StartPostShot(int ticks)
    {
        postShotRemaining = ticks;
        PostShotEndsAt.Value = NetworkManager.ServerTime.Time + ticks * (double)Step;
    }

    private void EndPostShot()
    {
        postShotRemaining = 0;
        PostShotEndsAt.Value = 0d;
        if (fuseLit) return;
        gunner = null;
        gunnerScoreOwnerVersion = 0;
        Gunner.Value = NoStationOwner;
    }

    private void ResetGunner()
    {
        gunner = null;
        gunnerScoreOwnerVersion = 0;
        Gunner.Value = NoStationOwner;
        FuseDeadline.Value = 0d;
        PostShotEndsAt.Value = 0d;
        fuseRemaining = 0f;
        postShotRemaining = 0;
    }
}

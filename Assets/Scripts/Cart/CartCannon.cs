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
    public readonly NetworkVariable<int> CannonCombo = new();
    internal void ResolveCannonCombo(int kills)
    {
        if (kills == 0) CannonCombo.Value = 0;
        else if (kills >= 2 || CannonCombo.Value > 0) CannonCombo.Value += kills;
    }
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
    internal int CannonTick => IsServer ? careTicks : State.Value.StageTick;
    internal float CannonCaliber => Projectile.ScaleFor(Barrel.Value);

    internal Vector3 CartArtworkOffset(double now)
    {
        Vector2 pivot = CannonPivot.position;
        return CannonRecoilOffset(now) + (Vector3)(CannonAim.Origin(pivot, this, now, CannonTick) - pivot);
    }

    internal Vector3 CannonRecoilOffset(double now)
    {
        var shot = LastCannonShot.Value;
        float age = (float)((now - shot.Time) * 30d);
        if (shot.Time < 0d || age < 0f || age >= 15f || SettingsPanel.ReducedMotion) return Vector3.zero;
        float t = 1f - age / 15f;
        Vector3 direction = Quaternion.Euler(0f, 0f, shot.Angle) * Vector3.right;
        return (-direction * (18f * Mathf.Cos(age * .76f)) + Vector3.down * (10f * Mathf.Cos(age * 1.38f)))
            * (t * t * WorldPerWebUnit);
    }

    public Vector3 RenderedMuzzle(Vector2 direction, double now, bool upper = false)
    {
        var shot = ShotAt(upper);
        float recoil = shot.Time < 0d ? -1f : (float)((now - shot.Time) * 30d);
        var scale = CartArtwork.BarrelScale(FiringAt(upper), (float)((FuseAt(upper) - now) * 30d), recoil);
        return PivotAt(upper).position + (Vector3)direction * (40f * WorldPerWebUnit * CannonCaliber * scale.x);
    }

    public float FuseDuration => CartRules.Active.fuseSeconds * (Owns(ShopPerk.LongFuse) ? LongFuseRate : 1f);
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
        && (PostShotEndsAt.Value > 0d && Gunner.Value == actor.NetworkObjectId
            || upperCannon.Value.PostShot > 0d && upperCannon.Value.Gunner == actor.NetworkObjectId);

    private bool CanReload => !NeedsCleaning && depositedShells > 0 && !loaded && !fuseLit;

    // ` 키 바로 적재: 포탄 더미에 남은 탄이 있을 때만 하나를 옮긴다.
    internal void AddStoredShell(NetworkPlayer actor, int stage)
    {
        if (!IsServer || !Running || stage != stageNumber || !actor.Assigned.Value || actor.Bot.Value || shellsInPile <= 0) return;
        shellsInPile--;
        depositedShells++;
        Refresh();
    }

    public void Ignite(NetworkPlayer actor, bool upper = false)
    {
        if (upper) { IgniteUpper(actor); return; }
        if (!IsServer || !Running || !CanUse(StationAction.Cannon, actor) || shellPrefab == null || cannonMuzzle == null) return;
        EndPostShot();
        gunner = actor;
        Gunner.Value = actor.NetworkObjectId;
        StartFuse();
    }
    private float fuseRemaining;
    private void StartFuse(float initial = -1f)
    {
        fuseLit = true;
        fuseRemaining = initial < 0f ? FuseDuration : initial;
        FuseDeadline.Value = NetworkManager.ServerTime.Time + fuseRemaining;
        if (fuseRemaining > 0f) PlayCartCueRpc(GameSfx.FuseBurn);
    }

    private void AdvanceFuse()
    {
        if (postShotRemaining > 0 && --postShotRemaining == 0) EndPostShot();
        if (!fuseLit) return;
        fuseRemaining -= Step;
        if (fuseRemaining > 0.00001f) return;
        double now = NetworkManager.ServerTime.Time;
        int tick = careTicks + 1;
        var shot = FireCannon(gunner, cannonMuzzle, now, tick, false);
        LastCannonShot.Value = shot;
        loaded = false;
        cleaningTaps = 0;
        fuseLit = false;
        FuseDeadline.Value = 0d;
        if (gunner != null && !gunner.Bot.Value && HoldsStation(StationAction.Cannon, gunner)) StartPostShot(Owns(ShopPerk.ReleaseLever) ? ReleaseLeverTicks : PostShotTicks);
        else EndPostShot();
        Refresh();
    }

    private CannonShotVisual FireCannon(NetworkPlayer shooter, Transform pivot, double now, int tick, bool upper)
    {
        var target = CannonAim.AimPoint(shooter != null ? shooter.Aim.Value : Vector2.zero, pivot.position,
            this, now, out var spread, out _, tick, upper);
        // Isolate shot scatter from child, horse and wave random rolls.
        uint seed = randomState ^ unchecked((uint)(tick + (upper ? 2 : 1)) * 1597334677u);
        seed = unchecked(seed*1664525u+1013904223u);
        float angle = 2f*Mathf.PI*(seed/4294967296f);
        seed = unchecked(seed*1664525u+1013904223u);
        float radius = Mathf.Sqrt(seed/4294967296f);
        target += Vector2.Scale(spread, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
        Vector2 origin = CannonAim.Origin(pivot.position, this, now, tick);
        Vector2 direction = (target - origin).normalized;
        Vector2 opening = origin + direction * (44f * WorldPerWebUnit * CannonCaliber);
        float angleDegrees = Vector2.SignedAngle(Vector2.right, direction);
        float recoil = Owns(ShopPerk.RecoilMount) ? RecoilMountRate : 1f;
        AddImpactEffects(recoilAnxietyIncrease * recoil, 0f, CannonShock * recoil, .0625f * recoil, cannon: true);
        var rotation = Quaternion.Euler(0f, 0f, angleDegrees);
        GameObject shot = Instantiate(shellPrefab, opening, rotation);
        var shell = shot.GetComponent<Projectile>();
        shell.SetCaliber(Barrel.Value);
        shell.SetShooter(shooter);
        shell.Launch(target, Projectile.FlightSpeed * WorldPerWebUnit);
        shot.GetComponent<NetworkObject>().Spawn(true);
        return new CannonShotVisual { Time = now, Angle = angleDegrees };
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
        Gunner.Value = NoStationOwner;
    }

    private void ResetGunner()
    {
        gunner = null;
        Gunner.Value = NoStationOwner;
        FuseDeadline.Value = 0d;
        PostShotEndsAt.Value = 0d;
        fuseRemaining = 0f;
        postShotRemaining = 0;
    }
}

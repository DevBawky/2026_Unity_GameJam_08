using Unity.Netcode;
using UnityEngine;

internal struct CannonImpactVisual : INetworkSerializeByMemcpy, System.IEquatable<CannonImpactVisual>
{
    public double Time;
    public Vector2 Landing;
    public float Scroll;
    public readonly bool Equals(CannonImpactVisual other) =>
        Time == other.Time && Landing.Equals(other.Landing) && Scroll == other.Scroll;
}

public sealed class Projectile : NetworkBehaviour
{
    internal const float BaseRadius = 28f;
    internal const float FlightSpeed = 1750f;
    internal const float ImpactSeconds = 3f;
    private const float WaveSeconds = .64f;
    private const float WaveSecondsPerCaliber = 20f / 30f;
    [SerializeField] private SpriteRenderer flight;
    [SerializeField] private SpriteRenderer impactWave;
    [SerializeField] private SpriteRenderer scorch;
    [SerializeField] private SpriteRenderer impactSmoke;
    [SerializeField] private SpriteSequence flightSequence, impactSequence, impactSmokeSequence;
    private readonly NetworkVariable<int> caliber = new();
    private readonly NetworkVariable<CannonImpactVisual> impact = new(new CannonImpactVisual { Time = double.MinValue });
    private int startCaliber;
    private float speed;
    private Vector2 landing;
    private CannonballHit hit;
    private Collider2D shape;
    private NetworkPlayer shooter;
    private int shooterOwnerVersion;

    internal void SetShooter(NetworkPlayer actor)
    {
        shooter = actor;
        shooterOwnerVersion = actor != null ? actor.ScoreOwnerVersion : 0;
    }

    internal void AwardElimination()
    {
        if (IsServer && shooter != null && shooter.ScoreOwnerVersion == shooterOwnerVersion)
            shooter.AwardScore(CartRules.EnemyEliminationScore);
    }
    private void Awake()
    {
        hit = GetComponent<CannonballHit>();
        shape = GetComponent<Collider2D>();
    }

    public void Launch(Vector2 point, float unitsPerSecond) { landing = point; speed = unitsPerSecond; }
    public void SetCaliber(int level) => startCaliber = level;
    internal static float ScaleFor(int level) => 1f + CartRules.Active.barrel.amount * level;
    internal float CaliberScale => ScaleFor(caliber.Value);
    private float ImpactUpgrade => Mathf.Min(.5f, CaliberScale - 1f);
    internal float Length => (200f + 70f * ImpactUpgrade) * CartStations.Instance.WorldPerWebUnit;
    internal float HalfWidth => (87f + 40f * ImpactUpgrade) * CartStations.Instance.WorldPerWebUnit;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            caliber.Value = startCaliber;
            CartStations.CannonProjectileStep += Advance;
        }
        if (shape is CircleCollider2D circle)
            circle.radius = BaseRadius * CaliberScale * CartStations.Instance.WorldPerWebUnit / Mathf.Abs(transform.lossyScale.x);
        impact.OnValueChanged += ImpactChanged;
        ImpactChanged(default, impact.Value);
    }
    public override void OnNetworkDespawn()
    {
        CartStations.CannonProjectileStep -= Advance;
        impact.OnValueChanged -= ImpactChanged;
    }
    private void ImpactChanged(CannonImpactVisual before, CannonImpactVisual after)
    {
        bool landed = after.Time != double.MinValue;
        flight.enabled = shape.enabled = !landed;
        scorch.enabled = impactSmoke.enabled = impactWave.enabled = landed;
    }

    private void Advance(float dt)
    {
        if (!IsSpawned || !IsServer) return;
        var cart = CartStations.Instance.Cart;
        if (impact.Value.Time != double.MinValue)
        {
            if (NetworkManager.ServerTime.Time - impact.Value.Time >= ImpactSeconds) NetworkObject.Despawn(true);
            return;
        }
        Vector2 pos = transform.position;
        Vector2 next = Vector2.MoveTowards(pos, landing, speed * dt);
        hit.Sweep(next - pos, (landing - pos).normalized * speed);
        transform.position = next;
        if (next != landing) return;
        hit.Blast(landing, Length, HalfWidth);
        impact.Value = new CannonImpactVisual { Time = NetworkManager.ServerTime.Time, Landing = landing, Scroll = cart.ScrollDistance };
        CartStations.Instance.PlayCannonLanding();
    }
    private void LateUpdate()
    {
        if (!IsSpawned) return;
        double now = NetworkManager.ServerTime.Time;
        float unit = CartStations.Instance.WorldPerWebUnit;
        var ground = impact.Value;
        if (ground.Time == double.MinValue)
        {
            float stageTime = CartStations.Instance.CannonTick / 30f;
            flight.sprite = flightSequence.FrameAt(SettingsPanel.ReducedMotion ? 0f : stageTime, true);
            SetVisual(flight, transform.position, transform.rotation, Vector2.one * (unit * CaliberScale));
            return;
        }

        float impactAge = (float)(now - ground.Time);
        float drift = CartStations.Instance.Cart.ScrollDistance - ground.Scroll;
        Vector3 impactPoint = new(ground.Landing.x + drift, ground.Landing.y, transform.position.z);
        int visualCaliber = Mathf.Clamp(caliber.Value, 0, 3);
        impactWave.enabled = impactAge >= 0f && impactAge < WaveSeconds;
        if (impactWave.enabled)
        {
            impactWave.sprite = impactSequence.FrameAt(visualCaliber * WaveSecondsPerCaliber + impactAge);
            SetVisual(impactWave, impactPoint, Quaternion.identity, Vector2.one * unit);
        }
        impactSmoke.enabled = impactAge >= 0f && impactAge < ImpactSeconds;
        if (impactSmoke.enabled)
        {
            impactSmoke.sprite = impactSmokeSequence.FrameAt(visualCaliber * ImpactSeconds + impactAge);
            SetVisual(impactSmoke, impactPoint, Quaternion.identity, Vector2.one * unit);
        }
        float alpha = Mathf.Clamp((1f - impactAge / ImpactSeconds) * 1.9f, 0f, .66f);
        scorch.color = new Color(1f, 1f, 1f, alpha);
        float scorchWidth = 94f * unit * CaliberScale / scorch.sprite.bounds.size.x;
        SetVisual(scorch, impactPoint + Vector3.right * (10f * unit * CaliberScale),
            Quaternion.identity, Vector2.one * scorchWidth);
    }

    private static void SetVisual(SpriteRenderer renderer, Vector3 position, Quaternion rotation, Vector2 scale)
    {
        Transform visual = renderer.transform;
        Vector3 parentScale = visual.parent.lossyScale;
        visual.SetPositionAndRotation(position, rotation);
        visual.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, 1f);
    }
    public ShotCheckpoint Capture(Enemy[] enemies) => new() {
        Position=transform.position, Rotation=transform.rotation, Landing=landing, Speed=speed, Caliber=caliber.Value, ShooterSlotKey=shooter != null ? shooter.Slot.Value + 1 : 0, ShooterOwnerVersion=shooterOwnerVersion,
        Landed=impact.Value.Time!=double.MinValue,
        ImpactAge=impact.Value.Time==double.MinValue ? 0 : (float)(NetworkManager.ServerTime.Time-impact.Value.Time),
        ScrollAtImpact=impact.Value.Scroll, HitEnemies=hit.HitIndices(enemies), NearMissEnemies=hit.NearMissIndices(enemies) };
    public void Restore(ShotCheckpoint data, Enemy[] enemies)
    {
        landing = data.Landing; speed = data.Speed;
        if (speed <= 0) { speed=data.Velocity.magnitude; landing=(Vector2)data.Position+(Vector2)data.Velocity*data.Remaining; }
        if (data.Landed)
            impact.Value = new CannonImpactVisual
            {
                Time = NetworkManager.ServerTime.Time - data.ImpactAge,
                Landing = landing,
                Scroll = data.ScrollAtImpact
            };
        hit.RestoreContacts(data.HitEnemies, data.NearMissEnemies ?? data.StartledEnemies, enemies);
        int slot = data.ShooterSlotKey - 1;
        if (slot >= 0 && slot < CartStations.Instance.Crew.Length)
        {
            var actor = CartStations.Instance.Crew[slot];
            if (actor.ScoreOwnerVersion == data.ShooterOwnerVersion) SetShooter(actor);
        }
    }
}

using Unity.Netcode;
using UnityEngine;

public sealed class Projectile : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer impactWave;
    [SerializeField] private SpriteRenderer scorch;
    [SerializeField] private ParticleSystem smoke;
    private readonly NetworkVariable<int> caliber = new();
    private readonly NetworkVariable<double> impactTime = new(double.MinValue);
    private int startCaliber;
    private float speed, scrollAtImpact;
    private Vector2 landing;
    private CannonballHit hit;
    private SpriteRenderer ball;
    private Collider2D shape;
    private void Awake()
    {
        hit = GetComponent<CannonballHit>();
        ball = GetComponent<SpriteRenderer>();
        shape = GetComponent<Collider2D>();
    }

    public void Launch(Vector2 point, float unitsPerSecond) { landing = point; speed = unitsPerSecond; }
    public void SetCaliber(int level) => startCaliber = level;
    public override void OnNetworkSpawn()
    {
        if (IsServer) { caliber.Value = startCaliber; CartStations.SimulationStep += Advance; }
        transform.localScale *= 1f + CartRules.Active.barrel.amount * caliber.Value;
        if (IsServer && shape is CircleCollider2D circle)
            circle.radius = 14f * CartStations.Instance.WorldPerWebUnit * (1f + CartRules.Active.barrel.amount * caliber.Value) / Mathf.Abs(transform.lossyScale.x);
        impactTime.OnValueChanged += ImpactChanged;
        ImpactChanged(-1, impactTime.Value);
    }
    public override void OnNetworkDespawn()
    {
        CartStations.SimulationStep -= Advance;
        impactTime.OnValueChanged -= ImpactChanged;
    }
    private void ImpactChanged(double before, double after)
    {
        bool landed = after != double.MinValue;
        ball.enabled = shape.enabled = !landed;
        if (scorch != null) scorch.enabled = landed;
        if (impactWave != null) impactWave.enabled = landed;
        if (landed && smoke != null) smoke.Play();
    }
    private float Upgrade => Mathf.Min(.5f, CartRules.Active.barrel.amount * caliber.Value);
    private float Length => (200f + 70f * Upgrade) * CartStations.Instance.WorldPerWebUnit;
    private float HalfWidth => (87f + 40f * Upgrade) * CartStations.Instance.WorldPerWebUnit;

    private void Advance(float dt)
    {
        if (!IsSpawned || !IsServer) return;
        var cart = CartStations.Instance.Cart;
        if (impactTime.Value != double.MinValue)
        {
            transform.position = landing + Vector2.right * (cart.ScrollDistance - scrollAtImpact);
            if (NetworkManager.ServerTime.Time - impactTime.Value >= 3) NetworkObject.Despawn(true);
            return;
        }
        Vector2 pos = transform.position;
        Vector2 next = Vector2.MoveTowards(pos, landing, speed * dt);
        hit.Sweep(next - pos);
        transform.position = next;
        if (next != landing) return;
        hit.Blast(landing, Length, HalfWidth);
        scrollAtImpact = cart.ScrollDistance;
        impactTime.Value = NetworkManager.ServerTime.Time;
    }
    private void LateUpdate()
    {
        if (!IsSpawned || impactTime.Value == double.MinValue) return;
        float age = (float)(NetworkManager.ServerTime.Time - impactTime.Value);
        if (impactWave != null)
        {
            float t = Mathf.Clamp01(age / .35f);
            impactWave.enabled = t < 1;
            impactWave.transform.position = transform.position + Vector3.right * (.5f * Length * t);
            var s = transform.lossyScale;
            var sprite = impactWave.sprite.bounds.size;
            impactWave.transform.localScale = new Vector3(t*Length/(sprite.x*s.x),t*2*HalfWidth/(sprite.y*s.y),1);
        }
        if (scorch != null) scorch.color = new Color(.23f,.17f,.14f,.8f * Mathf.Clamp01(3-age));
    }
    public ShotCheckpoint Capture(Enemy[] enemies) => new() {
        Position=transform.position, Rotation=transform.rotation, Landing=landing, Speed=speed, Caliber=caliber.Value,
        Landed=impactTime.Value!=double.MinValue, ImpactAge=impactTime.Value==double.MinValue ? 0 : (float)(NetworkManager.ServerTime.Time-impactTime.Value),
        ScrollAtImpact=scrollAtImpact, HitEnemies=hit.HitIndices(enemies) };
    public void Restore(ShotCheckpoint data, Enemy[] enemies)
    {
        landing = data.Landing; speed = data.Speed; scrollAtImpact = data.ScrollAtImpact;
        if (speed <= 0) { speed=data.Velocity.magnitude; landing=(Vector2)data.Position+(Vector2)data.Velocity*data.Remaining; }
        if (data.Landed) impactTime.Value = NetworkManager.ServerTime.Time - data.ImpactAge;
        hit.RestoreHits(data.HitEnemies,enemies);
    }
}

using System;
using UnityEngine;
using Unity.Netcode;

// 웨이브 배율이 적용된 값으로 초기화되고, 마차 몸체 가장자리를 향해 일정 속도로 다가온다.
// 사거리 안에 들어오면 유닛 종류(UnitSO.type)에 따라 공격한다.
//   Lancer → attackInterval마다 근접 공격
//   Archer → 화면 안(screenInset)에 들어오면 멈춰서 attackInterval마다 화살 발사
//   Chaser → 로프를 건다 (EnemyRope)
public class Enemy : NetworkBehaviour
{
    // 마차 피해 신호. EnemySpawner가 마차 내구도(CartStations)에 연결한다.
    public static event Action<float> CartHit;
    public static void HitCart(float damage) => CartHit?.Invoke(damage);

    [SerializeField] private UnitSO unitDefinition;
    [SerializeField] private SpriteRenderer mountedVisual;
    [SerializeField] private SpriteSequence gallop;
    public NetworkVariable<float> Health = new();
    public static readonly System.Collections.Generic.List<Enemy> Active = new();
    public UnitSO Unit { get; private set; }
    public override void OnNetworkSpawn()
    {
        Active.Add(this);
        PreviousPosition = transform.position;
        if (Unit==null) Unit=unitDefinition;
        if (IsServer) { Health.Value=hp; CartStations.SimulationStep+=Advance; }
    }

    public Vector2 PreviousPosition { get; private set; }
    private Vector2 knockVelocity;
    private float knockLeft;
    public bool Knocked => knockLeft > 0;
    public void KnockAway(Vector2 velocity)
    {
        if (!IsServer || !IsSpawned || Knocked) return;
        knockVelocity = velocity;
        knockLeft = 24f/30f;
        if (rope != null && rope.Attached) rope.Detach();
    }
    private SpriteRenderer target;
    private EnemyRope rope;
    private float speed;
    private float attack;
    private float hp;
    private float attackTimer;
    private float screenRightX;
    private float engagementAge = -1f;
    private float sinceStrike = float.PositiveInfinity;

    private void Awake() => TryGetComponent(out rope);

    private void LateUpdate()
    {
        if (mountedVisual != null && IsSpawned)
            mountedVisual.sprite = gallop.FrameAt((float)NetworkManager.ServerTime.Time, true);
    }

    public void Init(UnitSO unit, WaveSO wave, SpriteRenderer cartBody, float baseScreenRightX)
    {
        Unit = unit;
        speed = unit.moveSpeed * Mathf.Lerp(1-wave.speedMin,1+wave.speedMax,CartStations.Instance.Sample());
        attack = wave.AttackOf(unit);
        hp = unit.hp;
        target = cartBody;
        screenRightX = baseScreenRightX;

        if (unit.type == UnitType.Archer && unit.arrowPrefab == null)
            Debug.LogWarning($"[{unit.name}] 궁수인데 화살 프리팹이 없어 공격하지 않습니다", unit);
        if (unit.type == UnitType.Chaser && rope == null)
            Debug.LogWarning($"[{name}] 추격조 프리팹에 EnemyRope가 없어 로프를 걸지 않습니다", this);
    }

    public PursuerCheckpoint Capture() => new() { Unit=Unit.name,KnockLeft=knockLeft,KnockVelocity=knockVelocity,Angle=transform.eulerAngles.z,Position=transform.position,Health=hp,Speed=speed,Damage=attack,Wait=attackTimer,EngagementAge=engagementAge,SinceStrike=float.IsInfinity(sinceStrike)?10:sinceStrike };
    public void Restore(PursuerCheckpoint saved)
    {
        knockLeft=saved.KnockLeft;knockVelocity=saved.KnockVelocity;transform.rotation=Quaternion.Euler(0,0,saved.Angle);
        hp=saved.Health;speed=saved.Speed;attack=saved.Damage;attackTimer=saved.Wait;sinceStrike=saved.SinceStrike;engagementAge=saved.EngagementAge;Health.Value=hp;
    }

    public void TakeDamage(float damage)
    {
        if (!IsSpawned || !IsServer || Knocked || !CartStations.Running) return;
        hp -= Mathf.Max(0f,damage);
        Health.Value=hp;
        if (hp <= 0f) NetworkObject.Despawn(true);
    }

    public override void OnNetworkDespawn()
    {
        Active.Remove(this);
        CartStations.SimulationStep -= Advance;
    }
    private void Advance(float dt)
    {
        if (!IsSpawned || !IsServer || !CartStations.Running || Unit==null || target==null) return;
        PreviousPosition = transform.position;
        if (Knocked)
        {
            transform.position += (Vector3)(knockVelocity * dt);
            transform.Rotate(0,0,Mathf.Sign(knockVelocity.y)*420f*dt);
            knockVelocity *= .965f;
            knockLeft -= dt;
            if (knockLeft <= .0001f) NetworkObject.Despawn(true);
            return;
        }
        if (rope != null && rope.Attached) return;

        if (engagementAge >= 0f)
        {
            engagementAge += dt;
            if (CartStations.Instance.Endless.Value && engagementAge >= 14f) { NetworkObject.Despawn(true); return; }
        }
        sinceStrike += dt;
        if (sinceStrike < 0.8f)
        {
            transform.position += Vector3.right * (speed * dt);
            return;
        }
        Vector3 pos = transform.position;
        Vector3 goal = target.bounds.ClosestPoint(pos);
        goal.z = pos.z;
        bool inRange = Unit.type == UnitType.Archer
            ? pos.x <= screenRightX - Unit.screenInset
            : Vector2.Distance(pos, goal) <= Unit.attackRange;
        if (!inRange)
        {
            transform.position = Vector3.MoveTowards(pos, goal, speed * (sinceStrike < 2.2f ? 0.4f : 1f) * EnemyRope.MonsterSpeedMultiplier * dt);
            return;
        }

        if (Unit.type == UnitType.Chaser)
        {
            if (rope != null) rope.Attach(Unit.ropeSkill, target.transform, goal);
            return;
        }

        attackTimer -= dt;
        if (attackTimer > 0f) return;
        attackTimer = Unit.attackInterval;

        if (Unit.type == UnitType.Lancer)
        {
            if (engagementAge < 0) engagementAge = 0;
            HitCart(attack);
            sinceStrike = 0;
        }
        else if (Unit.arrowPrefab != null)
        {
            var shot=Instantiate(Unit.arrowPrefab,pos,Quaternion.identity);
            shot.GetComponent<Arrow>().Launch(goal,Unit.arrowSpeed,attack);
            shot.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}

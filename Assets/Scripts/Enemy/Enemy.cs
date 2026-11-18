using System;
using UnityEngine;

// 웨이브 배율이 적용된 값으로 초기화되고, 마차 몸체 가장자리를 향해 일정 속도로 다가온다.
// 사거리 안에 들어오면 유닛 종류(UnitSO.type)에 따라 공격한다.
//   Lancer → attackInterval마다 근접 공격
//   Archer → 화면 안(screenInset)에 들어오면 멈춰서 attackInterval마다 화살 발사
//   Chaser → 로프를 건다 (EnemyRope)
public class Enemy : MonoBehaviour
{
    // 마차 피해 신호. EnemySpawner가 마차 내구도(CartStations)에 연결한다.
    public static event Action<float> CartHit;
    public static void HitCart(float damage) => CartHit?.Invoke(damage);

    public UnitSO Unit { get; private set; }

    private SpriteRenderer target;
    private EnemyRope rope;
    private float speed;
    private float attack;
    private float hp;
    private float attackTimer;
    private float screenRightX;

    private void Awake() => TryGetComponent(out rope);

    public void Init(UnitSO unit, WaveSO wave, SpriteRenderer cartBody, float baseScreenRightX)
    {
        Unit = unit;
        speed = wave.RollSpeed(unit);
        attack = wave.AttackOf(unit);
        hp = unit.hp;
        target = cartBody;
        screenRightX = baseScreenRightX;

        if (unit.type == UnitType.Archer && unit.arrowPrefab == null)
            Debug.LogWarning($"[{unit.name}] 궁수인데 화살 프리팹이 없어 공격하지 않습니다", unit);
        if (unit.type == UnitType.Chaser && rope == null)
            Debug.LogWarning($"[{name}] 추격조 프리팹에 EnemyRope가 없어 로프를 걸지 않습니다", this);
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;
        if (hp <= 0f) Destroy(gameObject);
    }

    private void Update()
    {
        if (rope != null && rope.Attached) return;

        Vector3 pos = transform.position;
        Vector3 goal = target.bounds.ClosestPoint(pos);
        goal.z = pos.z;
        bool inRange = Unit.type == UnitType.Archer
            ? pos.x <= screenRightX - Unit.screenInset
            : Vector2.Distance(pos, goal) <= Unit.attackRange;
        if (!inRange)
        {
            transform.position = Vector3.MoveTowards(pos, goal, speed * EnemyRope.MonsterSpeedMultiplier * Time.deltaTime);
            return;
        }

        if (Unit.type == UnitType.Chaser)
        {
            if (rope != null) rope.Attach(Unit.ropeSkill, target.transform, goal);
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;
        attackTimer = Unit.attackInterval;

        if (Unit.type == UnitType.Lancer)
            HitCart(attack);
        else if (Unit.arrowPrefab != null)
            Instantiate(Unit.arrowPrefab, pos, Quaternion.identity, transform.parent).GetComponent<Arrow>().Launch(goal, Unit.arrowSpeed, attack);
    }
}

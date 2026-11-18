using System;
using System.Collections.Generic;
using UnityEngine;

// 생성 시 웨이브 배율이 적용된 값으로 초기화되고, 마차 몸체 가장자리를 향해 일정 속도로 다가온다.
// 사거리 안에 들어오면 유닛 종류(UnitSO.type)에 따라 공격한다.
//   Chaser → 로프를 건다(줄 표시, 마차 감속, 몬스터 전체 가속). 이 몬스터가 사라지면 풀림
//   Archer → attackInterval마다 화살 발사
//   Lancer → attackInterval마다 근접 공격
public class Enemy : MonoBehaviour
{
    // 마차 피해 신호. 마차 내구도 담당 코드가 구독한다.
    public static event Action<float> CartHit;
    public static void HitCart(float damage) => CartHit?.Invoke(damage);

    // 현재 로프를 건 추격조들
    // ponytail: 계수는 첫 로프의 SO를 쓴다. 추격조마다 다른 로프 SO를 쓰게 되면 합산 규칙을 정할 것
    public static readonly List<Enemy> Ropers = new();
    static RopeSkillSO ActiveRope => Ropers.Count > 0 ? Ropers[0].Unit.ropeSkill : null;
    public static float CartSpeedMultiplier => ActiveRope ? ActiveRope.CartSpeedMultiplier(Ropers.Count) : 1f;
    public static float MonsterSpeedMultiplier => ActiveRope ? ActiveRope.MonsterSpeedMultiplier(Ropers.Count) : 1f;

    public UnitSO Unit { get; private set; }
    public float Speed { get; private set; }
    public float Attack { get; private set; }
    public float Hp { get; private set; }
    public bool InRange { get; private set; }
    public bool Roped => Ropers.Contains(this);

    [SerializeField] LineRenderer rope; // 추격조 프리팹에만. 꺼둔 상태로 두면 로프를 걸 때 켜진다

    SpriteRenderer target;
    Vector3 ropeEnd;
    float attackTimer;

    public void Init(UnitSO unit, WaveSO wave, SpriteRenderer cartBody)
    {
        Unit = unit;
        Speed = wave.RollSpeed(unit);
        Attack = wave.AttackOf(unit);
        Hp = unit.hp;
        target = cartBody;
    }

    public void TakeDamage(float damage)
    {
        Hp -= damage;
        if (Hp <= 0f) Destroy(gameObject);
    }

    void Update()
    {
        if (Roped) return;

        Vector3 pos = transform.position;
        Vector3 goal = target.bounds.ClosestPoint(pos);
        goal.z = pos.z;
        InRange = Vector2.Distance(pos, goal) <= Unit.attackRange;
        if (!InRange)
        {
            transform.position = Vector3.MoveTowards(pos, goal, Speed * MonsterSpeedMultiplier * Time.deltaTime);
            return;
        }

        if (Unit.type == UnitType.Chaser)
        {
            AttachRope(goal);
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;
        attackTimer = Unit.attackInterval;

        if (Unit.type == UnitType.Archer)
            Instantiate(Unit.arrowPrefab, pos, Quaternion.identity).GetComponent<Arrow>().Launch(goal, Unit.arrowSpeed, Attack);
        else
            HitCart(Attack);
    }

    void AttachRope(Vector3 end)
    {
        ropeEnd = end;
        Ropers.Add(this);
        if (rope == null) return;
        rope.positionCount = 2;
        rope.enabled = true;
    }

    void LateUpdate()
    {
        if (rope == null || !rope.enabled) return;
        rope.SetPosition(0, transform.position);
        rope.SetPosition(1, ropeEnd);
    }

    void OnDisable() => Ropers.Remove(this);
}

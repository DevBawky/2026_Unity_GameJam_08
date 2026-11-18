using UnityEngine;

// 생성 시 웨이브 배율이 적용된 값으로 초기화되고, 마차 몸체 가장자리를 향해 일정 속도로 다가온다.
// 마차는 화면에 고정이므로 이 속도는 마차 기준 접근 속도다.
public class Enemy : MonoBehaviour
{
    public UnitSO Unit { get; private set; }
    public float Speed { get; private set; }
    public float Attack { get; private set; }
    public float Hp { get; private set; }
    public bool InRange { get; private set; }

    SpriteRenderer target;

    public void Init(UnitSO unit, WaveSO wave, SpriteRenderer cartBody)
    {
        Unit = unit;
        Speed = wave.RollSpeed(unit);
        Attack = wave.AttackOf(unit);
        Hp = unit.hp;
        target = cartBody;
    }

    void Update()
    {
        Vector2 pos = transform.position;
        Vector2 goal = target.bounds.ClosestPoint(pos);
        InRange = Vector2.Distance(pos, goal) <= Unit.attackRange;
        if (!InRange)
            transform.position = Vector2.MoveTowards(pos, goal, Speed * Time.deltaTime);
    }
}

using UnityEngine;
using System.Collections.Generic;

// 추격조 로프. 사거리 안에서 마차에 로프를 걸고, 이 몬스터가 사라지면 풀린다(유지 시간 없음).
// 걸린 로프 수로 마차 감속(CartMotion)과 몬스터 전체 가속(Enemy)을 계산한다.
[RequireComponent(typeof(LineRenderer))]
public class EnemyRope : MonoBehaviour
{
    private static readonly List<EnemyRope> attached = new();

    // 추후 변경점: 계수는 첫 로프의 SO를 쓴다. 추격조마다 다른 로프 SO를 쓰게 되면 합산 규칙을 정할 것
    private static RopeSkillSO Active => attached.Count > 0 ? attached[0].skill : null;

    public static int Count => attached.Count;
    public static float CartSpeedMultiplier => Active ? Active.CartSpeedMultiplier(attached.Count) : 1f;
    public static float MonsterSpeedMultiplier => Active ? Active.MonsterSpeedMultiplier(attached.Count) : 1f;

    public bool Attached { get; private set; }

    private LineRenderer line;
    private RopeSkillSO skill;
    private Transform cart;
    private Vector3 bodyOffset;
    private Vector3 endOffset;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.enabled = false;
    }

    // 마차가 위아래로 움직이므로 추격조와 줄 끝을 마차 기준 위치로 붙잡아 같이 움직인다.
    public void Attach(RopeSkillSO ropeSkill, Transform cartTransform, Vector3 cartPoint)
    {
        skill = ropeSkill;
        cart = cartTransform;
        bodyOffset = transform.position - cart.position;
        endOffset = cartPoint - cart.position;
        Attached = true;
        attached.Add(this);
        line.enabled = true;
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (!Attached) return;
        transform.position = cart.position + bodyOffset;
        line.SetPosition(0, transform.position);
        line.SetPosition(1, cart.position + endOffset);
    }

    private void OnDisable()
    {
        attached.Remove(this);
        Attached = false;
    }
}

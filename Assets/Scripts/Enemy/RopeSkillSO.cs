using UnityEngine;

// 추격조 로프 어택. 유지 시간 없음 — 로프를 건 추격조가 사라지면 풀린다.
// 중첩: 효과 = 로프 1개당 값 × (로프 수 ^ 중첩 계수). 공식은 미정이라 계수로 조정한다.
[CreateAssetMenu(menuName = "GameJam/Enemy/Skill/Rope", fileName = "Skill_Rope")]
public class RopeSkillSO : ScriptableObject
{
    [Tooltip("로프 1개당 마차 감속 비율 (소수). 0.2 = 마차 속도 20% 감소. 배경 스크롤·남은 거리 감소도 같이 느려진다")]
    [Min(0)] public float cartSlowPerRope = 0.2f;

    [Tooltip("마차 감속 상한 (소수). 0.8 = 아무리 중첩돼도 최대 80% 감소")]
    [Range(0, 1)] public float maxCartSlow = 0.8f;

    [Tooltip("로프 1개당 몬스터 전체 이동 속도 증가 비율 (소수). 0.1 = 10% 빨라짐")]
    [Min(0)] public float monsterSpeedBonusPerRope = 0.1f;

    [Tooltip("중첩 계수. 효과 = 1개당 값 × 로프 수^계수\n1 = 로프 수에 정비례, 0.5 = 갈수록 덜 늘어남, 0 = 중첩 안 됨")]
    [Min(0)] public float stackExponent = 1f;

    float Stack(int ropes) => ropes <= 0 ? 0f : Mathf.Pow(ropes, stackExponent);

    public float CartSpeedMultiplier(int ropes) => 1f - Mathf.Min(maxCartSlow, cartSlowPerRope * Stack(ropes));

    public float MonsterSpeedMultiplier(int ropes) => 1f + monsterSpeedBonusPerRope * Stack(ropes);
}

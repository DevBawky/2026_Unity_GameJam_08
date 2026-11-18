using UnityEngine;

// 몬스터 종류별 기본값. 웨이브(WaveSO)가 여기에 배율을 곱해 쓴다.
[CreateAssetMenu(menuName = "GameJam/Enemy/Unit", fileName = "Unit_")]
public class UnitSO : ScriptableObject
{
    [Tooltip("소환할 몬스터 프리팹 (Enemy 컴포넌트 필요)")]
    public GameObject prefab;

    [Tooltip("기본 이동 속도 (m/s). 웨이브의 속도 Min/Max가 이 값에 곱해진다")]
    [Min(0)] public float moveSpeed = 3f;

    [Tooltip("기본 공격력. 웨이브의 공격력 배율이 이 값에 곱해진다")]
    [Min(0)] public float attackPower = 10f;

    [Tooltip("사거리 (m). 마차 가장자리까지 이 거리 안에 들어오면 멈춘다. 웨이브에서 조정하지 않는 유닛 고유값")]
    [Min(0)] public float attackRange = 1f;

    [Tooltip("공격 간격 (초). 공격하는 유닛만 사용")]
    [Min(0)] public float attackInterval = 1f;

    [Tooltip("체력 (미확정). 대포 한 방에 처치되면 1로 둔다")]
    [Min(0)] public float hp = 1f;

    [Header("원거리 (궁수)")]
    [Tooltip("화살 프리팹 (Arrow 컴포넌트 필요). 비워두면 근접 공격")]
    public GameObject arrowPrefab;

    [Tooltip("화살 비행 속도 (m/s)")]
    [Min(0.1f)] public float arrowSpeed = 12f;

    [Header("스킬 (추격조)")]
    [Tooltip("로프 스킬. 넣으면 사거리 안에서 공격 대신 로프를 건다")]
    public RopeSkillSO ropeSkill;
}

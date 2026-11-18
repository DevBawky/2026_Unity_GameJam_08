using UnityEngine;

public enum UnitType
{
    Lancer, // 근접 기마병: 사거리 안에서 마차를 직접 공격
    Archer, // 궁수 기마병: 사거리 안에서 화살 발사
    Chaser, // 추격조: 사거리 안에서 로프를 걸어 마차 감속
}

// 몬스터 종류별 기본값. 웨이브(WaveSO)가 여기에 배율을 곱해 쓴다.
// 인스펙터는 type에 따라 필요한 칸만 보인다 (Editor/UnitSOEditor).
[CreateAssetMenu(menuName = "GameJam/Enemy/Unit", fileName = "Unit_")]
public class UnitSO : ScriptableObject
{
    [Tooltip("몬스터 종류. 종류에 따라 아래 표시되는 칸이 바뀐다\nLancer = 근접, Archer = 궁수, Chaser = 추격조(로프)")]
    public UnitType type;

    [Tooltip("소환할 몬스터 프리팹 (Enemy 컴포넌트 필요)")]
    public GameObject prefab;

    [Tooltip("기본 이동 속도 (m/s). 웨이브의 속도 Min/Max가 이 값에 곱해진다")]
    [Min(0)] public float moveSpeed = 3f;

    [Tooltip("기본 공격력. 웨이브의 공격력 배율이 이 값에 곱해진다")]
    [Min(0)] public float attackPower = 10f;

    [Tooltip("사거리 (m). 마차 가장자리까지 이 거리 안에 들어오면 멈춘다. 웨이브에서 조정하지 않는 유닛 고유값")]
    [Min(0)] public float attackRange = 1f;

    [Tooltip("공격 간격 (초)")]
    [Min(0)] public float attackInterval = 1f;

    [Tooltip("체력 (미확정). 대포 한 방에 처치되면 1로 둔다")]
    [Min(0)] public float hp = 1f;

    [Header("궁수")]
    [Tooltip("화살 프리팹 (Arrow 컴포넌트 필요)")]
    public GameObject arrowPrefab;

    [Tooltip("화살 비행 속도 (m/s)")]
    [Min(0.1f)] public float arrowSpeed = 12f;

    [Header("추격조")]
    [Tooltip("로프 스킬. 사거리 안에서 공격 대신 로프를 건다")]
    public RopeSkillSO ropeSkill;

    private void OnValidate()
    {
        if (type == UnitType.Archer && arrowPrefab == null) Debug.LogWarning($"[{name}] 궁수인데 화살 프리팹이 비어 있습니다", this);
        if (type == UnitType.Chaser && ropeSkill == null) Debug.LogWarning($"[{name}] 추격조인데 로프 스킬이 비어 있습니다", this);
    }
}

using UnityEngine;

// 몬스터 종류별 기본값. 웨이브(WaveSO)가 여기에 배율을 곱해 쓴다.
[CreateAssetMenu(menuName = "GameJam/Enemy/Unit", fileName = "Unit_")]
public class UnitSO : ScriptableObject
{
    public GameObject prefab;
    [Min(0)] public float moveSpeed = 3f;      // m/s
    [Min(0)] public float attackPower = 10f;
    [Min(0)] public float attackRange = 1f;    // 유닛 고유값, 웨이브에서 조정 안 함
    [Min(0)] public float attackInterval = 1f; // 공격 간격(초). 공격하지 않는 유닛은 무시
    [Min(0)] public float hp = 1f;             // 미확정: 대포 한 방이면 1로 둔다
}

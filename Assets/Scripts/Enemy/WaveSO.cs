using System;
using System.Collections.Generic;
using UnityEngine;

// 웨이브 하나. startTime(스테이지 시작 기준 초)에 spawns 전체를 한 프레임에 소환한다.
// 수(count)는 기획자가 "스테이지 기본 수 × 웨이브 배율"로 계산해 직접 입력한다.
[CreateAssetMenu(menuName = "GameJam/Enemy/Wave", fileName = "Wave_")]
public class WaveSO : ScriptableObject
{
    [Serializable]
    public struct Spawn
    {
        public UnitSO unit;
        [Min(0)] public int count;
    }

    const float RecommendedSpeedSpread = 0.05f; // 몬스터 간 속도 차이 권장 상한(미확정)

    [Min(0)] public float startTime;
    public List<Spawn> spawns = new();

    [Header("이동 속도: 기본 × Random(1 - Min, 1 + Max)")]
    [Range(0, 1)] public float speedMin;
    [Min(0)] public float speedMax;

    [Header("공격력: 기본 × 배율")]
    [Min(0)] public float attackMultiplier = 1f;

    public float RollSpeed(UnitSO unit) => unit.moveSpeed * UnityEngine.Random.Range(1f - speedMin, 1f + speedMax);

    public float AttackOf(UnitSO unit) => unit.attackPower * attackMultiplier;

    void OnValidate()
    {
        if (speedMin + speedMax > RecommendedSpeedSpread)
            Debug.LogWarning($"[{name}] 속도 범위 Min+Max = {speedMin + speedMax:P0} (권장 {RecommendedSpeedSpread:P0} 이하)", this);
    }
}

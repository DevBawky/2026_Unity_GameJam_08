using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

// 웨이브 하나. waveStartTime(스테이지 시작 기준 초)에 spawns 전체를 한 프레임에 소환한다.
// 수(count)는 기획자가 "스테이지 기본 수 × 웨이브 배율"로 계산해 직접 입력한다.
[CreateAssetMenu(menuName = "GameJam/Enemy/Wave", fileName = "Wave_")]
public class WaveSO : ScriptableObject
{
    [Serializable]
    public struct Spawn
    {
        [Tooltip("등장할 몬스터 종류")]
        public UnitSO unit;
        [Tooltip("등장 수. \"스테이지 기본 수 × 웨이브 배율\"로 계산해 입력")]
        [Min(0)] public int count;
    }

    private const float RecommendedSpeedSpread = 0.05f; // 몬스터 간 속도 차이 권장 상한(미확정)

    [Tooltip("스테이지 시작부터 몇 초 뒤에 등장하는지. 이 시점에 아래 몬스터 전부를 한 번에 소환한다")]
    [FormerlySerializedAs("startTime")]
    [Min(0)] public float waveStartTime;

    [Tooltip("이 웨이브에 등장하는 몬스터 종류와 수")]
    public List<Spawn> spawns = new();

    [Header("이동 속도: 기본 × Random(1 - Min, 1 + Max)")]
    [Tooltip("느려지는 쪽 폭 (소수). 0.05 = 기본 속도의 최대 5% 느림. 클수록 느린 몬스터가 나올 확률이 높다")]
    [Min(0)] public float speedMin;
    [Tooltip("빨라지는 쪽 폭 (소수). 0.05 = 기본 속도의 최대 5% 빠름. Min + Max는 0.05 이하 권장")]
    [Min(0)] public float speedMax;

    [Header("공격력: 기본 × 배율")]
    [Tooltip("이 웨이브 몬스터의 공격력 배율. 1 = 기본값 그대로")]
    [Min(0)] public float attackMultiplier = 1f;

    public float RollSpeed(UnitSO unit) => unit.moveSpeed * UnityEngine.Random.Range(1f - speedMin, 1f + speedMax);

    public float AttackOf(UnitSO unit) => unit.attackPower * attackMultiplier;

    private void OnValidate()
    {
        speedMin = Mathf.Min(speedMin, 0.99f); // 1 이상이면 속도 계수가 0 이하가 된다
        if (speedMin + speedMax > RecommendedSpeedSpread)
            Debug.LogWarning($"[{name}] 속도 범위 Min+Max = {speedMin + speedMax:P0} (권장 {RecommendedSpeedSpread:P0} 이하)", this);
    }
}

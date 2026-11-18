using System.Collections.Generic;
using UnityEngine;

// 스테이지 하나 = 웨이브 목록. 웨이브는 startTime 오름차순으로 둔다.
[CreateAssetMenu(menuName = "GameJam/Enemy/Stage", fileName = "Stage_")]
public class StageSO : ScriptableObject
{
    public List<WaveSO> waves = new();

    void OnValidate()
    {
        for (int i = 1; i < waves.Count; i++)
            if (waves[i] != null && waves[i - 1] != null && waves[i].startTime < waves[i - 1].startTime)
                Debug.LogWarning($"[{name}] 웨이브 {i}({waves[i].name})의 시작 시간이 앞 웨이브보다 빠릅니다", this);
    }
}

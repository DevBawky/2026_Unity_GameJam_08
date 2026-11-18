using UnityEngine;

// 스폰 영역 중심에 둔다(카메라 최대 범위 밖). 스테이지 시작부터 경과 시간이 웨이브 startTime을 지나면
// 그 웨이브의 몬스터를 한 프레임에 전부 영역 안 무작위 위치에 소환한다.
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] StageSO stage;
    [SerializeField] SpriteRenderer cartBody;
    [SerializeField] Vector2 areaSize = new(4f, 8f);

    float elapsed;
    int nextWave;

    void Update()
    {
        elapsed += Time.deltaTime;
        while (nextWave < stage.waves.Count && stage.waves[nextWave].startTime <= elapsed)
            SpawnWave(stage.waves[nextWave++]);
    }

    void SpawnWave(WaveSO wave)
    {
        foreach (var s in wave.spawns)
            for (int i = 0; i < s.count; i++)
            {
                var offset = new Vector3(Random.Range(-0.5f, 0.5f) * areaSize.x, Random.Range(-0.5f, 0.5f) * areaSize.y);
                var go = Instantiate(s.unit.prefab, transform.position + offset, Quaternion.identity, transform);
                go.GetComponent<Enemy>().Init(s.unit, wave, cartBody);
            }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}

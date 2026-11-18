using System;
using UnityEngine;
using Unity.Netcode;
using Random = UnityEngine.Random;

// 스폰 영역 중심에 둔다(카메라 최대 범위 밖). 스테이지 시작부터 경과 시간이 웨이브 waveStartTime을 지나면
// 그 웨이브의 몬스터를 한 프레임에 전부 영역 안 무작위 위치에 소환한다.
// 다음 스테이지로 넘어가면 웨이브를 처음부터 다시 돌린다.
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private StageSO stage;
    [SerializeField] private SpriteRenderer cartBody;
    [SerializeField] private CartStations cartStations;
    [SerializeField] private Vector2 areaSize = new(4f, 8f);

    private float elapsed;
    private int nextWave;
    private int stageNumber;
    private float baseScreenRightX;
    // 씬이 바뀔 때 CartStations가 먼저 파괴되면 cartStations != null 검사가 false라 해제를 못 한다.
    // 구독한 델리게이트를 들고 있다가 그대로 뺀다.
    private Action<float> cartHit;

    // 대포 조준 등으로 카메라가 움직이기 전, 기본 화면의 오른쪽 끝 (궁수 정지 기준)
    private void Awake()
    {
        Camera cam = Camera.main;
        baseScreenRightX = cam.transform.position.x + cam.orthographicSize * cam.aspect;
    }

    private void OnEnable()
    {
        if (cartStations == null)
        {
            Debug.LogWarning($"[{name}] cartStations가 비어 있어 몬스터 공격이 마차 내구도에 반영되지 않습니다", this);
            return;
        }
        cartHit = cartStations.ReceiveCollision;
        Enemy.CartHit += cartHit;
        stageNumber = cartStations.StageNumber;
    }

    private void OnDisable()
    {
        Enemy.CartHit -= cartHit;
        cartHit = null;
    }

    private void Update()
    {
        if (cartStations == null || !cartStations.IsServer || !CartStations.Running) return;
        if (cartStations.StageNumber != stageNumber) RestartWaves();

        elapsed += Time.deltaTime;
        while (nextWave < stage.waves.Count && stage.waves[nextWave].waveStartTime <= elapsed)
            SpawnWave(stage.waves[nextWave++]);
    }

    // 남은 몬스터와 날아가던 화살(둘 다 이 오브젝트의 자식)을 치우고 타이머를 처음으로 돌린다.
    private void RestartWaves()
    {
        stageNumber = cartStations.StageNumber;
        elapsed = 0f;
        nextWave = 0;
        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (enemy.IsSpawned) enemy.NetworkObject.Despawn(true);
        foreach (var arrow in FindObjectsByType<Arrow>(FindObjectsSortMode.None))
            if (arrow.IsSpawned) arrow.NetworkObject.Despawn(true);
    }

    private void SpawnWave(WaveSO wave)
    {
        foreach (var s in wave.spawns)
            for (int i = 0; i < s.count; i++)
            {
                var offset = new Vector3(Random.Range(-0.5f, 0.5f) * areaSize.x, Random.Range(-0.5f, 0.5f) * areaSize.y);
                var go = Instantiate(s.unit.prefab, transform.position + offset, Quaternion.identity);
                go.GetComponent<Enemy>().Init(s.unit,wave,cartBody,baseScreenRightX);
                go.GetComponent<NetworkObject>().Spawn(true);
            }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}

using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// 마차 앞쪽(왼쪽) 멀리에 바위·통나무를 소환해 배경과 같은 속도로 흘려보내고,
// 마차 몸체에 닿으면 CartStations.ReceiveCollision으로 피해를 준다.
// 한 프레임 이동량이 커도 지나치지 않게 이전·다음 위치 사이에서 충돌을 판정한다.
public sealed class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private CartMotion cart;
    [SerializeField] private CartStations stations;
    [SerializeField] private SpriteRenderer cartBody;
    [SerializeField] private Obstacle rockPrefab;
    [SerializeField] private Obstacle logPrefab;
    [SerializeField, Range(0f, 1f)] private float logProbability = 0.4f;
    [SerializeField, Min(0.1f)] private float minSpawnInterval = 3f;
    [SerializeField, Min(0.1f)] private float maxSpawnInterval = 5f;
    [Tooltip("스테이지가 하나 오를 때마다 소환 간격에 곱하는 값")]
    [SerializeField, Range(0.1f, 1f)] private float spawnIntervalPerStage = 0.9f;
    [Tooltip("마차 앞면에서 이만큼 앞(왼쪽)에 소환")]
    [SerializeField, Min(1f)] private float spawnDistance = 45f;
    [Tooltip("길 중앙 기준 위아래 소환 범위")]
    [SerializeField, Min(0f)] private float spawnSideRange = 4f;
    [Tooltip("마차 몸체 높이 중 충돌로 치는 비율 (가장자리로 스치면 피함)")]
    [SerializeField, Range(0.05f, 1f)] private float collisionHeightFraction = 0.65f;

    private readonly List<Obstacle> active = new();
    private float nextSpawnTime;

    private void Start() => Schedule();

    // 다음 스테이지 시작 시 CartStations가 호출한다.
    public void ResetForStage()
    {
        foreach (Obstacle obstacle in active) if (obstacle != null) obstacle.GetComponent<NetworkObject>().Despawn(true);
        active.Clear();
        Schedule();
    }

    private void Schedule()
    {
        float factor = Mathf.Pow(spawnIntervalPerStage, stations.StageNumber - 1);
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, Mathf.Max(minSpawnInterval, maxSpawnInterval)) * factor;
    }

    private void Spawn()
    {
        Obstacle prefab = Random.value < logProbability ? logPrefab : rockPrefab;
        var position = new Vector3(cartBody.bounds.min.x - spawnDistance, Random.Range(-spawnSideRange, spawnSideRange), 0f);
        var item=Instantiate(prefab,position,Quaternion.identity);
        item.GetComponent<NetworkObject>().Spawn(true);
        active.Add(item);
    }

    private void Update()
    {
        if (!stations.IsServer || !CartStations.Running) return;
        if (Time.time >= nextSpawnTime)
        {
            Spawn();
            Schedule();
        }

        Bounds cartBounds = cartBody.bounds;
        float cartHalfHeight = cartBounds.extents.y * collisionHeightFraction;
        Vector3 movement = cart.ScrollVelocity * Time.deltaTime;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Obstacle obstacle = active[i];
            if (obstacle == null) { active.RemoveAt(i); continue; }

            Vector3 previous = obstacle.transform.position;
            Vector3 next = previous + movement;
            obstacle.transform.position = next;

            float halfWidth = obstacle.Size.x * 0.5f;
            float hitX = cartBounds.min.x - halfWidth;
            bool crossed = previous.x <= cartBounds.max.x + halfWidth && next.x >= hitX;
            float hitT = Mathf.Abs(next.x - previous.x) > 0.0001f ? Mathf.Clamp01((hitX - previous.x) / (next.x - previous.x)) : 0f;
            float hitY = Mathf.Lerp(previous.y, next.y, hitT);
            if (crossed && Mathf.Abs(hitY - cartBounds.center.y) <= cartHalfHeight + obstacle.Size.y * 0.5f)
            {
                obstacle.GetComponent<NetworkObject>().Despawn(true);
                active.RemoveAt(i);
                stations.ReceiveCollision(obstacle.Damage);
                if (stations.Failed) return;
            }
            else if (next.x > cartBounds.max.x + spawnDistance)
            {
                obstacle.GetComponent<NetworkObject>().Despawn(true);
                active.RemoveAt(i);
            }
        }
    }
}

using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// 마차 앞쪽(왼쪽) 멀리에 도로 장애물(바위·통나무·사슴)을 소환해 배경과 같은 속도로 흘려보낸다.
// 소환 간격은 주행 거리 기준이라 빨리 달릴수록 자주 만난다(웹판 규칙).
// 마차 앞면에 닿으면 종류별 감속(CartMotion)과 피해(CartStations.ReceiveCollision)를 준다.
// 한 프레임 이동량이 커도 지나치지 않게 이전·다음 위치 사이에서 충돌을 판정한다.
public sealed class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private CartMotion cart;
    [SerializeField] private CartStations stations;
    [SerializeField] private SpriteRenderer cartBody;
    [Tooltip("같은 확률로 고르는 장애물 프리팹 목록")]
    [SerializeField] private Obstacle[] prefabs;

    [Header("Spawn gap (주행 거리 m)")]
    [Tooltip("1스테이지 기본 간격")]
    [SerializeField, Min(1f)] private float baseGap = 76f;
    [Tooltip("스테이지가 하나 오를 때마다 줄어드는 간격")]
    [SerializeField, Min(0f)] private float gapPerStage = 4f;
    [Tooltip("간격 최솟값")]
    [SerializeField, Min(1f)] private float minGap = 32f;
    [Tooltip("기본 간격에 더하는 무작위 범위")]
    [SerializeField, Min(0f)] private float randomGap = 23f;

    [Header("Placement")]
    [Tooltip("마차 앞면에서 이만큼 앞(왼쪽)에 소환. 화면 왼쪽 끝 바로 밖이 되게 맞춘다")]
    [SerializeField, Min(1f)] private float spawnDistance = 9.5f;
    [Tooltip("길 중앙 기준 위아래 소환 범위")]
    [SerializeField, Min(0f)] private float spawnSideRange = 4f;
    [Tooltip("마차 몸체 높이 중 충돌로 치는 비율 (가장자리로 스치면 피함)")]
    [SerializeField, Range(0.05f, 1f)] private float collisionHeightFraction = 0.65f;

    private readonly List<Obstacle> active = new();
    private float nextSpawnDistance;

    private void OnEnable()
    {
        if (stations != null) stations.CollectDrivingHazards += CollectDrivingHazards;
    }

    private void OnDisable()
    {
        if (stations != null) stations.CollectDrivingHazards -= CollectDrivingHazards;
    }

    private void CollectDrivingHazards(List<CartDrivingHazard> hazards)
    {
        Bounds body = cartBody.bounds;
        float speed = cart.ScrollVelocity.x;
        if (speed <= 0f) return;
        float centerOffset = body.center.y - cart.transform.position.y;
        foreach (var obstacle in active)
        {
            if (obstacle == null) continue;
            Vector2 pos = obstacle.transform.position;
            Vector2 half = obstacle.Size * .5f;
            float enter = (body.min.x - half.x - pos.x) / speed;
            float exit = (body.max.x + half.x - pos.x) / speed;
            if (exit < 0 || enter > CartDrivingHazard.Lookahead) continue;
            float radius = body.extents.y * collisionHeightFraction + half.y;
            hazards.Add(new CartDrivingHazard(pos.y - centerOffset, radius, enter, exit));
        }
    }

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
        float gap = Mathf.Max(minGap, baseGap - (stations.StageNumber - 1) * gapPerStage);
        nextSpawnDistance = cart.Distance + gap + Random.Range(0f, randomGap);
    }

    private void Spawn()
    {
        Obstacle prefab = prefabs[Random.Range(0, prefabs.Length)];
        var position = new Vector3(cartBody.bounds.min.x - spawnDistance, Random.Range(-spawnSideRange, spawnSideRange), 0f);
        Obstacle item = Instantiate(prefab, position, Quaternion.identity);
        item.GetComponent<NetworkObject>().Spawn(true);
        active.Add(item);
    }

    private void Update()
    {
        if (!stations.IsServer || !CartStations.Running) return;
        if (prefabs.Length > 0 && cart.Distance >= nextSpawnDistance)
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
                cart.ApplyObstacleSlow(obstacle.SlowFactor, obstacle.SlowSeconds);
                stations.ReceiveCollision(obstacle.Damage);
                obstacle.GetComponent<NetworkObject>().Despawn(true);
                active.RemoveAt(i);
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

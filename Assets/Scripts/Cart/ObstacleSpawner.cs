using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// 도로 장애물(바위·통나무·사슴). 웹판 v10 규칙:
// 위·아래 바깥 차선 중 한 곳을 정해 4초 전에 "!"(ObstacleWarning)로 예고한 뒤, 마차 앞쪽(왼쪽) 멀리 같은 차선에 소환한다.
// 예고 간격은 주행 거리 기준(첫 42m, 이후 125~150m)이고, 앞에 지나가지 않은 장애물이 있으면 다음 예고를 미룬다.
// 소환된 장애물은 배경과 같은 속도로 흘러가며, 마차 앞면에 닿으면 종류별 감속(CartMotion)과 피해(CartStations.ReceiveCollision)를 준다.
// 한 프레임 이동량이 커도 지나치지 않게 이전·다음 위치 사이에서 충돌을 판정한다.
// 투석기와 겹치지 않게: 날아오는 바위가 장애물이 마차 앞을 지나는 동안 떨어지면 소환을 1초 미루고(차선 유지),
// 투석기는 StrikeOverlapsObstacle·HasEscapeLane으로 발사 시점과 착탄 쪽을 정한다(웹판 v10 scheduleRoadObstacle·sharedEscapeLane).
public sealed class ObstacleSpawner : MonoBehaviour
{
    // 웹판 ROAD_STRIKE_MARGIN: 착탄과 장애물 통과 사이에 두는 여유(초)
    private const float StrikeMargin = 1f;

    [SerializeField] private CartMotion cart;
    [SerializeField] private CartStations stations;
    [SerializeField] private SpriteRenderer cartBody;
    [Tooltip("씬의 투석기. 비어 있으면 겹침 처리를 하지 않는다")]
    [SerializeField] private CatapultStrike catapult;
    [Tooltip("같은 확률로 고르는 장애물 프리팹 목록")]
    [SerializeField] private Obstacle[] prefabs;
    [Tooltip("예고 표시 프리팹 (NetworkObject + ObstacleWarning)")]
    [SerializeField] private NetworkObject warningPrefab;

    [Header("Warning (주행 거리 m)")]
    [Tooltip("스테이지 시작 후 첫 예고 거리")]
    [SerializeField, Min(0f)] private float firstWarningDistance = 42f;
    [Tooltip("예고 후 소환까지 시간(초)")]
    [SerializeField, Min(0f)] private float warningSeconds = 4f;
    [Tooltip("1스테이지 기본 예고 간격")]
    [SerializeField, Min(1f)] private float baseGap = 138f;
    [Tooltip("스테이지가 하나 오를 때마다 줄어드는 간격")]
    [SerializeField, Min(0f)] private float gapPerStage = 4f;
    [Tooltip("간격 최솟값")]
    [SerializeField, Min(1f)] private float minGap = 125f;
    [Tooltip("기본 간격에 더하는 무작위 범위")]
    [SerializeField, Min(0f)] private float randomGap = 12f;

    [Header("Placement")]
    [Tooltip("마차 앞면에서 이만큼 앞(왼쪽)에 소환. 화면 왼쪽 끝 바로 밖이 되게 맞춘다")]
    [SerializeField, Min(1f)] private float spawnDistance = 9.5f;
    [Tooltip("위·아래 바깥 차선 높이(±). 마차 차선 범위 CartMotion.LateralLimit와 맞춘다")]
    [SerializeField, Min(0f)] private float outerLaneY = 2f;
    [Tooltip("마차 몸체 높이 중 충돌로 치는 비율 (가장자리로 스치면 피함)")]
    [SerializeField, Range(0.05f, 1f)] private float collisionHeightFraction = 0.65f;

    private readonly List<Obstacle> active = new();
    private float nextWarningDistance;
    private Obstacle pendingPrefab;
    private float pendingY;
    private float pendingRemaining;
    private NetworkObject pendingMarker;

    private void Start() => nextWarningDistance = cart.Distance + firstWarningDistance;

    // 다음 스테이지 시작 시 CartStations가 호출한다.
    public void ResetForStage()
    {
        foreach (Obstacle obstacle in active) if (obstacle != null) obstacle.GetComponent<NetworkObject>().Despawn(true);
        active.Clear();
        ClearWarning();
        nextWarningDistance = cart.Distance + firstWarningDistance;
    }

    private float SpawnX => cartBody.bounds.min.x - spawnDistance;

    private void Warn()
    {
        pendingPrefab = prefabs[Random.Range(0, prefabs.Length)];
        pendingY = Random.value < 0.5f ? outerLaneY : -outerLaneY;
        pendingRemaining = warningSeconds;
        if (warningPrefab != null)
        {
            pendingMarker = Instantiate(warningPrefab, new Vector3(SpawnX, pendingY, 0f), Quaternion.identity);
            pendingMarker.Spawn(true);
        }
        float gap = Mathf.Max(minGap, baseGap - (stations.StageNumber - 1) * gapPerStage);
        nextWarningDistance = cart.Distance + gap + Random.Range(0f, randomGap);
    }

    private void SpawnPending()
    {
        Obstacle item = Instantiate(pendingPrefab, new Vector3(SpawnX, pendingY, 0f), Quaternion.identity);
        item.GetComponent<NetworkObject>().Spawn(true);
        active.Add(item);
        ClearWarning();
    }

    private void ClearWarning()
    {
        if (pendingMarker != null && pendingMarker.IsSpawned) pendingMarker.Despawn(true);
        pendingMarker = null;
        pendingPrefab = null;
    }

    // 마차 앞면에 아직 닿지 않은 장애물이 있는지. 웹판은 마차 끝까지 보지만 Unity 마차는 도로 기준으로 훨씬 길어
    // (옆을 지나가는 데만 약 70m) 그대로 쓰면 예고 간격이 두 배 가까이 벌어진다. 그래서 앞면 기준으로 본다.
    private bool ObstacleAhead()
    {
        float cartFront = cartBody.bounds.min.x;
        foreach (Obstacle obstacle in active)
            if (obstacle != null && obstacle.transform.position.x < cartFront) return true;
        return false;
    }

    // x에 있는 장애물이 spawnIn초 뒤부터 흘러와 마차 앞면 충돌 구간을 지나는 동안(앞뒤 여유 포함) landIn초 뒤 바위가 떨어지는지.
    private bool StrikeNear(float x, float halfWidth, float landIn, float spawnIn)
    {
        float speed = Mathf.Max(0.1f, cart.ScrollVelocity.x);
        float front = cartBody.bounds.min.x;
        float contact = spawnIn + Mathf.Max(0f, front - halfWidth - x) / speed;
        float clear = spawnIn + Mathf.Max(0f, front + halfWidth - x) / speed;
        return landIn >= contact - StrikeMargin && landIn <= clear + StrikeMargin;
    }

    // 투석기 질의(서버). landIn초 뒤 착탄이 예고·앞쪽 장애물의 마차 앞 통과와 겹치는지.
    public bool StrikeOverlapsObstacle(float landIn)
    {
        if (pendingPrefab != null && StrikeNear(SpawnX, pendingPrefab.SpriteSize.x * 0.5f, landIn, pendingRemaining)) return true;
        float front = cartBody.bounds.min.x;
        foreach (Obstacle obstacle in active)
            if (obstacle != null && obstacle.transform.position.x < front && StrikeNear(obstacle.transform.position.x, obstacle.Size.x * 0.5f, landIn, 0f)) return true;
        return false;
    }

    public bool HasThreatAhead => pendingPrefab != null || ObstacleAhead();

    // 투석기 질의(서버). 바위가 rockY에 떨어져도 바위와 예고·앞쪽 장애물을 모두 피할 마차 차선(CartMotion 정수 차선)이 남는지.
    public bool HasEscapeLane(float rockY, float rockClearance)
    {
        Bounds body = cartBody.bounds;
        float bodyOffset = body.center.y - cart.transform.position.y;
        float cartHalf = body.extents.y * collisionHeightFraction;
        int limit = Mathf.RoundToInt(cart.LateralLimit);
        for (int lane = -limit; lane <= limit; lane++)
        {
            float y = lane + bodyOffset;
            if (Mathf.Abs(y - rockY) < rockClearance) continue;
            if (pendingPrefab != null && Blocks(pendingY, pendingPrefab.SpriteSize.y, y, cartHalf)) continue;
            bool blocked = false;
            foreach (Obstacle obstacle in active)
                if (obstacle != null && obstacle.transform.position.x < body.min.x && Blocks(obstacle.transform.position.y, obstacle.Size.y, y, cartHalf)) { blocked = true; break; }
            if (!blocked) return true;
        }
        return false;
    }

    private static bool Blocks(float obstacleY, float height, float bodyY, float cartHalf) => Mathf.Abs(bodyY - obstacleY) <= cartHalf + height * 0.5f;

    private void Update()
    {
        if (!stations.IsServer || !CartStations.Running) return;
        if (pendingPrefab != null)
        {
            pendingRemaining -= Time.deltaTime;
            if (pendingRemaining <= 0f)
            {
                if (catapult != null && catapult.Airborne(out float landIn) && StrikeNear(SpawnX, pendingPrefab.SpriteSize.x * 0.5f, landIn, 0f))
                    pendingRemaining = StrikeMargin;
                else
                    SpawnPending();
            }
        }
        else if (prefabs.Length > 0 && cart.Distance >= nextWarningDistance && !ObstacleAhead())
            Warn();

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

using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// 도로 장애물(바위·통나무·사슴). 마차와 같은 30Hz 틱에서 이동·충돌·예고를 처리한다.
// 위·아래 바깥 차선 중 한 곳을 정해 4초 전에 "!"(ObstacleWarning)로 예고한 뒤, 마차 앞쪽(왼쪽) 멀리 같은 차선에 소환한다.
// 예고 간격은 주행 거리 기준(첫 42m, 이후 125~150m)이고, 앞에 지나가지 않은 장애물이 있으면 다음 예고를 미룬다.
// 소환된 장애물은 배경과 같은 속도로 흘러가며, 마차 앞 충돌 구간에서 종류별 감속과 피해를 준다.
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

    private float SpawnX => RoadX(-1100f);
    private float HitStartX => RoadX(287f);
    private float HitEndX => RoadX(344f);
    private float DespawnX => RoadX(1260f);

    private float RoadX(float x) => cartBody.bounds.min.x + (x - 363f) * stations.WorldPerWebUnit;

    private readonly List<Obstacle> active = new();
    private float nextWarningDistance;
    private Obstacle pendingPrefab;
    private float pendingY;
    private float pendingRemaining;
    private NetworkObject pendingMarker;

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
        float speed = cart.ScrollVelocity.x;
        if (speed <= 0f) return;
        foreach (var obstacle in active)
        {
            if (obstacle == null) continue;
            Vector2 pos = obstacle.transform.position;
            float enter = (HitStartX - pos.x) / speed;
            float exit = (HitEndX - pos.x) / speed;
            if (exit < 0 || enter > CartDrivingHazard.Lookahead) continue;
            hazards.Add(new CartDrivingHazard(pos.y, HitHalfWidth(obstacle), enter, exit));
        }
    }

    private void Start() => nextWarningDistance = cart.Distance + firstWarningDistance;

    // 다음 스테이지 시작 시 CartStations가 호출한다.
    public void ResetForStage()
    {
        foreach (Obstacle obstacle in active) if (obstacle != null) obstacle.GetComponent<NetworkObject>().Despawn(true);
        active.Clear();
        ClearWarning();
        nextWarningDistance = cart.Distance + firstWarningDistance;
    }

    private void Warn()
    {
        pendingPrefab = prefabs[Random.Range(0, prefabs.Length)];
        pendingY = Random.value < 0.5f ? cart.LateralLimit : -cart.LateralLimit;
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

    // 새 예고는 앞 장애물이 충돌 구간을 지나간 뒤에 낸다.
    private bool ObstacleAhead()
    {
        foreach (Obstacle obstacle in active)
            if (obstacle != null && obstacle.transform.position.x < HitEndX) return true;
        return false;
    }

    // x에 있는 장애물이 spawnIn초 뒤부터 흘러와 마차 앞면 충돌 구간을 지나는 동안(앞뒤 여유 포함) landIn초 뒤 바위가 떨어지는지.
    private bool StrikeNear(float x, float landIn, float spawnIn)
    {
        float speed = Mathf.Max(0.1f, cart.ScrollVelocity.x);
        float contact = spawnIn + Mathf.Max(0f, HitStartX - x) / speed;
        float clear = spawnIn + Mathf.Max(0f, HitEndX - x) / speed;
        return landIn >= contact - StrikeMargin && landIn <= clear + StrikeMargin;
    }

    // 투석기 질의(서버). landIn초 뒤 착탄이 예고·앞쪽 장애물의 마차 앞 통과와 겹치는지.
    public bool StrikeOverlapsObstacle(float landIn)
    {
        if (pendingPrefab != null && StrikeNear(SpawnX, landIn, pendingRemaining)) return true;
        foreach (Obstacle obstacle in active)
            if (obstacle != null && obstacle.transform.position.x < HitEndX
                && StrikeNear(obstacle.transform.position.x, landIn, 0f)) return true;
        return false;
    }

    public bool HasThreatAhead => pendingPrefab != null || ObstacleAhead();

    // 바위와 예고·앞쪽 장애물을 모두 피할 차선이 남는지 확인한다.
    public bool HasEscapeLane(float rockY, float rockClearance)
    {
        for (int lane = -1; lane <= 1; lane++)
        {
            float y = lane * cart.LateralLimit;
            if (Mathf.Abs(y - rockY) < rockClearance) continue;
            float margin = 6f * stations.WorldPerWebUnit;
            if (pendingPrefab != null && Mathf.Abs(y - pendingY) < HitHalfWidth(pendingPrefab) + margin) continue;
            bool blocked = false;
            foreach (Obstacle obstacle in active)
                if (obstacle != null && obstacle.transform.position.x < HitEndX
                    && Mathf.Abs(y - obstacle.transform.position.y) < HitHalfWidth(obstacle) + margin)
                { blocked = true; break; }
            if (!blocked) return true;
        }
        return false;
    }

    private float HitHalfWidth(Obstacle obstacle) => obstacle.HitHalfWidth * stations.WorldPerWebUnit;

    public void Advance(float dt)
    {
        if (!stations.IsServer || !CartStations.Running) return;
        Vector3 movement = cart.ScrollVelocity * dt;
        float hitStart = HitStartX;
        float hitEnd = HitEndX;
        float despawnX = DespawnX;
        float cartY = cart.transform.position.y;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Obstacle obstacle = active[i];
            if (obstacle == null) { active.RemoveAt(i); continue; }

            Vector3 previous = obstacle.transform.position;
            Vector3 next = previous + movement;
            obstacle.transform.position = next;

            if (previous.x < hitEnd && next.x >= hitStart && Mathf.Abs(next.y - cartY) < HitHalfWidth(obstacle))
            {
                cart.ApplyObstacleSlow(obstacle.SlowFactor, obstacle.SlowSeconds);
                stations.ReceiveCollision(obstacle.Damage);
                obstacle.GetComponent<NetworkObject>().Despawn(true);
                active.RemoveAt(i);
                if (stations.Failed) return;
            }
            else if (next.x >= despawnX)
            {
                obstacle.GetComponent<NetworkObject>().Despawn(true);
                active.RemoveAt(i);
            }
        }
        if (pendingPrefab != null)
        {
            pendingRemaining -= dt;
            if (pendingRemaining > .0001f) return;
            if (catapult != null && catapult.Airborne(out float landIn) && StrikeNear(SpawnX, landIn, 0f))
                pendingRemaining = StrikeMargin;
            else
                SpawnPending();
        }
        else if (prefabs.Length > 0 && cart.Distance >= nextWarningDistance && !ObstacleAhead())
            Warn();
    }
}

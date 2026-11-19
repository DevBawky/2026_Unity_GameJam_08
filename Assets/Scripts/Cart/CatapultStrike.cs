using System;
using UnityEngine;
using Unity.Netcode;
using Random = UnityEngine.Random;

public struct CatapultState : INetworkSerializeByMemcpy, IEquatable<CatapultState>
{
    public bool Active, Landed;
    public float Time, TargetDistance, Y, LandX;
    public bool Equals(CatapultState v) => Active==v.Active && Landed==v.Landed && Time==v.Time && TargetDistance==v.TargetDistance && Y==v.Y && LandX==v.LandX;
}

// 투석기 바위. 씬에 하나 두고 재사용한다(한 번에 한 발). 웹판 sim.mjs·scene.mjs의 투석기를 옮겼다.
// 서버: 스테이지 시작 firstDelay초 뒤 첫 발, 이후 간격마다 발사. 착탄 지점은 도로에 고정된 좌표로,
//       "기본 속도로 flightTime초 달렸을 때 마차 뒤끝"이 지나갈 곳이고 마차 중심에서 위나 아래로 비켜 있다.
//       가만있으면 맞고, 가속해서 지나가거나 반대쪽으로 조향하면 피한다.
// 모두: 그림자가 뒤에서 다가와 lockTime에 도로에 자리 잡고, 경고 링이 깜빡이다가
//       착탄 dropTime초 전에 바위가 화면 밖 위에서 떨어져 쾅 부딪힌다. 이후 충격파·자국이 남는다.
public sealed class CatapultStrike : NetworkBehaviour
{
    [SerializeField] private CartStations stations;
    [Tooltip("도로 장애물과 겹치지 않게 발사 시점·착탄 쪽을 정할 때 쓴다. 비어 있으면 무시")]
    [SerializeField] private ObstacleSpawner obstacles;

    [Header("Visual (프리팹 자식)")]
    [SerializeField] private SpriteRenderer shadow;
    [SerializeField] private SpriteRenderer rock;
    [SerializeField] private SpriteRenderer shockwave;
    [SerializeField] private Sprite approachShadow;
    [SerializeField] private Sprite dangerShadow;
    [SerializeField] private SpriteSequence impactSequence;
    private Sprite safeShadow;

    public bool TryGetWarning(out Vector3 point)
    {
        point = default;
        if (!IsSpawned || !CartStations.Running) return false;
        CatapultState strike = State.Value;
        if (!strike.Active || strike.Landed || !IsLocked(strike)) return false;
        point = new Vector3(GroundX(strike), strike.Y, 0f);
        return HitsCart(point.x, point.y);
    }

    [Header("Timing (초)")]
    [Tooltip("스테이지 시작 후 첫 발까지")]
    [SerializeField, Min(0f)] private float firstDelay = 6f;
    [Tooltip("1스테이지 발사 간격")]
    [SerializeField, Min(1f)] private float baseInterval = 13f;
    [Tooltip("스테이지가 하나 오를 때마다 줄어드는 간격")]
    [SerializeField, Min(0f)] private float intervalPerStage = 0.5f;
    [Tooltip("발사 간격 최솟값")]
    [SerializeField, Min(1f)] private float minInterval = 9f;
    [Tooltip("그림자가 다가와 착탄 지점이 고정되는 시간")]
    [SerializeField, Min(0.1f)] private float lockTime = 1.1f;
    [Tooltip("발사부터 착탄까지")]
    [SerializeField, Min(0.5f)] private float flightTime = 4.8f;
    [Tooltip("착탄 몇 초 전부터 바위가 화면 밖에서 떨어지기 시작하는지")]
    [SerializeField, Min(0.1f)] private float dropTime = 0.6f;
    [Tooltip("착탄 후 바위·충격파·자국이 남아 있는 시간")]
    [SerializeField, Min(0.1f)] private float lingerTime = 0.7f;

    [Header("Target")]
    [Tooltip("마차 중심에서 위·아래로 비키는 거리 (마차 반높이 비율, 최소~최대)")]
    [SerializeField] private Vector2 offsetRange = new(0.76f, 0.84f);
    private const float CatapultUnitsPerMeter = 4f;
    private Vector2 HitRadius => new Vector2(35f, 24f) * stations.WorldPerWebUnit;
    private float HullHalfHeight => 100f * stations.WorldPerWebUnit;
    private float HullLeft => stations.CartBody.bounds.min.x - 23f * stations.WorldPerWebUnit;
    private float HullRight => stations.CartBody.bounds.max.x + 21f * stations.WorldPerWebUnit;

    [Header("Hit")]
    [SerializeField, Min(0f)] private float hitDamage = 22f;
    [Tooltip("명중 시 현재 속도에 곱하는 값")]
    [SerializeField, Range(0.1f, 1f)] private float hitSpeedMultiplier = 0.78f;

    [Header("Look")]
    private const float WarningScale = 2f;
    [Tooltip("고정 전 그림자가 출발하는 뒤쪽 거리 (월드)")]
    [SerializeField, Min(0f)] private float approachDistance = 7f;
    [Tooltip("바위가 떨어지기 시작하는 높이 (월드). 화면 위쪽 밖이 되게 크게 둔다")]
    [SerializeField, Min(1f)] private float dropHeight = 12f;
    [Tooltip("충격파가 퍼지는 최대 크기 (그림자 크기 배율)")]
    [SerializeField, Min(1f)] private float shockwaveScale = 2.5f;
    [Tooltip("착탄 후 충격파가 퍼지는 구간 (남는 시간 대비 비율)")]
    [SerializeField, Range(0.1f, 1f)] private float shockwavePortion = 0.55f;

    public NetworkVariable<CatapultState> State = new();

    private int stage;
    private float stageTime;
    private float nextLaunch;
    private bool shown = true;
    private Color shadowColor;
    private Color rockColor;
    private Color shockwaveColor;
    // 스프라이트마다 원래 크기가 달라서(원 1, 링 2.5) 지름을 줄 때 이 값을 곱해 맞춘다.
    private float shockwaveUnit;

    private void Awake()
    {
        shadowColor = shadow.color;
        safeShadow = shadow.sprite;
        rockColor = rock.color;
        shockwaveColor = shockwave.color;
        shockwaveUnit = Unit(shockwave);
        SetVisible(false);
    }

    private static float Unit(SpriteRenderer renderer) => renderer.sprite != null ? 1f / renderer.sprite.bounds.size.x : 1f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer || stations == null) return;
        stations.CollectDrivingHazards += CollectDrivingHazards;
        CartStations.SimulationStep += Simulate;
    }

    public override void OnNetworkDespawn()
    {
        if (stations != null) stations.CollectDrivingHazards -= CollectDrivingHazards;
        CartStations.SimulationStep -= Simulate;
    }

    private void CollectDrivingHazards(System.Collections.Generic.List<CartDrivingHazard> hazards)
    {
        var strike = State.Value;
        if (!strike.Active || strike.Landed) return;
        float remaining = Mathf.Max(0, flightTime - strike.Time);
        var cart = stations.Cart;
        float x = GroundX(strike) + cart.Speed * CatapultUnitsPerMeter * stations.WorldPerWebUnit * remaining;
        float dx = Mathf.Abs(x - Mathf.Clamp(x, HullLeft, HullRight));
        var hitRadius = HitRadius;
        if (dx > hitRadius.x) return;
        float normalizedX = dx / Mathf.Max(.001f, hitRadius.x);
        float radius = HullHalfHeight + hitRadius.y * Mathf.Sqrt(1f - normalizedX * normalizedX);
        hazards.Add(new CartDrivingHazard(strike.Y, radius, remaining, remaining));
    }

    private void Update()
    {
        if (!IsSpawned || stations == null || !stations.IsSpawned) return;
        Draw(State.Value);
    }

    private void Simulate(float dt)
    {
        if (!CartStations.Running) return;
        if (stations.StageNumber != stage)
        {
            stage = stations.StageNumber;
            stageTime = 0f;
            nextLaunch = firstDelay;
            State.Value = default;
        }
        stageTime += dt;

        CatapultState s = State.Value;
        if (!s.Active)
        {
            if (stageTime + .0001f >= nextLaunch) Launch();
            return;
        }
        s.Time += dt;
        if (!s.Landed && s.Time + .0001f >= flightTime) Land(ref s);
        if (s.Time + .0001f >= flightTime + lingerTime) s = default;
        State.Value = s;
    }

    // 착탄까지 남은 시간. 날아가는 중이 아니면 false (ObstacleSpawner가 소환을 미룰지 볼 때 쓴다).
    public bool Airborne(out float landIn)
    {
        CatapultState s = State.Value;
        landIn = flightTime - s.Time;
        return s.Active && !s.Landed;
    }

    // 장애물과 겹치면 발사하지 않고 돌아간다. Simulate가 다음 프레임에 다시 시도한다(웹판 advanceCatapult).
    private void Launch()
    {
        // 착탄이 장애물의 마차 앞 통과와 겹치면 미룬다.
        if (obstacles != null && obstacles.StrikeOverlapsObstacle(flightTime)) return;
        CartMotion cart = stations.Cart;
        float halfHeight = HullHalfHeight;
        float offset = Random.Range(offsetRange.x, offsetRange.y) * halfHeight;
        float cartY = cart.transform.position.y;
        var hitRadius = HitRadius;
        // 가장자리에 붙어 있으면 바위를 그 가장자리 쪽에 둬서 길 안쪽으로 피할 수 있게 한다.
        float margin = 8f * stations.WorldPerWebUnit;
        float clearance = halfHeight + hitRadius.y - offset + margin;
        float side = Random.value < 0.5f ? 1f : -1f;
        if (cart.LateralLimit - cartY < clearance) side = 1f;
        else if (cartY + cart.LateralLimit < clearance) side = -1f;
        // 앞에 장애물이 있으면 바위와 장애물을 함께 피할 차선을 남긴다. 안 되면 반대쪽, 둘 다 안 되면 미룬다.
        if (obstacles != null && obstacles.HasThreatAhead)
        {
            float rockClearance = halfHeight + hitRadius.y + margin;
            if (!obstacles.HasEscapeLane(cartY + side * offset, rockClearance))
            {
                if (!obstacles.HasEscapeLane(cartY - side * offset, rockClearance)) return;
                side = -side;
            }
        }

        State.Value = new CatapultState
        {
            Active = true,
            TargetDistance = cart.Distance + cart.BaseSpeed * flightTime,
            Y = cartY + side * offset,
        };
        nextLaunch = stageTime + Mathf.Max(minInterval, baseInterval - (stage - 1) * intervalPerStage);
        stations.PlayCartCueRpc(GameSfx.StoneFlight);
    }

    private void Land(ref CatapultState s)
    {
        s.Landed = true;
        s.LandX = GroundX(s);
        if (!HitsCart(s.LandX, s.Y))
        {
            stations.PlayCartCueRpc(GameSfx.ImpactDirt);
            return;
        }
        stations.Cart.ApplySpeedHit(hitSpeedMultiplier);
        stations.ReceiveCatapultHit(hitDamage, new Vector2(s.LandX, s.Y));
    }

    private bool HitsCart(float x, float y)
    {
        float yCenter = stations.Cart.transform.position.y;
        var hitRadius = HitRadius;
        float dx = (x - Mathf.Clamp(x, HullLeft, HullRight)) / hitRadius.x;
        float dy = (y - Mathf.Clamp(y, yCenter - HullHalfHeight, yCenter + HullHalfHeight)) / hitRadius.y;
        return dx * dx + dy * dy <= 1f;
    }

    private float GroundX(CatapultState s) => HullRight + 3f * stations.WorldPerWebUnit
        + (stations.Cart.Distance - s.TargetDistance) * CatapultUnitsPerMeter * stations.WorldPerWebUnit;

    private bool IsLocked(CatapultState strike) => strike.Time + .0001f >= lockTime;

    private void Draw(CatapultState s)
    {
        SetVisible(s.Active);
        if (!s.Active) return;

        var hitRadius = HitRadius;
        var marker = new Vector3(hitRadius.x * 2f * WarningScale, hitRadius.y * 2f * WarningScale, 1f);
        if (s.Landed)
        {
            DrawImpact(s, marker);
            return;
        }

        float approach = Mathf.Clamp01(s.Time / lockTime);
        bool locked = IsLocked(s);
        // 고정 전에는 뒤(오른쪽)에서 다가오며 커진다.
        var ground = new Vector3(GroundX(s) + approachDistance * Mathf.Pow(1f - approach, 2f), s.Y, 0f);

        shadow.enabled = true;
        shadow.transform.position = ground;
        bool danger = locked && HitsCart(ground.x, ground.y);
        shadow.sprite = !locked && approachShadow != null ? approachShadow : danger && dangerShadow != null ? dangerShadow : safeShadow;
        shadow.color = locked ? Color.white : WithAlpha(shadowColor, Mathf.Lerp(.28f, .52f, approach));
        marker.x *= Mathf.Lerp(22f / 35f, 1f, approach);
        marker.y *= Mathf.Lerp(13f / 24f, 1f, approach);
        SetShadowScale(marker, 1f);

        // 착탄 dropTime초 전부터 화면 밖 위에서 가속하며 떨어진다.
        float fall = 1f - (flightTime - s.Time) / dropTime;
        rock.enabled = fall > 0f;
        if (fall > 0f)
        {
            rock.transform.position = ground + Vector3.up * (dropHeight * (1f - fall * fall));
            rock.color = WithAlpha(rockColor, Mathf.Min(1f, fall * 4f));
        }
        shockwave.enabled = false;
    }

    private void DrawImpact(CatapultState s, Vector3 marker)
    {
        float age = Mathf.Clamp01((s.Time - flightTime) / lingerTime);
        float fade = Mathf.Min(1f, (1f - age) * 3f);
        var ground = new Vector3(s.LandX, s.Y, 0f);
        if (impactSequence != null)
        {
            shadow.enabled = rock.enabled = false;
            float seconds = s.Time - flightTime;
            shockwave.enabled = impactSequence.PlayingAt(seconds);
            if (shockwave.enabled)
            {
                shockwave.transform.position = ground;
                shockwave.sprite = impactSequence.FrameAt(seconds);
                shockwave.color = WithAlpha(shockwaveColor, fade);
            }
            return;
        }

        // 바닥 자국 + 바위
        shadow.enabled = true;
        shadow.transform.position = ground;
        SetShadowScale(marker, 1f);
        shadow.color = WithAlpha(shadowColor, 0.6f * fade);
        rock.enabled = true;
        rock.transform.position = ground;
        rock.color = WithAlpha(rockColor, fade);

        // 충격파
        float wave = age / shockwavePortion;
        shockwave.enabled = wave < 1f;
        if (wave >= 1f) return;
        shockwave.transform.position = ground;
        shockwave.transform.localScale = marker * (Mathf.Lerp(1f, shockwaveScale, wave) * shockwaveUnit);
        shockwave.color = WithAlpha(shockwaveColor, shockwaveColor.a * (1f - wave));
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void SetShadowScale(Vector3 diameter, float growth)
    {
        Vector3 size = shadow.sprite.bounds.size;
        shadow.transform.localScale = new Vector3(diameter.x * growth / size.x, diameter.y * growth / size.y, 1f);
    }

    private void SetVisible(bool visible)
    {
        if (shown == visible) return;
        shown = visible;
        shadow.gameObject.SetActive(visible);
        rock.gameObject.SetActive(visible);
        shockwave.gameObject.SetActive(visible);
    }
}

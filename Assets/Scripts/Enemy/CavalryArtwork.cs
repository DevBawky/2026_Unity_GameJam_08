using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public struct CavalryPose : INetworkSerializeByMemcpy, IEquatable<CavalryPose>
{
    public double StrikeAt, DeathAt;
    public Vector2 Target, HitPoint;
    public int DeathKind;
    public readonly bool Equals(CavalryPose other) => StrikeAt == other.StrikeAt && DeathAt == other.DeathAt
        && Target == other.Target && HitPoint == other.HitPoint && DeathKind == other.DeathKind;
}

// 기병 그림 연출: 달리기·피격·폭발 프레임, 말발굽 그림자, 창 찌르기 동작과 깊이 정렬.
public sealed class CavalryArtwork : MonoBehaviour
{
    internal const float RiderOffset = 40f;
    [SerializeField] private SpriteRenderer rider, shadow, spear;
    [SerializeField] private TMPro.TMP_Text startledLabel;
    [SerializeField] private Transform visualRoot, grip;
    [SerializeField] private SortingGroup group;
    [SerializeField] private Sprite[] running, hit, blast;
    [SerializeField] private SpriteRenderer hitBurst;
    [SerializeField] private SpriteSequence hitBurstFrames;

    private static readonly Vector3[] Hooves =
    {
        new(62,136,172), new(49,131,175), new(41,156,175), new(41,156,175),
        new(35,155,178), new(37,174,177), new(64,154,177), new(64,154,177)
    };
    private Enemy enemy;
    private float shadowAlpha;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        shadowAlpha = shadow.color.a;
    }

    private void LateUpdate()
    {
        if (!enemy.IsSpawned || CartStations.Instance == null) return;
        float unit = CartStations.Instance.WorldPerWebUnit;
        var pose = enemy.VisualPose.Value;
        double time = enemy.NetworkManager.ServerTime.Time;
        bool dead = pose.DeathKind != 0;
        float age = (float)((time - pose.StrikeAt) * 30d);
        int runFrame = ((int)enemy.NetworkObjectId % 8 + Mathf.Max(0, Mathf.FloorToInt((float)time * 16f))) % 8;
        int hoof = dead ? 4 : enemy.Knocked ? 0 : runFrame;
        var ground = transform.position;
        Vector3 origin = ground + Vector3.right * (RiderOffset * unit);
        visualRoot.position = origin;
        float startled = enemy.StartledRemaining;
        float lean = SettingsPanel.ReducedMotion ? 0f : 8f * Mathf.Clamp01(1f - (2f - startled) * 30f / 8f);
        visualRoot.rotation = transform.rotation * Quaternion.Euler(0f, 0f, -lean);
        if (startledLabel != null)
        {
            startledLabel.enabled = !dead && !enemy.Knocked && startled > 0f;
            startledLabel.transform.position = origin + Vector3.up * (65f * unit);
        }

        if (dead)
        {
            float deathAge = (float)(time - pose.DeathAt);
            int frame = Mathf.Clamp(Mathf.FloorToInt(deathAge * 15f), 0, 7);
            rider.sprite = (pose.DeathKind == 2 ? blast : hit)[frame];
            float alpha = Mathf.Clamp01((1f - deathAge) * 30f / 8f);
            SetAlpha(rider, alpha);
            SetAlpha(shadow, alpha * shadowAlpha);
            if (hitBurst != null && hitBurstFrames != null)
            {
                hitBurst.enabled = hitBurstFrames.PlayingAt(deathAge);
                if (hitBurst.enabled)
                {
                    hitBurst.sprite = hitBurstFrames.FrameAt(deathAge);
                    hitBurst.transform.position = (Vector3)pose.HitPoint + Vector3.forward * ground.z;
                    Vector3 parent = hitBurst.transform.parent.lossyScale;
                    hitBurst.transform.localScale = new Vector3(unit / parent.x, unit / parent.y, 1f);
                }
            }
        }
        else rider.sprite = running[hoof];

        shadow.transform.rotation = Quaternion.identity;
        HoofShadow.Place(shadow, rider, Hooves[hoof], 200f, 184f, unit, 6.75f);
        spear.enabled = !dead && !enemy.Knocked && enemy.Unit?.type == UnitType.Lancer;
        Vector3 hand = origin + new Vector3(-15f, 4.5f, 0f) * unit;
        float handAngle = 0f;
        if (spear.enabled && pose.StrikeAt >= 0d && age >= 0f && age < 12f)
        {
            float retract = Mathf.SmoothStep(0f, 1f, (age - 3f) / 9f);
            Vector2 delta = pose.Target - (Vector2)hand;
            Vector2 direction = delta.normalized;
            Vector2 thrust = direction * Mathf.Max(0f, delta.magnitude - 91.875f * unit) * (1f - retract);
            Vector2 body = new(thrust.x * .845f, thrust.y * .65f);
            visualRoot.position += (Vector3)body;
            hand += (Vector3)thrust;
            ground.y += body.y;
            handAngle = Vector2.SignedAngle(Vector2.left, direction) * (1f - retract);
        }
        grip.position = hand;
        grip.rotation = Quaternion.Euler(0f, 0f, handAngle);
        group.sortingOrder = WorldDepth.Order(ground, false);
    }

    private static void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }
}

using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public struct CavalryPose : INetworkSerializeByMemcpy, IEquatable<CavalryPose>
{
    public double StrikeAt, DeathAt;
    public Vector2 Target;
    public int DeathKind;
    public readonly bool Equals(CavalryPose other) => StrikeAt == other.StrikeAt && DeathAt == other.DeathAt
        && Target == other.Target && DeathKind == other.DeathKind;
}

public sealed class CavalryArtwork : MonoBehaviour
{
    internal const float RiderOffset = 40f;
    [SerializeField] private SpriteRenderer rider, shadow, spear;
    [SerializeField] private Transform visualRoot, grip;
    [SerializeField] private SortingGroup group;
    [SerializeField] private Sprite[] running, hit, blast;

    private static readonly Vector3[] Hooves =
    {
        new(62,136,172), new(49,131,175), new(41,156,175), new(41,156,175),
        new(35,155,178), new(37,174,177), new(64,154,177), new(64,154,177)
    };
    private Enemy enemy;
    private void Awake() => enemy = GetComponent<Enemy>();

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
        visualRoot.rotation = transform.rotation;

        if (dead)
        {
            int frame = Mathf.Clamp(Mathf.FloorToInt((float)((time - pose.DeathAt) * 15d)), 0, 7);
            rider.sprite = (pose.DeathKind == 2 ? blast : hit)[frame];
        }
        else rider.sprite = running[hoof];

        shadow.transform.rotation = Quaternion.identity;
        HoofShadow.Place(shadow, rider, Hooves[hoof], 200f, 184f, unit, 4.5f);
        spear.enabled = !dead && !enemy.Knocked && enemy.Unit?.type == UnitType.Lancer;
        Vector3 hand = origin + new Vector3(-10f, 3f, 0f) * unit;
        float handAngle = 0f;
        if (spear.enabled && pose.StrikeAt >= 0d && age >= 0f && age < 12f)
        {
            float retract = Mathf.SmoothStep(0f, 1f, (age - 3f) / 9f);
            Vector2 delta = pose.Target - (Vector2)hand;
            Vector2 direction = delta.normalized;
            Vector2 thrust = direction * Mathf.Max(0f, delta.magnitude - 61.25f * unit) * (1f - retract);
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
}

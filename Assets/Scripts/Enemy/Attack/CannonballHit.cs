using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Projectile))]
public sealed class CannonballHit : NetworkBehaviour
{
    [Tooltip("몬스터에게 주는 피해. 몬스터 HP 이상이면 한 방에 처치")]
    [Min(0)] public float damage = 1f;

    private readonly HashSet<Enemy> victims = new();
    private readonly HashSet<Enemy> nearMisses = new();
    private const float BodyScale = 1.58f * .75f;
    private const float NearMissMargin = 30f;
    private Projectile projectile;

    private void Awake() => projectile = GetComponent<Projectile>();

    public int[] HitIndices(Enemy[] enemies) => Indices(victims, enemies);
    public int[] NearMissIndices(Enemy[] enemies) => Indices(nearMisses, enemies);

    private static int[] Indices(HashSet<Enemy> contacts, Enemy[] enemies)
    {
        var indices = new List<int>();
        for (int i = 0; i < enemies.Length; i++)
            if (contacts.Contains(enemies[i])) indices.Add(i);
        return indices.ToArray();
    }

    public void RestoreContacts(int[] hitIndices, int[] nearMissIndices, Enemy[] enemies)
    {
        Restore(victims, hitIndices, enemies);
        Restore(nearMisses, nearMissIndices, enemies);
    }

    private static void Restore(HashSet<Enemy> contacts, int[] indices, Enemy[] enemies)
    {
        if (indices == null) return;
        foreach (int i in indices)
            if (i >= 0 && i < enemies.Length && enemies[i] != null) contacts.Add(enemies[i]);
    }

    private static Vector2 BodyCenter(Enemy enemy, float unit) => (Vector2)enemy.transform.position
        + new Vector2(CavalryArtwork.RiderOffset - 9f * BodyScale, -3f * BodyScale) * unit;

    public void Sweep(Vector2 step, Vector2 velocity)
    {
        float unit = CartStations.Instance.WorldPerWebUnit;
        Vector2 origin = transform.position;
        float expansion = projectile.CaliberScale - 1f;
        float radius = Projectile.BaseRadius * projectile.CaliberScale;
        Vector2 radii = new Vector2(43f * BodyScale + radius + 22f * expansion,
            23f * BodyScale + radius + 30f * expansion) * unit;
        Vector2 nearRadii = radii + Vector2.one * (NearMissMargin * unit);
        for (int i = Enemy.Active.Count - 1; i >= 0; i--)
        {
            var enemy = Enemy.Active[i];
            if (enemy.Knocked || !enemy.IsCavalry || victims.Contains(enemy)) continue;
            Vector2 offset = origin - BodyCenter(enemy, unit);
            float entry = CartStations.EllipseEntry(offset, step, radii);
            if (entry <= 1f)
            {
                Vector2 impulse = new(116f * unit, Mathf.Clamp(velocity.y * .18f, -70f * unit, 70f * unit));
                Damage(enemy, false, impulse, origin + step * entry);
            }
            else if (CartStations.EllipseEntry(offset, step, nearRadii) <= 1f)
                nearMisses.Add(enemy);
        }
    }

    public void Blast(Vector2 origin, float length, float halfWidth)
    {
        float unit = CartStations.Instance.WorldPerWebUnit;
        for (int i = Enemy.Active.Count - 1; i >= 0; i--)
        {
            var enemy = Enemy.Active[i];
            if (enemy.Knocked || !enemy.IsCavalry || victims.Contains(enemy)) continue;
            Vector2 bodyCenter = BodyCenter(enemy, unit);
            Vector2 delta = bodyCenter - origin;
            float forward = Mathf.Clamp(delta.x, 0f, length);
            float t = forward / length;
            float width = halfWidth * Mathf.Min(1f, (.55f + .95f * Mathf.Sin(Mathf.PI * t)) * Mathf.Sqrt(1f - t));
            float outside = Mathf.Max(0f, Mathf.Abs(delta.y) - width);
            if (forward != delta.x || outside > 0f)
            {
                if (nearMisses.Contains(enemy) || new Vector2(delta.x - forward, outside).magnitude <= NearMissMargin * unit)
                    enemy.Startle();
                continue;
            }

            float side = delta.y > 0f ? 1f : -1f;
            Vector2 impulse = new Vector2(205f - 64f * t, side * (72f + Mathf.Abs(delta.y) / unit * .9f)) * unit;
            if (!Damage(enemy, true, impulse, bodyCenter) && enemy.KnockAway(impulse))
                projectile.AwardElimination();
        }
    }

    private bool Damage(Enemy enemy, bool blast, Vector2 impulse, Vector2 point)
    {
        victims.Add(enemy);
        if (!enemy.TakeDamage(damage, blast, impulse, point)) return false;
        projectile.AwardElimination();
        return true;
    }
}

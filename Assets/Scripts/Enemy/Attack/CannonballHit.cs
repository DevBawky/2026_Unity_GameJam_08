using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// One impact per enemy across all swept and trigger contacts.
// 이동은 기존 Projectile이 담당하고, 여기서는 충돌만 처리한다.
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CannonballHit : NetworkBehaviour
{
    [Tooltip("몬스터에게 주는 피해. 몬스터 HP 이상이면 한 방에 처치")]
    [Min(0)] public float damage = 1f;

    private readonly HashSet<Enemy> victims = new();
    private readonly List<RaycastHit2D> hits = new();
    private Collider2D shape;
    private void Awake() => shape = GetComponent<Collider2D>();

    public int[] HitIndices(Enemy[] enemies)
    {
        var indices = new List<int>();
        for (int i = 0; i < enemies.Length; i++) if (victims.Contains(enemies[i])) indices.Add(i);
        return indices.ToArray();
    }
    public void RestoreHits(int[] indices,Enemy[] enemies)
    {
        if (indices==null) return;
        foreach(int i in indices) if(i>=0 && i<enemies.Length && enemies[i]!=null) victims.Add(enemies[i]);
    }
    public void Sweep(Vector2 step)
    {
        if (step.sqrMagnitude == 0f) return;
        var filter = new ContactFilter2D { useTriggers = true };
        shape.Cast(step.normalized, filter, hits, step.magnitude);
        foreach (var hit in hits) OnTriggerEnter2D(hit.collider);
    }

    public void Blast(Vector2 origin, float length, float halfWidth)
    {
        for (int i = Enemy.Active.Count - 1; i >= 0; i--)
        {
            var enemy = Enemy.Active[i];
            Vector2 delta = (Vector2)enemy.transform.position - origin;
            if (delta.x < 0 || delta.x > length) continue;
            float t = delta.x / length;
            float width = halfWidth * Mathf.Min(1f, (.55f + .95f * Mathf.Sin(Mathf.PI * t)) * Mathf.Sqrt(1f-t));
            if (Mathf.Abs(delta.y) <= width && victims.Add(enemy))
                enemy.KnockAway(new Vector2(205f-64f*t, (delta.y>=0 ? 1 : -1)*(72+Mathf.Abs(delta.y)/CartStations.Instance.WorldPerWebUnit*.9f))*CartStations.Instance.WorldPerWebUnit);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsSpawned || !IsServer || !CartStations.Running) return;
        if (!other.TryGetComponent(out Enemy enemy) || !victims.Add(enemy)) return;
        enemy.TakeDamage(damage);

    }
}

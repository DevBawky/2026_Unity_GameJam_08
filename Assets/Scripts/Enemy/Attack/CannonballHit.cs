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

    public int[] HitIndices(Enemy[] enemies) => System.Array.FindAll(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Range(0,enemies.Length)),i=>victims.Contains(enemies[i]));
    public void RestoreHits(int[] indices,Enemy[] enemies)
    {
        foreach(int i in indices) if(i>=0 && i<enemies.Length && enemies[i]!=null) victims.Add(enemies[i]);
    }
    public void Sweep(Vector2 step)
    {
        if (step.sqrMagnitude == 0f) return;
        var filter = new ContactFilter2D { useTriggers = true };
        shape.Cast(step.normalized, filter, hits, step.magnitude);
        foreach (var hit in hits) OnTriggerEnter2D(hit.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsSpawned || !IsServer || !CartStations.Running) return;
        if (!other.TryGetComponent(out Enemy enemy) || !victims.Add(enemy)) return;
        enemy.TakeDamage(damage);

    }
}

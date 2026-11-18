using UnityEngine;

public sealed class Projectile : MonoBehaviour
{
    private Vector3 velocity;
    public void Launch(Vector3 direction, float speed) { velocity = direction.normalized * speed; Destroy(gameObject, 5f); }
    private void Update() => transform.position += velocity * Time.deltaTime;
}

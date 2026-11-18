using UnityEngine;

// 궁수 화살. 쏜 순간의 조준 지점(마차 가장자리)까지 직선으로 날아가 마차 피해를 낸다.
public class Arrow : MonoBehaviour
{
    private Vector3 target;
    private float speed;
    private float damage;

    public void Launch(Vector3 to, float flySpeed, float dmg)
    {
        target = to;
        speed = flySpeed;
        damage = dmg;
        transform.right = to - transform.position;
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (transform.position != target) return;
        Enemy.HitCart(damage);
        Destroy(gameObject);
    }
}

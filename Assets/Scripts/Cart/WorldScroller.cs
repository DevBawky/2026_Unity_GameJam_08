using UnityEngine;

// 월드 루트 1개에 붙인다. 마차가 왼쪽으로 달리므로 월드는 오른쪽(+x)으로 흐른다.
// 배경·장애물은 이 오브젝트의 자식으로 두면 같이 흐른다.
public class WorldScroller : MonoBehaviour
{
    [SerializeField] CartMotion cart; // 비우면 씬에서 찾는다(프리팹끼리 합칠 때 연결 생략용)

    void Awake()
    {
        if (cart == null) cart = FindAnyObjectByType<CartMotion>();
    }

    void Update() => transform.position += Vector3.right * (cart.Speed * Time.deltaTime);
}

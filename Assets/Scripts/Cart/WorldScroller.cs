using UnityEngine;

// 월드 루트(길·배경)에 붙는다. 마차 진행에 맞춰 좌우로만 흐른다. 위아래 이동은 마차(CartMotion)가 직접 한다.
public sealed class WorldScroller : MonoBehaviour
{
    [SerializeField] private CartMotion cart;

    private Vector3 origin;
    private void Awake() => origin=transform.position;
    private void Update()
    {
        if (cart==null || !cart.IsSpawned) return;
        transform.position=origin+Vector3.right*cart.ScrollDistance;
    }
}

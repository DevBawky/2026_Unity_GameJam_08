using UnityEngine;

// 상점에 나올 수 있는 상품 전체 목록. 진열 번호는 이 배열의 인덱스다(네트워크로 인덱스만 보낸다).
[CreateAssetMenu(menuName = "GameJam/Shop Catalog")]
public sealed class ShopCatalog : ScriptableObject
{
    public ShopItem[] items;

    public ShopItem Get(int index) => index >= 0 && index < items.Length ? items[index] : null;
}

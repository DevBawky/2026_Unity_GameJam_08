using UnityEngine;

// 상점 상품 하나. 영구 업그레이드는 한 판에 한 번만 살 수 있고, 일회성 상품은 다음 스테이지에 한 번 쓰인다.
// 효과는 ShopPerk(= 카탈로그 인덱스)로 구분한다. 영구 효과는 각 시스템이 CartStations.Owns()로 확인하고,
// 일회성 효과는 CartStations.ApplyStageItems()가 다음 스테이지 시작 때 적용한다. 수치는 CartShopItems의 상수에 있다.
[CreateAssetMenu(menuName = "GameJam/Shop Item")]
public sealed class ShopItem : ScriptableObject
{
    public enum Category { Driving, Cleaning, Aiming, Supply, Repair, Care, OneTime }

    [Tooltip("상점에 보이는 이름 (한국어 원문, 번역표 키)")]
    public string displayName;
    [Tooltip("선택했을 때 보이는 설명 (한국어 원문, 번역표 키)")]
    [TextArea] public string description;
    [Tooltip("이름 위에 보이는 아이콘 (비워두면 회색 자리표시)")]
    public Sprite icon;
    [Tooltip("구매 가격 (공동 AP)")]
    [Min(0)] public int price = 5;
    [Tooltip("일회성 상품: 다음 스테이지에 한 번 쓰이고, 다음 상점에 다시 나올 수 있다")]
    public bool consumable;
    public Category category;
}

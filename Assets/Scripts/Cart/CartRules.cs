using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Cart Rules")]
public sealed class CartRules : ScriptableObject
{
    [Serializable]
    public sealed class Card
    {
        public string title;
        [Min(0)] public int price;
        [Min(0)] public float amount;
        [Min(1)] public int maxLevel = 3;
        public Card(string title, int price, float amount)
        {
            this.title = title;
            this.price = price;
            this.amount = amount;
        }
    }

    // Order matches the shop buttons and the vote indices in network checkpoints.
    public Card repair = new("수리", 3, 35);
    public Card precision = new("정밀 도구", 4, 0.027f);
    public Card stabilizer = new("대포 고정대", 4, 1);
    public Card barrel = new("대구경 포신", 6, 0.25f);
    public Card hull = new("강화 차체", 6, 20);
    public Card porter = new("운반 장구", 4, 0.2f);
    public Card cushion = new("완충 차축", 5, 0.78f);
    public Card horses = new("길들인 군마", 5, 3);

    [Header("작업대 소요 시간 (초)")]
    [Min(0.01f)] public float cookSeconds = 3;
    [Min(0.01f)] public float fuseSeconds = 2.5f;
    [Min(0.01f)] public float shellHoldSeconds = 1.6f;

    [Header("개인 점수")]
    [Min(0)] public int horseBoostScore = 1;
    [Min(0)] public int cannonCleanScore = 1;
    [Min(0)] public int shellReloadScore = 2;
    [Min(0)] public int enemyEliminationScore = 4;
    [Min(0)] public int shellDeliveryScore = 8;
    [Min(0)] public int childReseatScore = 20;
    [Min(0)] public int wheelInstallScore = 8;
    [Min(0)] public int wheelRepairScore = 12;

    private static CartRules active;
    public static CartRules Active
    {
        get
        {
            if (active == null) active = Resources.Load<CartRules>("CartRules");
            return active;
        }
    }
    public Card GetCard(int slot) => slot switch
    {
        0 => repair, 1 => precision, 2 => stabilizer, 3 => barrel,
        4 => hull, 5 => porter, 6 => cushion, 7 => horses,
        _ => throw new ArgumentOutOfRangeException(nameof(slot))
    };
}

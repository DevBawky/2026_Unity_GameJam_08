using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

// 상점 진열·구매(서버)와 상품 효과의 공통 상태.
// 정비 단계가 시작되면 상품 6개를 겹치지 않게 뽑고 공동 AP로 산다. 영구 상품은 PurchasedItems에 쌓여 Owns()로 확인하고,
// 일회성 상품은 다음 스테이지 시작 때 한 번 적용된다(긴급 수리는 사는 즉시).
// 추후 변경점: 방장 승계 체크포인트(RunCheckpoint)에는 진열·구매 기록이 아직 들어가지 않는다.
public sealed partial class CartStations
{
    public const int ShopSlots = 6;
    public const int SoldOut = -1;

    // 효과 수치. 상품 설명에 맞춘 기본값이며, 밸런스는 여기서 조정한다.
    internal const float LightWheelsSpeedBonus = 1.2f, LightWheelsLimitRate = 1.08f;
    internal const float ImprovedWhipStrength = 1.4f, SteeringReinsResponse = 1.6f, OatFeedRageRate = .75f;
    internal const float TelescopeWarningRate = 1.5f;
    internal const float BrakeSlowFactor = .55f, BrakeSeconds = 1.2f, BrakeCooldown = 8f;
    internal const int BrushTapGain = 2, RelayTapBonus = 3;
    internal const float LongBrushReach = 1.35f, TimingBonusPerTool = 25f, NearMissDegrees = 18f;
    internal const float CallBellCooldown = 6f;
    internal const float GreaseMotionRate = .5f, FineAimRate = .45f, FineAimStick = .5f;
    internal const float ScopeSteadySeconds = .8f, ScopeSpreadRate = .7f;
    internal const float LongFuseRate = 1.4f;
    internal const int ReleaseLeverTicks = 30;
    internal const float RecoilMountRate = .5f;
    internal const float DumbbellPorterBonus = .25f, CarrySlingThreshold = .55f, WideRackScale = 1.4f;
    internal const float StairShelfRadius = 70f;
    internal const float LightSpareWheelRate = 150f / 165f, SupportStrutSpeed = .8f;
    internal const float HullBonus = 25f, CrashPadRate = .5f;
    internal const float CushionFallRate = .5f, SwaddleRate = .6f, FixedPotRate = .5f;
    internal const int PictureBookTicks = 360, HeartyRecipeHunger = 85;
    internal const float CarrotStartRate = .7f;
    internal const int SupplyCrateShells = 2;
    internal const float CheerSeconds = 20f, CheerSpeedRate = 1.25f;

    [SerializeField] private ShopCatalog shopCatalog;

    // 진열된 상품의 카탈로그 인덱스. 팔리면 SoldOut.
    public NetworkList<int> ShopOffer = new();
    // 산 영구 상품의 카탈로그 인덱스(산 순서대로).
    public NetworkList<int> PurchasedItems = new();
    // 칸마다 구매자 아닌 사람이 누른 하트 수. 새로 진열할 때 0으로 돌아간다.
    public NetworkList<int> ShopHearts = new();
    // 계단 전달 선반에 올려 둔 포탄 수(0/1), 비상 바퀴 꾸러미 대기 여부, 격려 깃발이 끝나는 서버 시간.
    public NetworkVariable<int> ShelfShells = new();
    public NetworkVariable<bool> SpareKitReady = new();
    public NetworkVariable<double> CheerUntil = new();

    private readonly List<int> pendingItems = new();
    private ulong ownedMask;
    private int ownedCount = -1;
    private bool rattleReady, coverReady, strapReady, reloadRetryUsed, relayUsed;
    private NetworkPlayer lastCleaner;
    private double brakeReadyAt, bellReadyAt;

    public ShopCatalog Catalog => shopCatalog;

    // 추후 변경점: 기여도 1등만 살 수 있게 바뀔 예정. 지금은 임시로 방장만 산다.
    // 방장이 아닌 사람이 사게 되면 NetworkPlayer에 구매 Rpc를 추가해 BuyShopItem을 서버에서 부르게 한다.
    public bool CanBuyShopItems(NetworkPlayer actor) => actor != null && actor.OwnerClientId == NetworkManager.ServerClientId;

    // 추후 변경점: 재화는 행동별 포인트로 바뀔 예정. 지금은 기존 공동 AP를 쓴다.
    public int ShopPoints => Affection.Value;

    // 영구 상품을 가졌는지. 목록이 바뀔 때만(늘어날 때만) 비트마스크를 다시 만든다.
    public bool Owns(ShopPerk perk)
    {
        if (!IsSpawned) return false;
        if (ownedCount != PurchasedItems.Count)
        {
            ownedMask = 0;
            foreach (int index in PurchasedItems) if (index >= 0 && index < 64) ownedMask |= 1UL << index;
            ownedCount = PurchasedItems.Count;
        }
        return (ownedMask & (1UL << (int)perk)) != 0;
    }

    public int DetachedTireNumber => detachedTire != null ? tires.IndexOf(detachedTire) + 1 : 0;

    public bool CheerActive => IsSpawned && CheerUntil.Value > NetworkManager.ServerTime.Time;

    private void RollShopOffer()
    {
        if (!IsServer || shopCatalog == null) return;
        ShopOffer.Clear();
        ShopHearts.Clear();
        for (int slot = 0; slot < ShopSlots; slot++) ShopHearts.Add(0);
        int count = shopCatalog.items.Length;
        // 이미 산 영구 상품을 뺀 후보에서 하나씩 뽑아 겹치지 않게 한다.
        var pool = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            var item = shopCatalog.items[i];
            if (item != null && (item.consumable || !PurchasedItems.Contains(i))) pool.Add(i);
        }
        for (int slot = 0; slot < ShopSlots && pool.Count > 0; slot++)
        {
            int pick = Random.Range(0, pool.Count);
            ShopOffer.Add(pool[pick]);
            pool.RemoveAt(pick);
        }
    }

    // 구매자가 아닌 사람이 갖고 싶은 상품에 하트를 누른다. 누를 때마다 1씩 오른다.
    public void AddShopHeart(NetworkPlayer actor, int slot, int round)
    {
        if (!IsServer || !ChoosingUpgrade || round != ShopRound.Value || CanBuyShopItems(actor)) return;
        if (slot < 0 || slot >= ShopHearts.Count || slot >= ShopOffer.Count || ShopOffer[slot] == SoldOut) return;
        ShopHearts[slot]++;
    }

    public void BuyShopItem(NetworkPlayer actor, int slot)
    {
        if (!IsServer || !ChoosingUpgrade || Report.Value < 0 || !CanBuyShopItems(actor)) return;
        if (slot < 0 || slot >= ShopOffer.Count || ShopOffer[slot] == SoldOut) return;
        int index = ShopOffer[slot];
        var item = shopCatalog.Get(index);
        if (item == null || ShopPoints < item.price) return;
        Affection.Value -= item.price;
        ShopOffer[slot] = SoldOut;
        if (index == (int)ShopPerk.EmergencyRepair)
        {
            durability = Capacity;
            if (wheelDetached) RepairWheel();
        }
        else if (item.consumable) pendingItems.Add(index);
        else
        {
            PurchasedItems.Add(index);
            if (index == (int)ShopPerk.ReinforcedHull) durability = Mathf.Min(Capacity, durability + HullBonus);
        }
        Refresh();
    }

    // 스테이지 시작(다음 구간 출발) 때: 스테이지마다 한 번 쓰는 영구 효과를 채우고, 사 둔 일회성 상품을 적용한다.
    private void ApplyStageItems()
    {
        if (!IsServer) return;
        rattleReady = Owns(ShopPerk.SilverRattle);
        coverReady = Owns(ShopPerk.MischiefCover);
        strapReady = false;
        reloadRetryUsed = relayUsed = false;
        lastCleaner = null;
        ShelfShells.Value = 0;
        SpareKitReady.Value = false;
        CheerUntil.Value = 0d;
        foreach (int index in pendingItems)
        {
            switch ((ShopPerk)index)
            {
                case ShopPerk.StartCarrots: cart.Kickstart(CarrotStartRate); break;
                case ShopPerk.SupplyCrate: depositedShells += SupplyCrateShells; break;
                case ShopPerk.OiledCloth: cleaningTaps = Mathf.Max(cleaningTaps, CleaningTapsRequired / 2); break;
                case ShopPerk.SpareWheelKit: SpareKitReady.Value = true; break;
                case ShopPerk.PremadeSnack: GiveSnack(); break;
                case ShopPerk.CheerFlag: CheerUntil.Value = NetworkManager.ServerTime.Time + CheerSeconds; break;
                case ShopPerk.SafetyStrap: strapReady = true; break;
            }
        }
        pendingItems.Clear();
    }

    private void GiveSnack()
    {
        foreach (var player in gameScene.Players)
        {
            if (!player.Assigned.Value || !player.HandsFree) continue;
            player.Snack.Value = true;
            return;
        }
    }

    // 포신 청소 한 번에 오르는 칸 수. 솔은 2칸, 교대 솔걸이는 청소 한 번마다 첫 교대 때 3칸을 더 준다.
    private int CleaningTapGain(NetworkPlayer actor)
    {
        if (cleaningTaps == 0) { relayUsed = false; lastCleaner = null; }
        int gain = Owns(ShopPerk.CleaningBrush) ? BrushTapGain : 1;
        if (Owns(ShopPerk.RelayBrushRack) && !relayUsed && lastCleaner != null && lastCleaner != actor)
        {
            gain += RelayTapBonus;
            relayUsed = true;
        }
        lastCleaner = actor;
        return gain;
    }

    // 장전 실패 때 포탄을 잃지 않는지: 회수판은 살짝 벗어난 실패, 보조 발판은 장전마다 첫 실패를 살린다.
    private bool KeepsShellOnReloadFail(bool nearMiss)
    {
        if (Owns(ShopPerk.RecoveryPlate) && nearMiss) return true;
        if (!Owns(ShopPerk.ReloadStep) || reloadRetryUsed) return false;
        reloadRetryUsed = true;
        return true;
    }

    // 작업대별 타이밍 성공 구간 추가(도).
    public float ShopTimingBonus(StationAction? action, int repairStep) => action switch
    {
        StationAction.Reload => Owns(ShopPerk.ReloadGauge) ? TimingBonusPerTool : 0f,
        StationAction.ShellPile => Owns(ShopPerk.PileTongs) ? TimingBonusPerTool : 0f,
        StationAction.WheelRepair => Owns(repairStep == 0 ? ShopPerk.MountGuide : ShopPerk.PinClamp) ? TimingBonusPerTool : 0f,
        _ => 0f
    };

    public float ShopCatchBonus => Owns(ShopPerk.CatchGloves) ? TimingBonusPerTool : 0f;

    // 비상 제동 손잡이: 마부가 당기면 잠깐 속도를 낮춘다(쿨다운 있음).
    public void PullBrake(NetworkPlayer actor)
    {
        if (!IsServer || !Running || !Owns(ShopPerk.EmergencyBrake) || !HoldsStation(StationAction.Driver, actor)) return;
        double now = NetworkManager.ServerTime.Time;
        if (now < brakeReadyAt) return;
        brakeReadyAt = now + BrakeCooldown;
        cart.ApplyObstacleSlow(BrakeSlowFactor, BrakeSeconds);
    }

    // 장전대 호출벨: 적재함이 비었는데 장전대에 사람이 있으면 그 사람 이름으로 "포탄 요청"을 띄운다.
    private void RingCallBell()
    {
        if (!Owns(ShopPerk.CallBell) || depositedShells > 0 || loaded || CannonAction != StationAction.Reload) return;
        double now = NetworkManager.ServerTime.Time;
        if (now < bellReadyAt) return;
        foreach (var player in gameScene.Players)
        {
            if (!player.Assigned.Value || player.Bot.Value || !HoldsStation(StationAction.Cannon, player)) continue;
            bellReadyAt = now + CallBellCooldown;
            player.Emote.Value = 1;
            player.EmoteTime.Value = now;
            return;
        }
    }

    // 계단 전달 선반: 지하층에서 들고 온 포탄을 올려 두거나, 갑판에서 빈손으로 받아 간다.
    public bool CanUseStairShelf(NetworkPlayer actor, Vector2 deckStairs, Vector2 cellarStairs)
    {
        if (actor == null || !Running || !Owns(ShopPerk.StairShelf) || actor.Holding.Value) return false;
        float radius = StairShelfRadius * WorldPerWebUnit;
        Vector2 position = actor.transform.position;
        if (actor.InCellar)
            return actor.Carrying.Value && ShelfShells.Value == 0 && Vector2.Distance(position, cellarStairs) <= radius;
        return actor.HandsFree && ShelfShells.Value > 0 && Vector2.Distance(position, deckStairs) <= radius;
    }

    public void UseStairShelf(NetworkPlayer actor, Vector2 deckStairs, Vector2 cellarStairs)
    {
        if (!IsServer || !CanUseStairShelf(actor, deckStairs, cellarStairs)) return;
        if (actor.InCellar)
        {
            actor.Carrying.Value = false;
            ShelfShells.Value = 1;
        }
        else
        {
            actor.Carrying.Value = true;
            ShelfShells.Value = 0;
        }
        Refresh();
    }
}

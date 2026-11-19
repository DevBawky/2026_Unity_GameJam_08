using System;
using Unity.Netcode;
using UnityEngine;

public enum CareAction { None, Soothe, Reseat, Feed, PickUp, Attend }
public enum RiderPose : byte { Seated, Wandering, Fallen, Carried }

[Serializable]
public struct RiderState : INetworkSerializeByMemcpy
{
    public RiderPose Pose;
    public float Attention, Hunger;
    public bool Upset, Bored, BoredomPending, InCellar;
    public float Soothing;
    public int AttentionHeartUntil;
    public Vector2 AttentionHeartFrom;
    public Vector2 LastFoot, BoredFrom;
    public int Mischief, NextMischief;
    public Vector2 Destination;
    public bool Crying => Upset || Hunger >= 70f;
    public int CarrierSlot;
    public Vector2 Position;
}

// The host samples both children once per simulation step. Positions are cart-local.
public sealed partial class CartStations
{
    private const float Step = 1f / 30f;
    private double accumulator;
    [SerializeField] private EnemySpawner waveSpawner;
    public Bounds PursuerEntryBounds => waveSpawner.SpawnBounds;
    [SerializeField] private Transform[] royalVisuals;
    [SerializeField] private SpriteSequence[] royalWalks;
    [SerializeField] private Sprite[] princePoses;
    [SerializeField] private Sprite[] princessPoses;
    private readonly Vector2[] seats = new Vector2[2];
    [SerializeField] private Transform[] royalChairs;
    [SerializeField] private UnityEngine.UI.Image[] royalMarkers;
    private Vector3[] chairStarts;
    private readonly NetworkVariable<CartWorkShock> royalSeatBounce = new();
    public NetworkVariable<RiderState> Prince = new(), Princess = new();
    public NetworkVariable<int> Waves = new(), Affection = new(), ShopRound = new(), Report = new(-1);
    public NetworkVariable<int> Barrel = new(), Stabilizer = new(), Porter = new(), Hull = new();
    public NetworkVariable<int> PrincePoints = new(), PrincessPoints = new();
    public NetworkList<int> Votes = new();
    private int careTicks;
    private readonly int[] seated = new int[2], crying = new int[2], ignoreWanderUntil = new int[2];
    private readonly double[] attentionSum = new double[2];
    private uint randomState;
    internal float Sample()
    {
        randomState ^= randomState << 13; randomState ^= randomState >> 17; randomState ^= randomState << 5;
        return (randomState >> 8) / 16777216f;
    }
    public float PorterMultiplier => 1f + CartRules.Active.porter.amount * Porter.Value + (Owns(ShopPerk.Dumbbell) ? DumbbellPorterBonus : 0f);
    public float SpeedLimit => StageGoal / (stageNumber == 1 ? 52.5f : stageNumber == 2 ? 59.5f : 63.5f)
        * (Owns(ShopPerk.LightWheels) ? LightWheelsLimitRate : 1f);
    public bool ChapterComplete => flow == StageFlow.Complete;
    public float Capacity => MaxDurability + CartRules.Active.hull.amount * Hull.Value + (Owns(ShopPerk.ReinforcedHull) ? HullBonus : 0f);
    public bool IsCarryingChild(NetworkPlayer p) => IsCarrier(Prince.Value,p) || IsCarrier(Princess.Value,p);
    private static bool IsCarrier(RiderState child, NetworkPlayer p) => child.Pose == RiderPose.Carried && child.CarrierSlot == p.Slot.Value;

    public CrewSpriteSet.Pose CrewPose(NetworkPlayer player)
    {
        if (IsCarrier(Prince.Value, player)) return CrewSpriteSet.Pose.Prince;
        if (IsCarrier(Princess.Value, player)) return CrewSpriteSet.Pose.Princess;
        if (player.Carrying.Value) return CrewSpriteSet.Pose.Shell;
        if (player.Snack.Value) return CrewSpriteSet.Pose.Snack;
        if (player.SpareWheel.Value) return CrewSpriteSet.Pose.SpareWheel;
        return CrewSpriteSet.Pose.Idle;
    }

    public int CryingCount => (Prince.Value.Crying ? 1 : 0) + (Princess.Value.Crying ? 1 : 0);
    public float TimingWidthRate => CryingCount == 2 ? .60f : CryingCount == 1 ? .80f : 1f;
    public float WalkRate => CryingCount == 2 ? .75f : CryingCount == 1 ? .90f : 1f;
    private float CookRate => CryingCount == 2 ? .65f : CryingCount == 1 ? .85f : 1f;
    public bool CarriedChild(NetworkPlayer actor, out RiderState child)
    {
        child = Prince.Value;
        if (IsCarrier(child, actor)) return true;
        child = Princess.Value;
        return IsCarrier(child, actor);
    }

    public readonly NetworkVariable<CartWorkShock> PrinceHeart = new(), PrincessHeart = new();

    internal bool RoyalNearby(NetworkPlayer viewer, RiderState child)
    {
        if (viewer == null || !viewer.Assigned.Value || viewer.InCellar != child.InCellar) return false;
        Vector2 childWorld = cart.transform.TransformPoint(child.Position);
        return Vector2.Distance(childWorld, viewer.transform.position) <= 52f * WorldPerWebUnit;
    }

    internal StationAction? AttentionTarget(NetworkPlayer actor)
    {
        if (!actor.HandsFree) return null;
        var prince = Prince.Value;
        var princess = Princess.Value;
        bool first = prince.Pose == RiderPose.Seated && RoyalNearby(actor, prince);
        bool second = princess.Pose == RiderPose.Seated && RoyalNearby(actor, princess);
        if (!first && !second) return null;
        Vector2 foot = cart.transform.InverseTransformPoint(actor.transform.position);
        return first && (!second || (foot - prince.Position).sqrMagnitude <= (foot - princess.Position).sqrMagnitude)
            ? StationAction.RoyalFamily : StationAction.Princess;
    }

    internal bool WithinRoyalInteraction(StationAction action, NetworkPlayer actor)
    {
        int index = action == StationAction.Princess ? 1 : 0;
        var child = index == 0 ? Prince.Value : Princess.Value;
        if (child.InCellar != actor.InCellar) return false;
        bool atSeat = child.Pose is RiderPose.Seated or RiderPose.Carried;
        Vector2 point = RoyalInteractionPosition(child, index);
        float radius = atSeat ? 9f : 30f;
        return Vector2.Distance(point, actor.transform.position) <= radius * WorldPerWebUnit;
    }

    private float RoyalSeatLift()
    {
        var bounce = royalSeatBounce.Value;
        if (bounce.Strength <= 0f || SettingsPanel.ReducedMotion) return 0f;
        float progress = Mathf.Clamp01((float)(NetworkManager.ServerTime.Time - bounce.Time) / .4f);
        return (12f + 7f * bounce.Strength) * 4f * progress * (1f - progress) * WorldPerWebUnit;
    }

    public const float SootheDistance = 312.5f;
    // 포대기가 있으면 더 짧게 걸어도 달래진다.
    public float SootheTarget => Owns(ShopPerk.Swaddle) ? SootheDistance * SwaddleRate : SootheDistance;
    private void ResetCare()
    {
        careTicks = 0;
        Array.Clear(nextBotAction,0,nextBotAction.Length);
        Array.Fill(botJobs,-1);
        Array.Clear(walkingRight,0,walkingRight.Length);
        randomState = (uint)(1987 + stageNumber);
        Array.Clear(seated,0,2);
        Array.Clear(crying,0,2);
        Array.Clear(attentionSum,0,2);
        for (int i=0;i<2;i++)
        {
            var value = new RiderState { Pose=RiderPose.Seated, Position=seats[i], Attention=i == 0 ? 75 : 63, Hunger=i == 0 ? 14 : 22, CarrierSlot=-1 };
            if (i==0) Prince.Value=value; else Princess.Value=value;
            ignoreWanderUntil[i]=0;
        }
        HorseRage.Value = default;
        royalSeatBounce.Value = default;
        PrinceHeart.Value = PrincessHeart.Value = default;
        Waves.Value = 0;
        Report.Value = -1;
        KingReply.Value = default;
        Votes.Clear();
        for (int i=0;i<4;i++) Votes.Add(-1);
    }

    private void AdvanceCare()
    {
        careTicks++;
        AdvanceHorseRage();
        AdvanceChild(Prince,0);
        AdvanceChild(Princess,1);
        const float cookLossSecondsPerShock = .55f;
        float cookSeconds = CartRules.Active.cookSeconds;
        float potRate = Owns(ShopPerk.FixedPot) ? FixedPotRate : 1f;
        int lostTicks = Mathf.FloorToInt(pendingCookingShock * cookLossSecondsPerShock * potRate / Step + .5f);
        pendingCookingShock = 0f;
        foreach (var actor in gameScene.Players)
        {
            if (!actor.Assigned.Value) continue;
            if (!HoldsStation(StationAction.Kitchen, actor))
            {
                if (actor.CookProgress.Value > 0f) actor.CookProgress.Value = 0f;
                continue;
            }

            float loss = Mathf.Min(actor.CookProgress.Value, lostTicks * Step);
            if (loss > 0f)
            {
                actor.CookProgress.Value -= loss;
                actor.ShowCookingImpact(Mathf.FloorToInt(loss * 100f / cookSeconds + .5f));
            }
            if (!actor.CookingHeld.Value || !actor.HandsFree) continue;

            actor.CookProgress.Value = Mathf.Min(cookSeconds, actor.CookProgress.Value + Step * CookRate);
            if (actor.CookProgress.Value < cookSeconds - .00001f) continue;
            actor.Snack.Value = true;
            actor.CookProgress.Value = 0f;
            CompleteStation(StationAction.Kitchen, actor);
        }
    }

    private void AdvanceChild(NetworkVariable<RiderState> field, int index)
    {
        var child=field.Value;
        child.Hunger = Mathf.Min(100, child.Hunger + Step * 1.65f);
        if (child.Pose == RiderPose.Seated && careTicks >= ignoreWanderUntil[index])
        {
            child.Attention = Mathf.Max(0, child.Attention - Step * 4.2f);
            if (child.Attention <= 20)
            {
                child.Pose = RiderPose.Wandering;
                child.NextMischief = careTicks + 120 + (int)(Sample() * 60);
                child.Destination = seats[index] + Vector2.down * .6f;
            }
        }
        if (child.Pose == RiderPose.Wandering) Wander(ref child);
        if (child.Pose == RiderPose.Carried)
        {
            var carrier = gameScene.Players[child.CarrierSlot];
            if (carrier.Assigned.Value) WalkChild(ref child, carrier);
            else { child.Pose = RiderPose.Fallen; child.CarrierSlot = -1; }
        }
        if (child.Pose == RiderPose.Seated) seated[index]++;
        if (child.Crying) crying[index]++;
        attentionSum[index]+=child.Attention;
        field.Value=child;
    }

    private void Wander(ref RiderState child)
    {
        if (child.Mischief == 0 && careTicks >= child.NextMischief)
        {
            var action = (StationAction)(Sample() < .5f && depositedShells > 0 ? (int)StationAction.ShellDeposit :
                loaded && !fuseLit && Sample() < .5f ? (int)StationAction.Cannon : (int)StationAction.Driver);
            var area = gameScene.Players[0].Timing.AreaFor(action);
            if (area != null)
            {
                child.Mischief = (int)action + 1;
                child.Destination = cart.transform.InverseTransformPoint(area.position);
            }
            child.NextMischief = careTicks + 270 + (int)(Sample() * 120);
        }
        child.Position = Vector2.MoveTowards(child.Position, child.Destination, 56f * WorldPerWebUnit * Step);
        if (Vector2.Distance(child.Position, child.Destination) > .05f) return;
        switch ((StationAction)(child.Mischief - 1))
        {
            case StationAction.ShellDeposit:
                if (coverReady) coverReady = false;
                else if (depositedShells > 0) { depositedShells--; lostShells++; }
                break;
            case StationAction.Cannon:
                if (loaded && !fuseLit) { gunner = null; StartFuse(0); }
                break;
            case StationAction.Driver: WhipHorse(); break;
        }
        child.Mischief = 0;
        Bounds bounds = CartBody.bounds;
        child.Destination = cart.transform.InverseTransformPoint(new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, .1f + .8f * Sample()),
            Mathf.Lerp(bounds.min.y, bounds.max.y, .1f + .7f * Sample()), bounds.center.z));
    }

    private void WalkChild(ref RiderState child, NetworkPlayer carrier)
    {
        Vector2 foot = cart.transform.InverseTransformPoint(carrier.transform.position);
        bool changedFloor = child.InCellar != carrier.InCellar;
        float distance = changedFloor ? 0f : Vector2.Distance(foot, child.LastFoot) / WorldPerWebUnit;
        child.LastFoot = foot;
        child.InCellar = carrier.InCellar;
        child.Position = foot + Vector2.up * .35f;
        if (!child.Upset) return;
        if (child.Bored)
        {
            var bounds = cartBody.bounds;
            float x = carrier.transform.position.x;
            bool nearEnd = Mathf.Min(x - bounds.min.x, bounds.max.x - x) <= 80f * WorldPerWebUnit;
            if (child.InCellar || nearEnd && Vector2.Distance(foot, child.BoredFrom) >= 82f * WorldPerWebUnit)
                child.Bored = false;
            return;
        }
        child.Soothing = Mathf.Min(SootheTarget, child.Soothing + distance);
        if (child.BoredomPending && child.Soothing >= SootheTarget / 3f)
        {
            child.BoredomPending = false;
            // 은방울 딸랑이: 스테이지마다 한 번, 지루해져 달래기가 멈추는 것을 막는다.
            if (!child.InCellar && rattleReady) rattleReady = false;
            else if (!child.InCellar)
            {
                child.Bored = true;
                child.BoredFrom = foot;
                return;
            }
        }
        if (child.Soothing >= SootheTarget) child.Upset = false;
    }

    private void LateUpdate()
    {
        if (cannonVisual != null)
            cannonVisual.sprite = IsFiring ? cannonFuse : cannonReady;
        if (muzzleVisual != null)
        {
            var shot = LastCannonShot.Value;
            double now = IsSpawned ? NetworkManager.ServerTime.Time : 0d;
            float age = shot.Time >= 0d && IsSpawned ? (float)(now - shot.Time) : -1f;
            muzzleVisual.enabled = muzzleFlash != null && muzzleFlash.PlayingAt(age);
            if (muzzleVisual.enabled)
            {
                muzzleVisual.sprite = muzzleFlash.FrameAt(SettingsPanel.ReducedMotion ? 0f : age);
                muzzleVisual.transform.rotation = Quaternion.Euler(0f, 0f, shot.Angle);
                Vector3 direction = muzzleVisual.transform.right;
                float unit = WorldPerWebUnit * CannonCaliber;
                muzzleVisual.transform.position = RenderedMuzzle(direction, now)
                    + (direction * 53f + muzzleVisual.transform.up * 3f) * unit;
                float size = 119f * unit / muzzleVisual.sprite.bounds.size.x;
                var parent = muzzleVisual.transform.parent.lossyScale;
                muzzleVisual.transform.localScale = new Vector3(size / parent.x, size / parent.y, 1f);
            }
        }
        if (IsSpawned)
        {
            float time = (float)NetworkManager.ServerTime.Time;
            if (horseWhipZones != null && horseWhipZones.Length == 2)
            {
                horseWhipZones[0]?.ShowGallop(Running, HorseRage.Value.Value > 0, CurrentHorseMood, time);
                horseWhipZones[1]?.ShowGallop(Running, HorseRage.Value.Value > 0, CurrentHorseMood, time);
            }
        }
        if (!IsSpawned || !Running || royalVisuals == null || royalVisuals.Length != 2) return;
        ShowWheels();
        float lift = RoyalSeatLift();
        for (int i = 0; i < chairStarts.Length; i++)
            royalChairs[i].localPosition = chairStarts[i] + Vector3.up * lift;
        PlaceChild(Prince.Value,0, lift);
        PlaceChild(Princess.Value,1, lift);
        if (royalMarkers is { Length: 2 })
        {
            ShowRoyalMarker(royalMarkers[0], Prince.Value);
            ShowRoyalMarker(royalMarkers[1], Princess.Value);
        }
    }
    private void ShowRoyalMarker(UnityEngine.UI.Image marker, RiderState child)
    {
        bool visible = false;
        foreach (var player in Crew)
        {
            if (player == null || !player.Local || player.InCellar) continue;
            if (child.Pose == RiderPose.Seated && player.Snack.Value
                || child.Pose == RiderPose.Carried && !child.Upset && child.CarrierSlot == player.Slot.Value)
            {
                visible = true;
                break;
            }
        }
        marker.enabled = visible;
    }

    private void PlaceChild(RiderState child,int index, float lift)
    {
        var visual=royalVisuals[index];
        visual.position=cart.transform.TransformPoint(child.Position);
        var poses = index == 0 ? princePoses : princessPoses;
        if (poses != null && poses.Length == 4 && visual.TryGetComponent(out SpriteRenderer newSprite))
        {
            newSprite.enabled = child.Pose != RiderPose.Carried;
            newSprite.sortingOrder = WorldDepth.Order(visual.position, true);
            bool seated = child.Pose == RiderPose.Seated;
            if (seated) visual.position += Vector3.up * (15f * WorldPerWebUnit + lift);
            visual.localScale = Vector3.one * (WorldPerWebUnit * 100f * (seated ? 1.42f : 1f));
            newSprite.sprite = poses[seated ? 0 : child.Bored ? 3 : child.Crying ? 2 : 1];
        }
        if (poses != null && poses.Length == 5 && visual.TryGetComponent(out SpriteRenderer sprite))
        {
            sprite.enabled = child.Pose != RiderPose.Carried;
            if (child.Pose == RiderPose.Seated) visual.position += Vector3.up * lift;
            float size = WorldPerWebUnit * 100f * (child.Pose == RiderPose.Seated ? .83f : 1f);
            visual.localScale = Vector3.one * size;
            sprite.sprite = child.Pose == RiderPose.Fallen ? poses[1]
                : child.Crying ? poses[2]
                : child.Bored ? poses[3]
                : child.Pose == RiderPose.Seated ? poses[0]
                : child.Pose == RiderPose.Wandering && royalWalks != null && index < royalWalks.Length && royalWalks[index] != null
                    ? royalWalks[index].FrameAt(Time.time, true)
                    : poses[4];
        }
    }

    private Vector3 RoyalInteractionPosition(RiderState child, int index) =>
        cart.transform.TransformPoint(child.Pose is RiderPose.Seated or RiderPose.Carried ? seats[index] + Vector2.down * (40f * WorldPerWebUnit) : child.Position);

    public CareAction AvailableCare(StationAction action, NetworkPlayer actor)
    {
        var child = action == StationAction.Princess ? Princess.Value : Prince.Value;
        if (AttentionTarget(actor) == action)
        {
            var effect = (action == StationAction.Princess ? PrincessHeart : PrinceHeart).Value;
            bool waiting = effect.Strength > 0f && actor.NetworkManager.ServerTime.Time < effect.Time + .6d;
            if (child.Attention < 100f && !waiting) return CareAction.Attend;
        }
        if (!WithinRoyalInteraction(action, actor)) return CareAction.None;
        if (child.Pose == RiderPose.Carried && !IsCarrier(child, actor)) return CareAction.None;
        if (IsCarrier(child, actor)) return !child.Upset && !actor.InCellar ? CareAction.Reseat : CareAction.None;
        if (child.Pose == RiderPose.Seated) return actor.Snack.Value ? CareAction.Feed : CareAction.None;
        return actor.HandsFree ? CareAction.PickUp : CareAction.None;
    }

    private void CareFor(NetworkVariable<RiderState> field, NetworkPlayer actor, int index)
    {
        var child=field.Value;
        var station = index == 0 ? StationAction.RoyalFamily : StationAction.Princess;
        switch (AvailableCare(station, actor))
        {
            case CareAction.Attend:
                if (!actor.Timing.IsInActionArea(station) || actor.CanUseStairShelf()
                    || child.Attention >= 100f || careTicks < child.AttentionHeartUntil) return;
                child.Attention = Mathf.Min(100f, child.Attention + 20f);
                child.AttentionHeartUntil = careTicks + 18;
                child.AttentionHeartFrom = cart.transform.InverseTransformPoint(actor.transform.position);
                var effect = index == 0 ? PrinceHeart : PrincessHeart;
                effect.Value = new CartWorkShock { Time = NetworkManager.ServerTime.Time, Strength = 1f };
                break;
            case CareAction.Reseat:
                actor.AwardScore(CartRules.ChildReseatScore);
                child.Pose = RiderPose.Seated;
                child.Position = seats[index];
                child.CarrierSlot = -1;
                child.Attention = 100f;
                child.Soothing = 0;
                child.Bored = child.BoredomPending = child.InCellar = false;
                ignoreWanderUntil[index] = careTicks + (Owns(ShopPerk.PictureBook) ? PictureBookTicks : 210);
                break;
            case CareAction.Feed:
                child.Hunger = Mathf.Max(0, child.Hunger - (Owns(ShopPerk.HeartyRecipe) ? HeartyRecipeHunger : 55));
                actor.Snack.Value = false;
                break;
            case CareAction.PickUp:
                child.Pose = RiderPose.Carried;
                child.CarrierSlot = actor.Slot.Value;
                child.LastFoot = cart.transform.InverseTransformPoint(actor.transform.position);
                child.InCellar = actor.InCellar;
                child.Bored = false;
                child.BoredomPending = child.Upset && Sample() < .30f;
                child.Mischief = 0;
                break;
            default:
                return;
        }
        field.Value=child;
    }

    private void KnockDown(NetworkVariable<RiderState> field,float chance)
    {
        var child=field.Value;
        if (Owns(ShopPerk.SeatCushion)) chance *= CushionFallRate;
        if(child.Pose!=RiderPose.Seated || Sample()>=chance) return;
        child.Pose=RiderPose.Fallen;
        child.Position += Vector2.down * (40f * WorldPerWebUnit);
        child.Upset=Sample()<0.85;
        child.Soothing=0;
        field.Value=child;
    }
}

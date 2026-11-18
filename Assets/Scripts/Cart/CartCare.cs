using System;
using Unity.Netcode;
using UnityEngine;

public enum HorseMood : byte { Normal, Angry, Runaway, Tired }
[Serializable]
public struct HorseCondition : INetworkSerializeByMemcpy
{
    public HorseMood Mood;
    public int Remaining;
}

public enum CareAction { None, Soothe, Reseat, Feed, PickUp, Attend }
public enum RiderPose : byte { Seated, Wandering, Fallen, Carried }

[Serializable]
public struct RiderState : INetworkSerializeByMemcpy
{
    public RiderPose Pose;
    public float Attention, Hunger;
    public bool Upset, Bored, BoredomPending, InCellar;
    public float Soothing;
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
    internal const int RunawayHits = 8, RunawayTicks = 36;
    internal const float RunawaySteeringSpeed = 176f;
    private double accumulator;
    [SerializeField] private EnemySpawner waveSpawner;
    public Bounds PursuerEntryBounds => waveSpawner.SpawnBounds;
    [SerializeField] private Transform[] royalVisuals;
    [SerializeField] private SpriteSequence[] royalWalks;
    [SerializeField] private Sprite[] princePoses;
    [SerializeField] private Sprite[] princessPoses;
    [SerializeField] private RectTransform[] royalAreas;
    private readonly Vector2[] seats = new Vector2[2];
    public NetworkVariable<RiderState> Prince = new(), Princess = new();
    public NetworkVariable<int> Waves = new(), Affection = new(), ShopRound = new(), Report = new(-1);
    public NetworkVariable<int> Barrel = new(), Stabilizer = new(), Porter = new(), Hull = new();
    public NetworkVariable<int> PrincePoints = new(), PrincessPoints = new();
    public NetworkVariable<int> RightWounds = new(), LeftWounds = new();
    public NetworkList<int> Votes = new();
    [SerializeField] private SpriteRenderer driverCursor;
    [SerializeField] private HorseWhipZone[] horseWhipZones;
    public NetworkVariable<HorseCondition> LeftHorse = new(), RightHorse = new();
    private readonly int[] nextHorseRest = new int[2];
    private HorseWhipZone WhipZone(Vector2 aim) => horseWhipZones[aim.y >= 0f ? 1 : 0];
    public bool AimsAtSteeringZone(Vector2 aim)
    {
        var zone = WhipZone(aim);
        return zone.SteersAt(HorseWhipZone.CursorUV(aim));
    }
    private int careTicks;
    private readonly int[] seated = new int[2], crying = new int[2], ignoreWanderUntil = new int[2];
    private readonly double[] attentionSum = new double[2];
    private uint randomState;
    internal float Sample()
    {
        randomState ^= randomState << 13; randomState ^= randomState >> 17; randomState ^= randomState << 5;
        return (randomState >> 8) / 16777216f;
    }
    public float PorterMultiplier => 1f + CartRules.Active.porter.amount * Porter.Value;
    public float SpeedLimit => StageGoal / (stageNumber == 1 ? 52.5f : stageNumber == 2 ? 59.5f : 63.5f);
    public bool ChapterComplete => flow == StageFlow.Complete;
    public float Capacity => MaxDurability + CartRules.Active.hull.amount * Hull.Value;
    public NetworkVariable<Unity.Collections.FixedString512Bytes> KingReply = new();
    public const int Departure = 8;
    public bool CanBuy(int choice)
    {
        var card = CartRules.Active.GetCard(choice);
        return Affection.Value >= card.price && (choice == 0 ? durability < Capacity || wheelDetached : CardLevel(choice) < card.maxLevel);
    }
    public static readonly string[] Reports = { "왕족 돌봄", "마차 피해", "포탄 보급" };
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

    internal Vector3 RoyalStatusAnchor(RiderState child, int index)
    {
        if (child.Pose == RiderPose.Carried && child.CarrierSlot >= 0 && child.CarrierSlot < gameScene.Players.Length)
        {
            var carrier = gameScene.Players[child.CarrierSlot];
            if (carrier != null && carrier.Assigned.Value)
            {
                // The carry sprites draw the child on the left, with feet below the crew pivot.
                return carrier.transform.position + new Vector3(-13f, -16f, 0f) * WorldPerWebUnit;
            }
        }

        if (royalVisuals != null && index < royalVisuals.Length && royalVisuals[index] != null)
            return royalVisuals[index].TransformPoint(Vector3.down * .16f);
        return cart.transform.TransformPoint(child.Position) + Vector3.down * (16f * WorldPerWebUnit);
    }

    public const float SootheDistance = 312.5f;
    private void ResetCare()
    {
        careTicks = 0;
        nextDodgeWhip = 0;
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
        Array.Clear(nextHorseRest,0,2);
        LeftHorse.Value = RightHorse.Value = default;
        RightWounds.Value = LeftWounds.Value = 0;
        Waves.Value = 0;
        Report.Value = -1;
        KingReply.Value = default;
        Votes.Clear();
        for (int i=0;i<4;i++) Votes.Add(-1);
    }

    private void AdvanceCare()
    {
        careTicks++;
        AdvanceHorse(false);
        AdvanceHorse(true);
        AdvanceChild(Prince,0);
        AdvanceChild(Princess,1);
        foreach (var actor in gameScene.Players)
        {
            if (!actor.Assigned.Value) continue;
            if (actor.CookingHeld.Value && HoldsStation(StationAction.Kitchen,actor) && actor.HandsFree)
            {
                actor.CookProgress.Value = Mathf.Min(CartRules.Active.cookSeconds, actor.CookProgress.Value + Step * CookRate);
                if (actor.CookProgress.Value >= CartRules.Active.cookSeconds - .00001f)
                {
                    actor.Snack.Value = true;
                    actor.CookProgress.Value = 0;
                    CompleteStation(StationAction.Kitchen, actor);
                }
            }
            else if (!HoldsStation(StationAction.Kitchen, actor) && actor.CookProgress.Value > 0) actor.CookProgress.Value=0;
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
                if (depositedShells > 0) { depositedShells--; lostShells++; }
                break;
            case StationAction.Cannon:
                if (loaded && !fuseLit) { gunner = null; StartFuse(0); }
                break;
            case StationAction.Driver: WhipRump(Sample() >= .5f); break;
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
        child.Soothing = Mathf.Min(SootheDistance, child.Soothing + distance);
        if (child.BoredomPending && child.Soothing >= SootheDistance / 3f)
        {
            child.BoredomPending = false;
            if (!child.InCellar)
            {
                child.Bored = true;
                child.BoredFrom = foot;
                return;
            }
        }
        if (child.Soothing >= SootheDistance) child.Upset = false;
    }

    private void LateUpdate()
    {
        ShowDriverAim();
        if (cannonVisual != null)
            cannonVisual.sprite = IsFiring ? cannonFuse : cannonReady;
        if (muzzleVisual != null)
        {
            float age = Time.time - muzzleFlashStart;
            muzzleVisual.enabled = muzzleFlash.PlayingAt(age);
            if (muzzleVisual.enabled) muzzleVisual.sprite = muzzleFlash.FrameAt(age);
        }
        if (IsSpawned)
        {
            float time = (float)NetworkManager.ServerTime.Time;
            if (horseWhipZones != null && horseWhipZones.Length == 2)
            {
                horseWhipZones[0]?.ShowGallop(Running, LeftWounds.Value > 0, LeftHorse.Value.Mood, time);
                horseWhipZones[1]?.ShowGallop(Running, RightWounds.Value > 0, RightHorse.Value.Mood, time);
            }
        }
        if (!IsSpawned || !Running || royalVisuals == null || royalVisuals.Length != 2) return;
        ShowWheels();
        PlaceChild(Prince.Value,0);
        PlaceChild(Princess.Value,1);
    }
    private void PlaceChild(RiderState child,int index)
    {
        var visual=royalVisuals[index];
        visual.position=cart.transform.TransformPoint(child.Position);
        var poses = index == 0 ? princePoses : princessPoses;
        if (poses != null && poses.Length == 5 && visual.TryGetComponent(out SpriteRenderer sprite))
        {
            sprite.enabled = child.Pose != RiderPose.Carried;
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
        var target = child.Pose == RiderPose.Carried ? seats[index] : child.Position;
        royalAreas[index].position=cart.transform.TransformPoint(target);
    }

    public CareAction AvailableCare(StationAction action, NetworkPlayer actor)
    {
        var child = action == StationAction.Princess ? Princess.Value : Prince.Value;
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
            case CareAction.Reseat:
                child.Pose = RiderPose.Seated;
                child.Position = seats[index];
                child.CarrierSlot = -1;
                child.Attention = 100f;
                child.Soothing = 0;
                child.Bored = child.BoredomPending = child.InCellar = false;
                ignoreWanderUntil[index] = careTicks + 210;
                break;
            case CareAction.Feed:
                child.Hunger = Mathf.Max(0, child.Hunger - 55);
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

    private void ShockCare(float strength, float chance)
    {
        if(randomState==0) return;
        foreach(var player in gameScene.Players)
            if(player.CookProgress.Value>0) player.CookProgress.Value=Mathf.Max(0,player.CookProgress.Value-strength*7);
        KnockDown(Prince,chance);
        KnockDown(Princess,chance);
    }
    private void KnockDown(NetworkVariable<RiderState> field,float chance)
    {
        var child=field.Value;
        if(child.Pose!=RiderPose.Seated || Sample()>=chance) return;
        child.Pose=RiderPose.Fallen;
        child.Position+=Vector2.down*0.65f;
        child.Upset=Sample()<0.85;
        child.Soothing=0;
        field.Value=child;
    }

    private void ShowDriverAim()
    {
        if (driverCursor == null) return;
        if (IsSpawned && Running && horseWhipZones != null && horseWhipZones.Length == 2)
        {
            foreach (var player in gameScene.Players)
            {
                if (!HoldsStation(StationAction.Driver, player)) continue;
                Vector2 aim = player.DriverAim.Value;
                var zone = WhipZone(aim);
                Vector2 uv = HorseWhipZone.CursorUV(aim);
                driverCursor.transform.position = zone.transform.TransformPoint(zone.LocalPoint(uv));
                driverCursor.color = zone.SteersAt(uv) ? new Color(.62f, 1f, .66f) : new Color(1f, .46f, .42f);
                if (!driverCursor.gameObject.activeSelf) driverCursor.gameObject.SetActive(true);
                return;
            }
        }
        if (driverCursor.gameObject.activeSelf) driverCursor.gameObject.SetActive(false);
    }
    public int RunawayDirection => RightHorse.Value.Mood == HorseMood.Runaway ? 1 :
        LeftHorse.Value.Mood == HorseMood.Runaway ? -1 : 0;
    public float FatigueRate => (LeftHorse.Value.Mood == HorseMood.Tired ? .78f : 1f) *
        (RightHorse.Value.Mood == HorseMood.Tired ? .78f : 1f);

    private void AdvanceHorse(bool right)
    {
        int side = right ? 1 : 0;
        var status = right ? RightHorse : LeftHorse;
        var wounds = right ? RightWounds : LeftWounds;
        var state = status.Value;
        if (state.Mood == HorseMood.Normal)
        {
            if (wounds.Value > 0 && careTicks >= nextHorseRest[side])
            {
                wounds.Value--;
                nextHorseRest[side] = careTicks + 24;
            }
            return;
        }
        state.Remaining--;
        if (state.Mood == HorseMood.Tired) wounds.Value = Mathf.CeilToInt(RunawayHits * state.Remaining / 120f);
        if (state.Remaining <= 0)
        {
            if (state.Mood == HorseMood.Runaway)
            {
                cart.ResetHorsePace(right);
                state = new HorseCondition { Mood = HorseMood.Tired, Remaining = 120 };
            }
            else state = default;
        }
        status.Value = state;
    }

    internal void StrikeHorse(Vector2 aim)
    {
        if (!IsServer || !float.IsFinite(aim.x) || !float.IsFinite(aim.y) ||
            Mathf.Abs(aim.x) > 1f || Mathf.Abs(aim.y) > 1f) return;
        bool right = aim.y >= 0f;
        var state = right ? RightHorse : LeftHorse;
        if (state.Value.Mood != HorseMood.Normal) return;
        if (AimsAtSteeringZone(aim)) WhipRump(right);
        else
        {
            cart.ResetHorsePace(right);
            state.Value = new HorseCondition { Mood = HorseMood.Angry, Remaining = 90 };
        }
    }

    private void WhipRump(bool right)
    {
        var wounds = right ? RightWounds : LeftWounds;
        var state = right ? RightHorse : LeftHorse;
        if (state.Value.Mood != HorseMood.Normal || (wounds.Value == RunawayHits - 1 && RunawayDirection != 0)) return;
        nextHorseRest[right ? 1 : 0] = careTicks + 90;
        wounds.Value++;
        cart.BoostHorse(right);
        if (wounds.Value == RunawayHits)
            state.Value = new HorseCondition { Mood = HorseMood.Runaway, Remaining = RunawayTicks };
    }

    private void FinishCare()
    {
        PrincePoints.Value=CarePoints(0);
        PrincessPoints.Value=CarePoints(1);
        Affection.Value+=PrincePoints.Value+PrincessPoints.Value;
        ShopRound.Value++;
    }
    private int CarePoints(int i)
    {
        int n=Mathf.Max(1,careTicks);
        int seatPercent = Mathf.FloorToInt(100f * seated[i] / n + .5f);
        int attention = Mathf.FloorToInt((float)(attentionSum[i] / n) + .5f);
        int score = Mathf.FloorToInt((seatPercent * .6f + attention * .4f) / 10f + .5f);
        int penalty=crying[i]==0 ? 0 : Mathf.Clamp(Mathf.FloorToInt(4f*crying[i]/n + .5f),1,4);
        return Mathf.Max(0,score-penalty);
    }
    public void ReceiveChoice(NetworkPlayer actor,int choice,int round,int stage)
    {
        if(!IsServer || !ChoosingUpgrade || !actor.Assigned.Value || round!=ShopRound.Value || stage!=stageNumber) return;
        if(Report.Value<0)
        {
            if(actor.OwnerClientId!=NetworkManager.ServerClientId || choice<0 || choice>=Reports.Length) return;
            string reply = choice switch {
                0 => crying[0]+crying[1]>0 ? "울었던 시간만큼 감점했다. 놀라면 안고 걷고, 배고프면 간식을 주어라." :
                    PrincePoints.Value+PrincessPoints.Value>=14 ? "두 아이를 잘 돌봤군." : "자리를 비운 아이는 자기 좌석으로 데려다주어라.",
                1 => $"내구도 {Mathf.RoundToInt(durability)}/{Capacity:0}. " + (wheelDetached ? "빠진 바퀴를 수리할지 상의해라." : "수리와 보강 중 무엇이 필요한지 상의해라."),
                _ => $"남은 포탄 {shellsInPile+depositedShells+(loaded?1:0)}발, 분실 {lostShells}발. 장전수와 운반 담당이 연락하게 해라."
            };
            KingReply.Value = new Unity.Collections.FixedString512Bytes(reply);
            Report.Value=choice; ShopRound.Value++; return;
        }
        if(choice<0 || choice>Departure) return;
        if(choice<Departure && !CanBuy(choice)) return;
        Votes[actor.Slot.Value]=choice;
        TallyChoices();
    }

    public void TallyChoices()
    {
        if(!IsServer || !ChoosingUpgrade || Report.Value<0) return;
        int humans=0;
        foreach(var p in gameScene.Players) if(p.Assigned.Value && !p.Bot.Value) humans++;
        if(humans==0) return;
        for(int choice=0;choice<=Departure;choice++)
        {
            int votes=0;
            foreach(var p in gameScene.Players) if(p.Assigned.Value && !p.Bot.Value && Votes[p.Slot.Value]==choice) votes++;
            if(votes<=humans/2) continue;
            ShopRound.Value++;
            for(int i=0;i<Votes.Count;i++) Votes[i]=-1;
            if(choice==Departure) { NextStage(); return; }
            var card = CartRules.Active.GetCard(choice);
            Affection.Value -= card.price;
            switch(choice)
            {
                case 0: durability=Mathf.Min(Capacity,durability+card.amount); if(wheelDetached) RepairWheel(); break;
                case 1: precisionLevel++; break;
                case 2: Stabilizer.Value++; break;
                case 3: Barrel.Value++; break;
                case 4: Hull.Value++; durability+=card.amount; break;
                case 5: Porter.Value++; break;
                case 6: cushionAxleLevel++; break;
                case 7: horseLevel++; break;
            }
            Publish();
            return;
        }
    }
    private int CardLevel(int i) => i switch {1=>precisionLevel,2=>Stabilizer.Value,3=>Barrel.Value,4=>Hull.Value,5=>Porter.Value,6=>cushionAxleLevel,7=>horseLevel,_=>0};
}


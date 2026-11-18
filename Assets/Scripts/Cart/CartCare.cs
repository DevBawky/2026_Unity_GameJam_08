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
    public bool Crying;
    public int CarrierSlot;
    public Vector2 Position;
}

// The host samples both children once per simulation step. Positions are cart-local.
public sealed partial class CartStations
{
    private const float Step = 1f / 30f;
    private double accumulator;
    [SerializeField] private EnemySpawner waveSpawner;
    [SerializeField] private Transform[] royalVisuals;
    [SerializeField] private RectTransform[] royalAreas;
    private readonly Vector2[] seats = new Vector2[2];
    public NetworkVariable<RiderState> Prince = new(), Princess = new();
    public NetworkVariable<int> Waves = new(), Affection = new(), ShopRound = new(), Report = new(-1);
    public NetworkVariable<int> Barrel = new(), Stabilizer = new(), Porter = new(), Hull = new();
    public NetworkVariable<int> PrincePoints = new(), PrincessPoints = new();
    public NetworkVariable<int> RightWounds = new(), LeftWounds = new();
    public NetworkList<int> Votes = new();
    [SerializeField] private SpriteRenderer driverCursor;
    [SerializeField] private SpriteRenderer[] horseTargets;
    private readonly int[] nextHorseRest = new int[2];
    public static string HorseZone(float x) => x>0.15f ? "엉덩이" : x>-.25f ? "허리" : x>-.65f ? "목" : "머리";
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
    public const int Departure = 8;
    public bool CanBuy(int choice)
    {
        var card = CartRules.Active.GetCard(choice);
        return Affection.Value >= card.price && (choice == 0 || CardLevel(choice) < card.maxLevel);
    }
    public static readonly string[] Reports = { "왕족 돌봄", "마차 피해", "포탄 보급" };
    public bool IsCarryingChild(NetworkPlayer p) => IsCarrier(Prince.Value,p) || IsCarrier(Princess.Value,p);
    private static bool IsCarrier(RiderState child, NetworkPlayer p) => child.Pose == RiderPose.Carried && child.CarrierSlot == p.Slot.Value;

    private void ResetCare()
    {
        careTicks = 0;
        randomState = (uint)(1987 + stageNumber);
        Array.Clear(seated,0,2);
        Array.Clear(crying,0,2);
        Array.Clear(attentionSum,0,2);
        for (int i=0;i<2;i++)
        {
            var value = new RiderState { Pose=RiderPose.Seated, Position=seats[i], Attention=100, CarrierSlot=-1 };
            if (i==0) Prince.Value=value; else Princess.Value=value;
            ignoreWanderUntil[i]=210;
        }
        Array.Clear(nextHorseRest,0,2);
        RightWounds.Value = LeftWounds.Value = 0;
        Waves.Value = 0;
        Report.Value = -1;
        Votes.Clear();
        for (int i=0;i<4;i++) Votes.Add(-1);
    }

    private void AdvanceCare()
    {
        careTicks++;
        RestHorse(0,LeftWounds);RestHorse(1,RightWounds);
        AdvanceChild(Prince,0);
        AdvanceChild(Princess,1);
        foreach (var actor in gameScene.Players)
        {
            if (!actor.Assigned.Value) continue;
            if (actor.CookingHeld.Value && HoldsStation(StationAction.Kitchen,actor) && actor.HandsFree)
            {
                actor.CookProgress.Value = Mathf.Min(CartRules.Active.cookSeconds, actor.CookProgress.Value + Step);
                if (actor.CookProgress.Value >= CartRules.Active.cookSeconds)
                {
                    actor.Snack.Value = true;
                    actor.CookProgress.Value = 0;
                    CompleteStation(StationAction.Kitchen, actor);
                }
            }
            else if (actor.CookProgress.Value > 0) actor.CookProgress.Value=0;
        }
    }

    private void AdvanceChild(NetworkVariable<RiderState> field, int index)
    {
        var child=field.Value;
        child.Attention=Mathf.Max(0,child.Attention-Step*1.8f);
        child.Hunger=Mathf.Min(100,child.Hunger+Step*1.35f);
        if (child.Hunger >= 70 && careTicks%90 == 0) child.Crying=true;
        if (child.Pose == RiderPose.Seated && child.Attention<=20 && careTicks>=ignoreWanderUntil[index]) child.Pose=RiderPose.Wandering;
        if (child.Pose == RiderPose.Wandering)
        {
            float phase = careTicks*Step*0.65f+index*2;
            child.Position=Vector2.MoveTowards(child.Position,new Vector2(Mathf.Sin(phase)*2.8f,Mathf.Cos(phase)*1.3f),Step);
            if ((careTicks+index*95)%180 == 0)
            {
                switch((int)(Sample()*3))
                {
                    case 0: if(depositedShells>0) { depositedShells--; lostShells++; } break;
                    case 1: if(loaded && !fuseLit) { gunner=null; StartFuse(); } break;
                    case 2: cart.BoostHorse(index==0); break;
                }
            }
        }
        if (child.Pose == RiderPose.Carried)
        {
            var carrier = gameScene.Players[child.CarrierSlot];
            if (carrier.Assigned.Value) child.Position=(Vector2)carrier.transform.localPosition + Vector2.up*0.35f;
            else { child.Pose=RiderPose.Fallen; child.CarrierSlot=-1; }
        }
        if (child.Pose == RiderPose.Seated) seated[index]++;
        if (child.Crying) crying[index]++;
        attentionSum[index]+=child.Attention;
        field.Value=child;
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !Running || royalVisuals == null || royalVisuals.Length != 2) return;
        ShowDriverAim();
        PlaceChild(Prince.Value,0);
        PlaceChild(Princess.Value,1);
    }
    private void PlaceChild(RiderState child,int index)
    {
        var visual=royalVisuals[index];
        visual.position=cart.transform.TransformPoint(child.Position);
        var target = child.Pose == RiderPose.Carried ? seats[index] : child.Position;
        royalAreas[index].position=cart.transform.TransformPoint(target);
    }

    public CareAction AvailableCare(StationAction action, NetworkPlayer actor)
    {
        var child = action == StationAction.Princess ? Princess.Value : Prince.Value;
        if (child.Pose == RiderPose.Carried && !IsCarrier(child, actor)) return CareAction.None;
        if (child.Crying) return CareAction.Soothe;
        if (IsCarrier(child, actor)) return CareAction.Reseat;
        if (child.Pose == RiderPose.Seated) return actor.Snack.Value ? CareAction.Feed : CareAction.Attend;
        return actor.HandsFree ? CareAction.PickUp : CareAction.None;
    }

    private void CareFor(NetworkVariable<RiderState> field, NetworkPlayer actor, int index)
    {
        var child=field.Value;
        var station = index == 0 ? StationAction.RoyalFamily : StationAction.Princess;
        switch (AvailableCare(station, actor))
        {
            case CareAction.Soothe:
                child.Crying = false;
                break;
            case CareAction.Reseat:
                child.Pose = RiderPose.Seated;
                child.Position = seats[index];
                child.CarrierSlot = -1;
                child.Attention = Mathf.Min(100, child.Attention + 45);
                ignoreWanderUntil[index] = careTicks + 210;
                break;
            case CareAction.Feed:
                child.Hunger = Mathf.Max(0, child.Hunger - 55);
                actor.Snack.Value = false;
                break;
            case CareAction.PickUp:
                child.Pose = RiderPose.Carried;
                child.CarrierSlot = actor.Slot.Value;
                break;
            case CareAction.Attend:
                child.Attention = Mathf.Min(100, child.Attention + 28);
                break;
            default:
                return;
        }
        field.Value=child;
    }

    private void ShockCare(float strength)
    {
        if(randomState==0) return;
        foreach(var player in gameScene.Players)
            if(player.CookProgress.Value>0) player.CookProgress.Value=Mathf.Max(0,player.CookProgress.Value-strength*7);
        float chance = 0.25f * strength / Mathf.Max(0.01f,recoilShakeStrength);
        KnockDown(Prince,chance);
        KnockDown(Princess,chance);
    }
    private void KnockDown(NetworkVariable<RiderState> field,float chance)
    {
        var child=field.Value;
        if(child.Pose!=RiderPose.Seated || Sample()>=chance) return;
        child.Pose=RiderPose.Fallen;
        child.Position+=Vector2.down*0.65f;
        child.Crying=Sample()<0.85;
        field.Value=child;
    }

    private void ShowDriverAim()
    {
        if(driverCursor==null || horseTargets==null || horseTargets.Length!=2) return;
        driverCursor.enabled=false;
        foreach(var p in gameScene.Players)
        {
            if(!HoldsStation(StationAction.Driver,p)) continue;
            var aim=p.DriverAim.Value;var bounds=horseTargets[aim.y>=0?1:0].bounds;
            driverCursor.transform.position=bounds.center + Vector3.right*(bounds.extents.x*aim.x);
            driverCursor.color=aim.x>0.15f ? Color.green : Color.red;driverCursor.enabled=true;break;
        }
    }
    private void RestHorse(int i,NetworkVariable<int> wounds)
    {
        if(wounds.Value>0 && careTicks>=nextHorseRest[i]) { wounds.Value--;nextHorseRest[i]=careTicks+24; }
    }
    private void StrikeHorse(NetworkPlayer actor)
    {
        Vector2 aim=actor.DriverAim.Value;
        bool right=aim.y>=0;
        var wounds=right?RightWounds:LeftWounds;
        int side=right?1:0;
        if(wounds.Value>=8 && careTicks<nextHorseRest[side]) return;
        nextHorseRest[side]=careTicks+90;
        if(aim.x<=0.15f) {cart.SpookHorse(right);return;}
        wounds.Value=Mathf.Min(8,wounds.Value+1);
        if(wounds.Value==8) cart.SpookHorse(right);
        else cart.BoostHorse(right,wounds.Value>=5?1f/3f:1f);
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
        int score=Mathf.RoundToInt((seated[i]*60f/n+(float)(attentionSum[i]/n)*0.4f)/10f);
        int penalty=crying[i]==0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(4f*crying[i]/n),1,4);
        return Mathf.Max(0,score-penalty);
    }
    public void ReceiveChoice(NetworkPlayer actor,int choice,int round,int stage)
    {
        if(!IsServer || !ChoosingUpgrade || !actor.Assigned.Value || round!=ShopRound.Value || stage!=stageNumber) return;
        if(Report.Value<0)
        {
            if(actor.OwnerClientId!=NetworkManager.ServerClientId || choice<0 || choice>=Reports.Length) return;
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
        foreach(var p in gameScene.Players) if(p.Assigned.Value) humans++;
        if(humans==0) return;
        for(int choice=0;choice<=Departure;choice++)
        {
            int votes=0;
            foreach(var p in gameScene.Players) if(p.Assigned.Value && Votes[p.Slot.Value]==choice) votes++;
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


using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct SeatCheckpoint
{
    public int Slot;
    public int CompletedStationKey; // Zero also means no completed station in older saves.
    public Vector3 Position;
    public Vector2 Aim;
    public bool FacingRight;
    public int Score, ScoreOwnerVersion;
    public bool Shell, Wheel, Snack, Holding;
    public float HoldRemaining, Cooking;
    public float ShellSafetyRemaining;
}
[Serializable]
public struct PursuerCheckpoint
{
    public string Unit;
    public Vector2 KnockVelocity;
    public float KnockLeft, Angle;
    public Vector3 Position;
    public float Health, Speed, Damage, Wait, SinceStrike, EngagementAge;
    public float StrikeAge, StartledRemaining;
    public Vector2 StrikeTarget;
    public int AttackSlotKey;
}
[Serializable]
public struct ShotCheckpoint
{
    public Vector3 Position, Velocity;
    public Quaternion Rotation;
    public float Remaining, Speed, ImpactAge, ScrollAtImpact;
    public Vector2 Landing;
    public bool Landed;
    public int Caliber, ShooterSlotKey, ShooterOwnerVersion;
    public int[] StartledEnemies;
    public int[] NearMissEnemies;
    public int[] HitEnemies;
}
[Serializable]
public sealed class RunCheckpoint
{
    public CartMotionState Motion;
    public CartNetworkState Cart;
    public string Failure, StageEntry, KingReply;
    public LooseWheelState[] LooseWheels;
    public int[] StationSlots;
    public bool SharedCannon;
    public float CartY, WaveTime, FuseRemaining;
    public int PostShotRemaining;
    public int MountSlotKey;
    public float MountAge, ShotVisualAge, ShotVisualAngle;
    public bool HasShotVisual;
    public float ShockStrength, ShockPhase;
    public int Wave, CareTicks, GunnerSlot, AP, Report, Round, Barrel, Stabilizer, Porter, Hull, PrincePoints, PrincessPoints;
    public uint RandomState;
    public RiderState Prince, Princess;
    public HorseRageState HorseRage;
    public int[] Seated, Crying, Grace, Votes;
    public double[] Attention;
    public SeatCheckpoint[] Seats;
    public string[] SeatKeys;
    public PursuerCheckpoint[] Enemies;
    public ShotCheckpoint[] Shots;
}

public sealed partial class CartStations
{
    public RunCheckpoint Capture(string[] keys)
    {
        Publish();
        var enemies=Enemy.Active.ToArray();
        double now = NetworkManager.ServerTime.Time;
        float shock = WorkShakeAt(now, out float phase);
        return new RunCheckpoint {
            StageEntry=stageEntryJson, KingReply=KingReply.Value.ToString(),
            ShockStrength=shock, ShockPhase=phase,
            LooseWheels=Enumerable.Range(0,LooseWheels.Count).Select(i=>LooseWheels[i]).ToArray(),
            SharedCannon=true,
            MountSlotKey=WheelMount.Value.SlotKey, MountAge=(float)(now-WheelMount.Value.Time),
            HasShotVisual=LastCannonShot.Value.Time >= 0d,
            ShotVisualAge=(float)(now-LastCannonShot.Value.Time), ShotVisualAngle=LastCannonShot.Value.Angle,
            StationSlots=Enumerable.Range(0,StationOwners.Count).Select(i=>Array.FindIndex(gameScene.Players,p=>p.NetworkObjectId==StationOwners[i])).ToArray(),
            Cart=State.Value, Failure=FailureReason, Motion=cart.Capture(), CartY=cart.transform.position.y,
            WaveTime=waveSpawner.Elapsed, Wave=Waves.Value, FuseRemaining=fuseRemaining, PostShotRemaining=postShotRemaining,
            GunnerSlot=gunner==null?-1:gunner.Slot.Value,
            AP=Affection.Value, Report=Report.Value, Round=ShopRound.Value, Barrel=Barrel.Value, Stabilizer=Stabilizer.Value,
            Porter=Porter.Value, Hull=Hull.Value,
            HorseRage=HorseRage.Value,
            Prince=Prince.Value, Princess=Princess.Value, PrincePoints=PrincePoints.Value, PrincessPoints=PrincessPoints.Value,
            CareTicks=careTicks, RandomState=randomState, Seated=seated, Crying=crying, Grace=ignoreWanderUntil, Attention=attentionSum,
            Votes=Enumerable.Range(0,4).Select(i=>Votes[i]).ToArray(), SeatKeys=keys, Seats=gameScene.Players.Select(p=>new SeatCheckpoint {
                Slot=p.Slot.Value,Position=p.transform.localPosition,Aim=p.Aim.Value,Score=p.Score.Value,ScoreOwnerVersion=p.ScoreOwnerVersion,FacingRight=p.FacingRight.Value,Shell=p.Carrying.Value,Wheel=p.SpareWheel.Value,Snack=p.Snack.Value,
                Holding=p.Holding.Value,HoldRemaining=(float)Math.Max(0,p.HoldDeadline.Value-now),Cooking=p.CookProgress.Value,
                ShellSafetyRemaining=(float)Math.Max(0,p.ShellSafeUntil-now),CompletedStationKey=p.CompletedStation.Value+1
            }).ToArray(), Enemies=enemies.Select(e=>e.Capture()).ToArray(),
            Shots=FindObjectsByType<Projectile>(FindObjectsSortMode.None).Select(p=>p.Capture(enemies)).ToArray()
        };
    }

    public void Restore(RunCheckpoint data)
    {
        StopAllCoroutines();
        ResetGunner();
        double now = NetworkManager.ServerTime.Time;
        WheelMount.Value = new WheelMountVisual { SlotKey = data.MountSlotKey, Time = now - data.MountAge };
        LastCannonShot.Value = new CannonShotVisual { Time = data.HasShotVisual ? now - data.ShotVisualAge : -1d, Angle = data.ShotVisualAngle };
        pendingCookingShock = 0f;
        workShock.Value = new CartWorkShock { Time = now, Strength = data.ShockStrength, Phase = data.ShockPhase };
        cart.Restore(data.Motion,data.Cart.Horse);
        cart.transform.position=new Vector3(cart.transform.position.x,data.CartY,cart.transform.position.z);
        Apply(data.Cart); FailureReason=data.Failure;
        stageEntryJson = string.IsNullOrEmpty(data.StageEntry) ? JsonUtility.ToJson(data) : data.StageEntry;
        KingReply.Value = new Unity.Collections.FixedString512Bytes(data.KingReply ?? string.Empty);
        LooseWheels.Clear();
        if(data.LooseWheels!=null) foreach(var wheel in data.LooseWheels) LooseWheels.Add(wheel);
        ClearStations();
        int legacyCannonSlot = -1;
        if(data.StationSlots!=null) for(int i=0;i<Math.Min(StationOwners.Count,data.StationSlots.Length);i++)
        {
            int slot = data.StationSlots[i];
            if (slot < 0 || slot >= gameScene.Players.Length) continue;
            var action = (StationAction)i;
            if (TimingArea.IsCannon(action) && !data.SharedCannon)
            {
                if (legacyCannonSlot < 0 || action == StationAction.Cannon) legacyCannonSlot = slot;
            }
            else StationOwners[i] = gameScene.Players[slot].NetworkObjectId;
        }
        if (legacyCannonSlot >= 0)
            StationOwners[(int)StationAction.Cannon] = gameScene.Players[legacyCannonSlot].NetworkObjectId;
        Affection.Value=data.AP;Report.Value=data.Report;ShopRound.Value=data.Round;
        Barrel.Value=data.Barrel;Stabilizer.Value=data.Stabilizer;Porter.Value=data.Porter;Hull.Value=data.Hull;
        HorseRage.Value=data.HorseRage;
        Prince.Value=data.Prince;Princess.Value=data.Princess;PrincePoints.Value=data.PrincePoints;PrincessPoints.Value=data.PrincessPoints;
        careTicks=data.CareTicks;randomState=data.RandomState;
        data.Seated.CopyTo(seated,0);data.Crying.CopyTo(crying,0);data.Attention.CopyTo(attentionSum,0);data.Grace.CopyTo(ignoreWanderUntil,0);
        for(int i=0;i<4;i++)
        {
            var p=gameScene.Players[i];var saved=data.Seats[i];p.Slot.Value=saved.Slot;
            if (i == legacyCannonSlot && p.Timing.AreaFor(StationAction.Cannon) is RectTransform cannonArea)
                saved.Position = p.transform.parent != null ? p.transform.parent.InverseTransformPoint(cannonArea.position) : cannonArea.position;
            p.RestoreSeat(saved);
            if (p.ScoreOwnerVersion == saved.ScoreOwnerVersion) p.Score.Value = saved.Score;
            p.Carrying.Value=saved.Shell;p.SpareWheel.Value=saved.Wheel;p.Snack.Value=saved.Snack;
            p.Holding.Value=saved.Holding;p.HoldDeadline.Value=now+saved.HoldRemaining;p.CookProgress.Value=saved.Cooking;
            p.ShellSafeUntil=now+saved.ShellSafetyRemaining;
            p.CompletedStation.Value = saved.CompletedStationKey - 1;
            Votes[i]=data.Votes[i];
        }
        Waves.Value=data.Wave;
        var enemies=waveSpawner.Restore(stageNumber,data.Wave,data.WaveTime,data.Enemies);
        foreach(var saved in data.Shots)
        {
            var shot=Instantiate(shellPrefab,saved.Position,saved.Rotation).GetComponent<Projectile>();
            shot.SetCaliber(saved.Caliber);shot.NetworkObject.Spawn(true);shot.Restore(saved,enemies);
        }
        if (fuseLit || data.PostShotRemaining > 0)
        {
            gunner = data.GunnerSlot >= 0 && data.GunnerSlot < gameScene.Players.Length ? gameScene.Players[data.GunnerSlot] : null;
            Gunner.Value=gunner==null?NoStationOwner:gunner.NetworkObjectId;
            if (fuseLit) StartFuse(data.FuseRemaining);
            else if (gunner != null && HoldsStation(StationAction.Cannon, gunner)) StartPostShot(data.PostShotRemaining);
        }
        Refresh();
    }
}

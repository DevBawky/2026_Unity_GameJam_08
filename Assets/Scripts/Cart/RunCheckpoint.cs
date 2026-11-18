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
    public Vector2 Aim, DriverAim;
    public bool HasWhipAim;
    public bool Shell, Wheel, Snack, Holding;
    public float HoldRemaining, Cooking;
}
[Serializable]
public struct PursuerCheckpoint
{
    public string Unit;
    public Vector3 Position;
    public float Health, Speed, Damage, Wait, SinceStrike;
}
[Serializable]
public struct ShotCheckpoint
{
    public Vector3 Position, Velocity;
    public Quaternion Rotation;
    public float Remaining;
    public int Caliber;
    public int[] HitEnemies;
}
[Serializable]
public sealed class RunCheckpoint
{
    public CartMotionState Motion;
    public CartNetworkState Cart;
    public string Failure;
    public float CartY, WaveTime, FuseRemaining;
    public int Wave, CareTicks, GunnerSlot, AP, Report, Round, Barrel, Stabilizer, Porter, Hull, RightWounds, LeftWounds, PrincePoints, PrincessPoints;
    public uint RandomState;
    public RiderState Prince, Princess;
    public int[] Seated, Crying, Grace, Votes, HorseRest;
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
        var enemies=FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        return new RunCheckpoint {
            Cart=State.Value, Failure=FailureReason, Motion=cart.Capture(), CartY=cart.transform.position.y,
            WaveTime=waveSpawner.Elapsed, Wave=Waves.Value, FuseRemaining=fuseRemaining, GunnerSlot=gunner==null?-1:gunner.Slot.Value,
            AP=Affection.Value, Report=Report.Value, Round=ShopRound.Value, Barrel=Barrel.Value, Stabilizer=Stabilizer.Value,
            Porter=Porter.Value, Hull=Hull.Value, RightWounds=RightWounds.Value, LeftWounds=LeftWounds.Value,
            Prince=Prince.Value, Princess=Princess.Value, PrincePoints=PrincePoints.Value, PrincessPoints=PrincessPoints.Value,
            CareTicks=careTicks, RandomState=randomState, Seated=seated, Crying=crying, Grace=ignoreWanderUntil, HorseRest=nextHorseRest, Attention=attentionSum,
            Votes=Enumerable.Range(0,4).Select(i=>Votes[i]).ToArray(), SeatKeys=keys, Seats=gameScene.Players.Select(p=>new SeatCheckpoint {
                Slot=p.Slot.Value,Position=p.transform.localPosition,Aim=p.Aim.Value,DriverAim=p.DriverAim.Value,HasWhipAim=true,Shell=p.Carrying.Value,Wheel=p.SpareWheel.Value,Snack=p.Snack.Value,
                Holding=p.Holding.Value,HoldRemaining=(float)Math.Max(0,p.HoldDeadline.Value-NetworkManager.ServerTime.Time),Cooking=p.CookProgress.Value, CompletedStationKey=p.CompletedStation.Value+1
            }).ToArray(), Enemies=enemies.Select(e=>e.Capture()).ToArray(),
            Shots=FindObjectsByType<Projectile>(FindObjectsSortMode.None).Select(p=>p.Capture(enemies)).ToArray()
        };
    }

    public void Restore(RunCheckpoint data)
    {
        StopAllCoroutines();
        cart.Restore(data.Motion,data.Cart.Horse);
        cart.transform.position=new Vector3(cart.transform.position.x,data.CartY,cart.transform.position.z);
        Apply(data.Cart); FailureReason=data.Failure;
        if(detachedTire!=null) detachedTireAlpha=tireAlphas[tires.IndexOf(detachedTire)];
        Affection.Value=data.AP;Report.Value=data.Report;ShopRound.Value=data.Round;
        Barrel.Value=data.Barrel;Stabilizer.Value=data.Stabilizer;Porter.Value=data.Porter;Hull.Value=data.Hull;
        RightWounds.Value=data.RightWounds;LeftWounds.Value=data.LeftWounds;
        Prince.Value=data.Prince;Princess.Value=data.Princess;PrincePoints.Value=data.PrincePoints;PrincessPoints.Value=data.PrincessPoints;
        careTicks=data.CareTicks;randomState=data.RandomState;
        data.HorseRest?.CopyTo(nextHorseRest, 0);
        data.Seated.CopyTo(seated,0);data.Crying.CopyTo(crying,0);data.Attention.CopyTo(attentionSum,0);data.Grace.CopyTo(ignoreWanderUntil,0);
        for(int i=0;i<4;i++)
        {
            var p=gameScene.Players[i];var saved=data.Seats[i];p.Slot.Value=saved.Slot;
            if (p.IsOwner)
            {
                if (saved.Aim != Vector2.zero) p.Aim.Value = saved.Aim;
                if (saved.HasWhipAim || saved.DriverAim != Vector2.zero) p.DriverAim.Value = saved.DriverAim;
            }
            p.transform.localPosition=saved.Position;p.Carrying.Value=saved.Shell;p.SpareWheel.Value=saved.Wheel;p.Snack.Value=saved.Snack;
            p.Holding.Value=saved.Holding;p.HoldDeadline.Value=NetworkManager.ServerTime.Time+saved.HoldRemaining;p.CookProgress.Value=saved.Cooking;
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
        if(fuseLit)
        {
            gunner=data.GunnerSlot<0?null:gameScene.Players[data.GunnerSlot];
            Gunner.Value=gunner==null?NoStationOwner:gunner.NetworkObjectId;
            StationOwners[(int)StationAction.Cannon]=Gunner.Value;
            StartFuse(data.FuseRemaining);
        }
        Refresh();
    }
}

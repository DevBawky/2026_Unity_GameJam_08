using Unity.Netcode;
using UnityEngine;

// 작업대 점유(예약·해제·정리)와 작업 결과(포탄 운반·장전·바퀴 수리·포탄 버티기)를 처리한다.
public sealed partial class CartStations
{
    public void Enter(StationAction action, NetworkPlayer actor)
    {
        if (!IsServer || action != StationAction.ShellDeposit || !actor.Carrying.Value) return;
        actor.Carrying.Value = false;
        depositedShells++;
        CompleteStation(action, actor);
        Refresh();
    }

    public void Succeed(StationAction action, NetworkPlayer actor)
    {
        if (!IsServer) return;
        switch (action)
        {
            case StationAction.RoyalFamily: CareFor(Prince, actor, 0); break;
            case StationAction.Princess: CareFor(Princess, actor, 1); break;
            case StationAction.WheelRack:
                actor.SpareWheel.Value = true;
                CompleteStation(action, actor);
                break;
            case StationAction.ShellPile:
                if (actor.Carrying.Value || shellsInPile <= 0) break;
                shellsInPile--;
                actor.Carrying.Value = true;
                CompleteStation(action, actor);
                break;
            case StationAction.CannonCleaning:
                if (!NeedsCleaning) break;
                if (++cleaningTaps == CleaningTapsRequired && IsPostShotGunner(actor)) EndPostShot();
                break;
            case StationAction.Reload:
                if (!CanReload) break;
                depositedShells--;
                loaded = true;
                actor.ShowWorkResult(true);
                break;
            case StationAction.WheelRepair:
                if (!wheelDetached || (wheelRepairSteps == 0 && !actor.SpareWheel.Value)) break;
                if (wheelRepairSteps == 0) actor.SpareWheel.Value = false;
                wheelRepairSteps++;
                if (wheelRepairSteps == 1)
                {
                    WheelMount.Value = new WheelMountVisual { SlotKey = tires.IndexOf(detachedTire) + 1, Time = NetworkManager.ServerTime.Time };
                    SetTireAlpha(detachedTireAlpha);
                }
                if (wheelRepairSteps >= WheelRepairStepsNeeded)
                {
                    CompleteStation(action, actor);
                    RepairWheel();
                }
                break;
        }
        Refresh();
    }

    public bool CanUse(StationAction action, NetworkPlayer actor)
    {
        if (TimingArea.IsRoyal(action)) return AvailableCare(action, actor) != CareAction.None;
        return HoldsStation(action, actor) && HasWork(action, actor);
    }

    public bool HasWork(StationAction action, NetworkPlayer actor)
    {
        switch (action)
        {
            case StationAction.ShellPile: return actor.HandsFree && shellsInPile > 0;
            case StationAction.Kitchen: return actor.HandsFree;
            case StationAction.WheelRack: return wheelDetached && wheelRepairSteps == 0 && actor.HandsFree;
            case StationAction.CannonCleaning: return NeedsCleaning && actor.HandsFree;
            case StationAction.Reload: return CanReload && actor.HandsFree;
            case StationAction.Cannon: return loaded && !fuseLit && actor.HandsFree;
            case StationAction.ShellDeposit: return actor.Carrying.Value;
            case StationAction.WheelRepair: return wheelDetached && (wheelRepairSteps > 0 || actor.SpareWheel.Value);
            default: return true;
        }
    }

    public bool HoldsStation(StationAction action, NetworkPlayer actor)
    {
        int index = TimingArea.IsCannon(action) ? (int)StationAction.Cannon : (int)action;
        return actor != null && actor.Assigned.Value && index >= 0 && index < StationOwners.Count
            && StationOwners[index] == actor.NetworkObjectId;
    }

    private void CompleteStation(StationAction action, NetworkPlayer actor)
    {
        if (!HoldsStation(action, actor)) return;
        if (actor.Timing.IsInActionArea(action)) actor.CompletedStation.Value = (int)action;
        StationOwners[(int)action] = NoStationOwner;
    }

    public void ReserveStation(NetworkPlayer actor, int requested)
    {
        if (!IsServer || !actor.Assigned.Value) return;
        if (requested >= 0 && TimingArea.IsCannon((StationAction)requested)) requested = (int)StationAction.Cannon;
        ReleaseStations(actor.NetworkObjectId, requested);
        if (!Running || actor.Holding.Value || requested < 0 || requested >= StationCount) return;
        var action = (StationAction)requested;
        if (action == StationAction.Stairs || actor.CompletedStation.Value == requested || TimingArea.IsRoyal(action)
            || !(action == StationAction.Cannon ? actor.HandsFree : HasWork(action, actor)) || !actor.Timing.IsInActionArea(action)) return;
        if (!actor.Bot.Value && action != StationAction.Cannon && StationOwners[requested] != NoStationOwner)
        {
            foreach (var other in gameScene.Players)
                if (other.Bot.Value && StationOwners[requested] == other.NetworkObjectId)
                    ReleaseStations(other.NetworkObjectId);
        }
        if (StationOwners[requested] == NoStationOwner) StationOwners[requested] = actor.NetworkObjectId;
    }

    public void ReleaseStations(ulong client, int keep = -1)
    {
        if (!IsServer) return;
        for (int i = 0; i < StationOwners.Count; i++)
        {
            if (StationOwners[i] != client || i == keep) continue;
            if (i == (int)StationAction.Cannon && postShotRemaining > 0 && Gunner.Value == client) EndPostShot();
            StationOwners[i] = NoStationOwner;
        }
    }

    private void ReconcileStations()
    {
        for (int i = 0; i < StationOwners.Count; i++)
        {
            ulong owner = StationOwners[i];
            if (owner == NoStationOwner) continue;
            bool valid = false;
            if (Running)
                foreach (var actor in gameScene.Players)
                    if (actor.Assigned.Value && actor.NetworkObjectId == owner && !actor.Holding.Value
                        && (i == (int)StationAction.Cannon ? actor.HandsFree : HasWork((StationAction)i, actor))
                        && actor.Timing.IsInActionArea((StationAction)i)) { valid = true; break; }
            if (!valid)
            {
                if (i == (int)StationAction.Cannon && postShotRemaining > 0 && Gunner.Value == owner) EndPostShot();
                StationOwners[i] = NoStationOwner;
            }
        }
    }

    private void ClearStations()
    {
        for (int i = 0; i < StationOwners.Count; i++) StationOwners[i] = NoStationOwner;
    }

    public void Fail(StationAction action, NetworkPlayer actor)
    {
        if (!IsServer || action != StationAction.Reload || !CanReload) return;
        depositedShells--;
        lostShells++;
        actor.ShowWorkResult(false);
        Refresh();
    }

    private void StartShellCatch(double time)
    {
        foreach (var actor in gameScene.Players)
        {
            if (!actor.Assigned.Value || !actor.Carrying.Value || actor.Holding.Value || actor.ShellSafeUntil > time) continue;
            actor.HoldDeadline.Value = time + CartRules.Active.shellHoldSeconds;
            actor.Holding.Value = true;
        }
    }

    public void ResolveShellHold(NetworkPlayer actor, bool success)
    {
        if (!IsServer || !actor.Holding.Value) return;
        actor.Holding.Value = false;
        if (success) actor.ShellSafeUntil = NetworkManager.ServerTime.Time + 1d;
        else { actor.Carrying.Value = false; lostShells++; }
        Refresh();
    }
}

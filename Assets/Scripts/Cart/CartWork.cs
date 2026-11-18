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
            case StationAction.Reload:
                if (depositedShells <= 0 || loaded || fuseLit) break;
                depositedShells--;
                loaded = true;
                CompleteStation(action, actor);
                actor.ShowWorkResult(true);
                break;
            case StationAction.WheelRepair:
                if (!wheelDetached || (wheelRepairSteps == 0 && !actor.SpareWheel.Value)) break;
                if (wheelRepairSteps == 0) actor.SpareWheel.Value = false;
                wheelRepairSteps++;
                if (wheelRepairSteps == 1) SetTireAlpha(detachedTireAlpha);
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
            case StationAction.Reload: return depositedShells > 0 && !loaded && !fuseLit;
            case StationAction.Cannon: return loaded && !fuseLit;
            case StationAction.ShellDeposit: return actor.Carrying.Value;
            case StationAction.WheelRepair: return wheelDetached && (wheelRepairSteps > 0 || actor.SpareWheel.Value);
            default: return true;
        }
    }

    public bool HoldsStation(StationAction action, NetworkPlayer actor)
    {
        int index = (int)action;
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
        ReleaseStations(actor.NetworkObjectId, requested);
        if (!Running || actor.Holding.Value || requested < 0 || requested >= StationCount) return;
        var action = (StationAction)requested;
        if (actor.CompletedStation.Value == requested || TimingArea.IsRoyal(action) || !HasWork(action, actor) || !actor.Timing.IsInActionArea(action)) return;
        if (!actor.Bot.Value && StationOwners[requested] != NoStationOwner &&
            !(action == StationAction.Cannon && fuseLit))
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
            // A lit cannon belongs to its gunner until the shot finishes, even after departure.
            if (i == (int)StationAction.Cannon && fuseLit) continue;
            StationOwners[i] = NoStationOwner;
        }
    }

    private void ReconcileStations()
    {
        for (int i = 0; i < StationOwners.Count; i++)
        {
            ulong owner = StationOwners[i];
            if (owner == NoStationOwner || (i == (int)StationAction.Cannon && fuseLit)) continue;
            bool valid = false;
            if (Running)
                foreach (var actor in gameScene.Players)
                    if (actor.Assigned.Value && actor.NetworkObjectId == owner && !actor.Holding.Value
                        && HasWork((StationAction)i, actor) && actor.Timing.IsInActionArea((StationAction)i)) { valid = true; break; }
            if (!valid) StationOwners[i] = NoStationOwner;
        }
    }

    private void ClearStations()
    {
        for (int i = 0; i < StationOwners.Count; i++) StationOwners[i] = NoStationOwner;
    }

    public void Fail(StationAction action, NetworkPlayer actor)
    {
        if (!IsServer || action != StationAction.Reload || depositedShells <= 0 || loaded || fuseLit) return;
        depositedShells--;
        lostShells++;
        CompleteStation(action, actor);
        actor.ShowWorkResult(false);
        Refresh();
    }

    // 큰 충격(발사 반동, 장애물·몬스터 충돌) 때 포탄을 든 플레이어에게 버티기 판정을 건다.
    public void ApplyImpact()
    {
        if (!IsServer) return;
        foreach (var actor in gameScene.Players)
        {
            if (!actor.Assigned.Value || !actor.Carrying.Value) continue;
            actor.Holding.Value = true;
            actor.HoldDeadline.Value = NetworkManager.ServerTime.Time + CartRules.Active.shellHoldSeconds;
        }
    }

    public void ResolveShellHold(NetworkPlayer actor, bool success)
    {
        if (!IsServer || !actor.Holding.Value) return;
        actor.Holding.Value = false;
        if (!success) { actor.Carrying.Value = false; lostShells++; }
        Refresh();
    }
}

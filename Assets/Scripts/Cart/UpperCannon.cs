using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct UpperCannonState : INetworkSerializeByMemcpy, IEquatable<UpperCannonState>
{
    public bool Loaded;
    public int Cleaning;
    public double Fuse, PostShot;
    public ulong Gunner;
    public CannonShotVisual Shot;

    public readonly bool Equals(UpperCannonState other) =>
        Loaded == other.Loaded && Cleaning == other.Cleaning && Fuse == other.Fuse &&
        PostShot == other.PostShot && Gunner == other.Gunner && Shot.Equals(other.Shot);
}

public sealed partial class CartStations
{
    [SerializeField] private Transform upperCannonPivot;
    private readonly NetworkVariable<UpperCannonState> upperCannon = new();
    private NetworkPlayer upperGunner;

    public StationAction CannonActionAt(bool upper) => !upper ? CannonAction :
        CleaningAt(true) < CleaningTapsRequired ? StationAction.UpperCleaning :
        upperCannon.Value.Loaded ? StationAction.UpperCannon : StationAction.UpperReload;
    internal int CleaningAt(bool upper) => upper ? upperCannon.Value.Cleaning : CleaningTaps;
    internal Transform PivotAt(bool upper) => upper ? upperCannonPivot : CannonPivot;
    internal double FuseAt(bool upper) => upper ? upperCannon.Value.Fuse : FuseDeadline.Value;
    internal bool FiringAt(bool upper) => upper ? upperCannon.Value.Fuse > 0d : IsFiring;
    internal CannonShotVisual ShotAt(bool upper) => upper ? upperCannon.Value.Shot : LastCannonShot.Value;
    internal double PostShotAt(NetworkPlayer actor) => upperCannon.Value.Gunner == actor.NetworkObjectId && upperCannon.Value.PostShot > 0d
        ? upperCannon.Value.PostShot : PostShotEndsAt.Value;
    internal NetworkPlayer OperatorAt(bool upper)
    {
        if (!upper) return CannonOperator;
        foreach (var actor in gameScene.Players)
            if (HoldsStation(StationAction.UpperCannon, actor)) return actor;
        return null;
    }

    private bool CanReloadUpper => upperCannonPivot != null && CannonActionAt(true) == StationAction.UpperReload && depositedShells > 0;

    private void ResetUpperCannon()
    {
        upperGunner = null;
        upperCannon.Value = new UpperCannonState { Gunner = NoStationOwner, Shot = new CannonShotVisual { Time = -1d } };
    }

    private UpperCannonState CaptureUpperCannon(double now)
    {
        var state = upperCannon.Value;
        state.Fuse = state.Fuse > 0d ? Math.Max(.001d, state.Fuse - now) : 0d;
        state.PostShot = state.PostShot > 0d ? Math.Max(0d, state.PostShot - now) : 0d;
        state.Shot = new CannonShotVisual { Time = -1d };
        return state;
    }

    private void RestoreUpperCannon(UpperCannonState state, int slotKey, double now)
    {
        upperGunner = slotKey > 0 && slotKey <= gameScene.Players.Length ? gameScene.Players[slotKey - 1] : null;
        state.Gunner = upperGunner != null ? upperGunner.NetworkObjectId : NoStationOwner;
        if (state.Fuse > 0d) state.Fuse += now;
        if (state.PostShot > 0d) state.PostShot += now;
        state.Shot = new CannonShotVisual { Time = -1d };
        upperCannon.Value = state;
    }

    private void SucceedUpper(StationAction action, NetworkPlayer actor)
    {
        var state = upperCannon.Value;
        if (action == StationAction.UpperCleaning && state.Cleaning < CleaningTapsRequired)
        {
            state.Cleaning = Mathf.Min(CleaningTapsRequired, state.Cleaning + CleaningTapGain(actor));
            actor.AwardScore(CartRules.CannonCleanScore);
            actor.PlayWorkCue(state.Cleaning == CleaningTapsRequired ? GameSfx.UiConfirm : GameSfx.BrushScrub);
            if (state.Cleaning == CleaningTapsRequired) state.PostShot = 0d;
        }
        else if (action == StationAction.UpperReload && CanReloadUpper)
        {
            depositedShells--;
            state.Loaded = true;
            actor.AwardScore(CartRules.ShellReloadScore);
            actor.ShowWorkResult(true);
            actor.PlayWorkCue(GameSfx.CannonLoad);
        }
        upperCannon.Value = state;
        Refresh();
    }

    private void IgniteUpper(NetworkPlayer actor)
    {
        if (!IsServer || !Running || upperCannonPivot == null || shellPrefab == null || !CanUse(StationAction.UpperCannon, actor)) return;
        upperGunner = actor;
        var state = upperCannon.Value;
        state.Gunner = actor.NetworkObjectId;
        state.PostShot = 0d;
        state.Fuse = NetworkManager.ServerTime.Time + FuseDuration;
        upperCannon.Value = state;
        PlayCartCueRpc(GameSfx.FuseBurn);
    }

    private void AdvanceUpperFuse()
    {
        if (upperCannonPivot == null) return;
        var state = upperCannon.Value;
        double now = NetworkManager.ServerTime.Time;
        if (state.PostShot > 0d && (now >= state.PostShot || !HoldsStation(StationAction.UpperCannon, upperGunner)))
        {
            state.PostShot = 0d;
            state.Gunner = NoStationOwner;
            upperGunner = null;
        }
        if (state.Fuse > 0d && now >= state.Fuse)
        {
            state.Shot = FireCannon(upperGunner, upperCannonPivot, now, careTicks + 1, true);
            state.Loaded = false;
            state.Cleaning = 0;
            state.Fuse = 0d;
            if (HoldsStation(StationAction.UpperCannon, upperGunner))
                state.PostShot = now + (Owns(ShopPerk.ReleaseLever) ? ReleaseLeverTicks : PostShotTicks) * (double)Step;
        }
        upperCannon.Value = state;
    }
}

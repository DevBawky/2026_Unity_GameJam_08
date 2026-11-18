using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct PlayerTaskState : INetworkSerializeByMemcpy, IEquatable<PlayerTaskState>
{
    public int Action;
    public bool Visible;
    public float Target, Width, Angle, Speed;
    public double Time;
    public bool Equals(PlayerTaskState v) => Action==v.Action && Visible==v.Visible && Target==v.Target && Width==v.Width && Angle==v.Angle && Speed==v.Speed && Time==v.Time;
}

// Scene-authored player: owner reads input, host validates shared outcomes.
[RequireComponent(typeof(NetworkObject))]
public sealed class NetworkPlayer : NetworkBehaviour
{
    public NetworkVariable<bool> Assigned = new(false);
    public NetworkVariable<int> Slot = new(-1);
    public NetworkVariable<FixedString64Bytes> Nickname = new();
    public NetworkVariable<bool> Carrying = new(false);
    public NetworkVariable<bool> Holding = new(false);
    public NetworkVariable<double> HoldDeadline = new();
    public NetworkVariable<PlayerTaskState> TaskState = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> Aim = new(Vector2.right, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private GameObject carriedShell;
    [SerializeField] private GameObject namePanel;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private PlayerTiming timing;
    [SerializeField] private CannonAim cannon;
    private double lastActionTime = -1;
    private float nextRequestTime;
    private float nextReservationTime;
    private bool shown;
    private bool carryingShown;
    private FixedString64Bytes shownName;
    public bool Local => IsSpawned && Assigned.Value && IsOwner;
    public static NetworkPlayer LocalPlayer { get; private set; }
    public PlayerTiming Timing => timing;
    public CannonAim Cannon => cannon;

    public override void OnNetworkSpawn()
    {
        Assigned.OnValueChanged += AssignmentChanged;
        RefreshAssignment();
        body.enabled = false;
        namePanel.SetActive(false);
        carriedShell.SetActive(false);
    }
    public override void OnGainedOwnership() => RefreshAssignment();
    public override void OnLostOwnership() { if (LocalPlayer == this) LocalPlayer = null; }
    private void AssignmentChanged(bool before, bool after) => RefreshAssignment();
    private void RefreshAssignment()
    {
        if (Local) LocalPlayer = this;
        else if (LocalPlayer == this) LocalPlayer = null;
    }
    private void Update()
    {
        bool active = IsSpawned && Assigned.Value;
        if (shown != active) { shown = active; body.enabled = active; namePanel.SetActive(active); }
        bool carrying = active && Carrying.Value;
        if (carryingShown != carrying) { carryingShown = carrying; carriedShell.SetActive(carrying); }
        if (!active) return;
        if (shownName != Nickname.Value) { shownName = Nickname.Value; nameLabel.text = shownName.ToString(); }
        if (IsServer && CartStations.Running && Holding.Value && NetworkManager.ServerTime.Time >= HoldDeadline.Value)
            CartStations.Instance.ResolveShellHold(this, false);
    }
    public void Assign(ulong client, int slot, FixedString64Bytes nickname)
    {
        if (!IsServer) return;
        Slot.Value = slot;
        Nickname.Value = nickname;
        NetworkObject.ChangeOwnership(client);
        Assigned.Value = true;
        RefreshAssignment();
    }
    public void Release()
    {
        if (!IsServer) return;
        CartStations.Instance.ReleaseStations(OwnerClientId);
        Assigned.Value = false;
        Carrying.Value = Holding.Value = false;
        NetworkObject.RemoveOwnership();
    }
    public void RequestStation(int action)
    {
        if (!Local || Time.unscaledTime < nextReservationTime) return;
        nextReservationTime = Time.unscaledTime + 0.2f;
        ReserveRpc(action);
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ReserveRpc(int action)
    {
        if (CartStations.Instance != null) CartStations.Instance.ReserveStation(this, action);
    }
    public void Request(StationAction action, bool success, bool hold = false)
    {
        if (!Local || (!hold && Time.unscaledTime < nextRequestTime)) return;
        if (!hold) nextRequestTime = Time.unscaledTime + 0.12f;
        ActionRpc(action, success, hold);
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ActionRpc(StationAction action, bool success, bool hold)
    {
        var stations = CartStations.Instance;
        if (!Assigned.Value || stations == null || !CartStations.Running) return;
        if (hold) { if (Holding.Value) stations.ResolveShellHold(this, success); return; }
        if (Holding.Value) return;
        double now = NetworkManager.ServerTime.Time;
        if (now-lastActionTime < 0.12 || !timing.IsInActionArea(action) || !stations.CanUse(action,this)) return;
        lastActionTime = now;
        if (action == StationAction.ShellDeposit) stations.Enter(action,this);
        else if (action == StationAction.Cannon) stations.Ignite(this);
        else if (success) stations.Succeed(action,this);
        else stations.Fail(action);
    }
    public void PublishTask(int action, bool visible, float target, float width, float angle, float speed)
    {
        if (!Local) return;
        var v = TaskState.Value;
        if (v.Action==action && v.Visible==visible && v.Target==target && v.Width==width && v.Speed==speed) return;
        TaskState.Value = new PlayerTaskState { Action=action, Visible=visible, Target=target, Width=width, Angle=angle, Speed=speed, Time=NetworkManager.ServerTime.Time };
    }
    public override void OnNetworkDespawn()
    {
        Assigned.OnValueChanged -= AssignmentChanged;
        if (LocalPlayer == this) LocalPlayer = null;
    }
}

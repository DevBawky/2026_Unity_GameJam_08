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
    public NetworkVariable<bool> CookingHeld = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> DriverAim = new(Vector2.right, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> Emote = new();
    public NetworkVariable<double> EmoteTime = new(-10);
    public bool SecondLocal => Local && LobbySession.LocalSeats == 2 && Slot.Value == 1;
    public bool TouchHeld {get; set;}
    public NetworkVariable<bool> SpareWheel = new();
    public NetworkVariable<bool> Snack = new();
    // The completed station becomes available to this player again after stepping outside it.
    public NetworkVariable<int> CompletedStation = new(-1);
    public NetworkVariable<float> CookProgress = new();
    public bool InCellar => transform.localPosition.y > CellarOffset * 0.5f;
    public const float CellarOffset = 64f;
    public bool HandsFree => !Carrying.Value && !SpareWheel.Value && !Snack.Value && !CartStations.Instance.IsCarryingChild(this);
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
    private int shownEmote;
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
        if (Local && !SecondLocal) LocalPlayer = this;
        else if (LocalPlayer == this) LocalPlayer = null;
    }
    private void Update()
    {
        bool active = IsSpawned && Assigned.Value;
        if (shown != active) { shown = active; body.enabled = active; namePanel.SetActive(active); }
        bool carrying = active && Carrying.Value;
        if (carryingShown != carrying) { carryingShown = carrying; carriedShell.SetActive(carrying); }
        if (!active) return;
        if (IsServer && CompletedStation.Value >= 0 && !timing.IsInActionArea((StationAction)CompletedStation.Value))
            CompletedStation.Value = -1;
        if (Local)
        {
            var keys = UnityEngine.InputSystem.Keyboard.current;
            bool pressed = SecondLocal ? keys?.enterKey.isPressed == true : keys?.spaceKey.isPressed == true || UnityEngine.InputSystem.Gamepad.current?.buttonSouth.isPressed == true || UnityEngine.InputSystem.Gamepad.current?.rightTrigger.isPressed == true;
            bool cooking = timing.IsAt(StationAction.Kitchen) && (pressed || TouchHeld);
            if (cooking != CookingHeld.Value) CookingHeld.Value = cooking;
        }
        bool showEmote=Emote.Value>0 && NetworkManager.ServerTime.Time-EmoteTime.Value<3;
        if (shownName != Nickname.Value || shownEmote != (showEmote ? Emote.Value : 0))
        {
            shownName=Nickname.Value; shownEmote=showEmote ? Emote.Value : 0;
            nameLabel.text=shownName.ToString()+(showEmote ? " · "+CrewControls.Messages[shownEmote-1] : "");
        }
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
        CartStations.Instance.ReleaseStations(NetworkObjectId);
        if (CartStations.Instance.Votes.Count > Slot.Value) CartStations.Instance.Votes[Slot.Value] = -1;
        Assigned.Value = false;
        CartStations.Instance.TallyChoices();
        Holding.Value = false;
        CookProgress.Value = 0;
        CompletedStation.Value = -1;
        if (IsSpawned && OwnerClientId != NetworkManager.ServerClientId) NetworkObject.RemoveOwnership();
    }
    public void SendEmote(int id) { if(Local) EmoteRpc(id); }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void EmoteRpc(int id)
    {
        if(!Assigned.Value || id<1 || id>7 || NetworkManager.ServerTime.Time-EmoteTime.Value<0.3) return;
        Emote.Value=id; EmoteTime.Value=NetworkManager.ServerTime.Time;
    }

    public void SubmitChoice(int choice)
    {
        var game = CartStations.Instance;
        if (Local && game != null) VoteRpc(choice, game.ShopRound.Value, game.StageNumber);
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void VoteRpc(int choice, int round, int stage) => CartStations.Instance.ReceiveChoice(this, choice, round, stage);

    public void RequestStation(int action)
    {
        if (!Local || (action >= 0 && Time.unscaledTime < nextReservationTime)) return;
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
        var stations = CartStations.Instance;
        var care = TimingArea.IsRoyal(action) ? stations.AvailableCare(action, this) : CareAction.None;
        ActionRpc(action, success, hold, stations.StageNumber, care);
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ActionRpc(StationAction action, bool success, bool hold, int stage, CareAction care)
    {
        var stations = CartStations.Instance;
        if (!Assigned.Value || stations == null || stage != stations.StageNumber || !CartStations.Running) return;
        if (hold) { if (Holding.Value) stations.ResolveShellHold(this, success); return; }
        if (Holding.Value) return;
        double now = NetworkManager.ServerTime.Time;
        if (now-lastActionTime < 0.12 || !timing.IsInActionArea(action) || !stations.CanUse(action,this)) return;
        if (TimingArea.IsRoyal(action) && care != stations.AvailableCare(action, this)) return;
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

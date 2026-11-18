using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

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
    public NetworkVariable<bool> Bot = new(false);
    public NetworkVariable<bool> Assigned = new(false);
    public NetworkVariable<int> Slot = new(-1);
    public NetworkVariable<FixedString64Bytes> Nickname = new();
    public NetworkVariable<bool> Carrying = new(false);
    public NetworkVariable<bool> CookingHeld = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> DriverAim = new(new Vector2(.6f, .5f), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
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
    internal double ShellSafeUntil { get; set; }
    public NetworkVariable<PlayerTaskState> TaskState = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> Aim = new(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<bool> FacingRight = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private GameObject carriedShell;
    [SerializeField] private GameObject namePanel;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private SpriteRenderer carriedItem;
    [SerializeField] private CrewSpriteSet crewSprites;
    [SerializeField] private PlayerTiming timing;
    [SerializeField] private CannonAim cannon;
    [Header("Emote")]
    [SerializeField] private GameObject emoteBubble;
    [SerializeField] private Image emoteIcon;
    [Tooltip("캐릭터 옆 이모티콘 말풍선 표시 시간(초)")]
    [SerializeField, Min(0f)] private float emoteBubbleSeconds = 2.6f;
    // 모든 클라이언트에서 이모티콘을 받았을 때 (보낸 플레이어, 0부터 시작하는 이모티콘 번호).
    public static event Action<NetworkPlayer, int> Emoted;
    private float emoteHideTime;
    private double lastActionTime = -1;
    private float nextRequestTime;
    private float nextReservationTime;
    private int shownEmote;
    private bool shown;
    private bool carryingShown;
    private static readonly int LocalOutline = Shader.PropertyToID("_LocalOutline");
    private MaterialPropertyBlock outlineProperties;
    private bool outlineShown;
    private Vector3 previousArtPosition;
    private FixedString64Bytes shownName;
    public bool Local => IsSpawned && Assigned.Value && !Bot.Value && NetworkObject.IsOwner;
    public static NetworkPlayer LocalPlayer { get; private set; }
    public PlayerTiming Timing => timing;
    public CannonAim Cannon => cannon;

    public override void OnNetworkSpawn()
    {
        Assigned.OnValueChanged += AssignmentChanged;
        Bot.OnValueChanged += AssignmentChanged;
        RefreshAssignment();
        body.enabled = false;
        namePanel.SetActive(false);
        carriedShell.SetActive(false);
        // mainGame처럼 말풍선을 아직 배치하지 않은 씬에서는 팀 알림만 뜬다.
        if (emoteBubble != null) emoteBubble.SetActive(false);
        EmoteTime.OnValueChanged += EmoteReceived;
        previousArtPosition = transform.localPosition;
    }
    // Emote(1~7)가 EmoteTime보다 먼저 선언돼 있어 이 시점에 이미 새 값이다.
    private void EmoteReceived(double before, double after)
    {
        if (Emote.Value > 0) Emoted?.Invoke(this, Emote.Value - 1);
    }
    public override void OnGainedOwnership() => RefreshAssignment();
    public override void OnLostOwnership() { if (LocalPlayer == this) LocalPlayer = null; }
    private void AssignmentChanged(bool before, bool after) => RefreshAssignment();
    private void RefreshAssignment()
    {
        if (Local && !SecondLocal)
        {
            if (LocalPlayer == null || !LocalPlayer.Local || LocalPlayer.SecondLocal) LocalPlayer = this;
        }
        else if (LocalPlayer == this) LocalPlayer = null;
    }
    private void Update()
    {
        bool active = IsSpawned && Assigned.Value;
        if (shown != active) { shown = active; body.enabled = active; namePanel.SetActive(active); }
        ShowLocalOutline(Local);
        bool carrying = active && Carrying.Value && crewSprites == null;
        if (carryingShown != carrying) { carryingShown = carrying; carriedShell.SetActive(carrying); }
        if (emoteBubble != null && emoteBubble.activeSelf && (!active || Time.unscaledTime >= emoteHideTime)) emoteBubble.SetActive(false);
        if (!active) return;
        if (crewSprites != null) DrawCrew();
        if (IsServer && CompletedStation.Value >= 0 && !timing.InsideCompletedArea((StationAction)CompletedStation.Value))
            CompletedStation.Value = -1;
        if (Local)
        {
            if (LocalPlayer == null) RefreshAssignment();
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

    private void DrawCrew()
    {
        var stations = CartStations.Instance;
        var pose = stations != null ? stations.CrewPose(this) : CrewSpriteSet.Pose.Idle;
        if (!crewSprites.HasWalkFrames || carriedItem == null)
        {
            body.sprite = crewSprites.Get(Slot.Value, pose);
            return;
        }
        Vector3 delta = transform.localPosition - previousArtPosition;
        previousArtPosition = transform.localPosition;
        bool moving = delta.sqrMagnitude > .000004f && delta.sqrMagnitude < 4f;
        if (moving && Mathf.Abs(delta.x) > .001f && (Local || IsServer && Bot.Value))
        {
            bool facing = delta.x > 0f;
            if (FacingRight.Value != facing) FacingRight.Value = facing;
        }
        int tick = Mathf.Max(0, (int)System.Math.Floor(NetworkManager.ServerTime.Time * 30d));
        body.sprite = crewSprites.Walk(Mathf.Clamp(Slot.Value, 0, 3), tick, moving);
        int order = WorldDepth.Order(transform.position, true);
        body.sortingOrder = order;
        body.flipX = FacingRight.Value;
        carriedItem.sprite = crewSprites.Equipment(pose);
        carriedItem.enabled = carriedItem.sprite != null;
        carriedItem.flipX = body.flipX;
        carriedItem.sortingOrder = order + 1;
        var offset = carriedItem.transform.localPosition;
        offset.x = Mathf.Abs(offset.x) * (body.flipX ? 1 : -1);
        carriedItem.transform.localPosition = offset;
    }

    private void ShowLocalOutline(bool visible)
    {
        if (crewSprites == null || outlineShown == visible) return;
        outlineShown = visible;
        outlineProperties ??= new MaterialPropertyBlock();
        body.GetPropertyBlock(outlineProperties);
        outlineProperties.SetFloat(LocalOutline, visible ? 1f : 0f);
        body.SetPropertyBlock(outlineProperties);
    }
    public void Assign(ulong client, int slot, FixedString64Bytes nickname)
    {
        if (!IsServer) return;
        Slot.Value = slot;
        Nickname.Value = nickname;
        // A host-owned bot must stay a bot until its new owner is assigned.
        NetworkObject.ChangeOwnership(client);
        Bot.Value = false;
        Assigned.Value = true;
        RefreshAssignment();
    }
    public void TakeBotControl(int slot)
    {
        if (!IsServer) return;
        Bot.Value = true;
        Slot.Value = slot;
        Nickname.Value = new FixedString64Bytes($"도우미 {slot + 1}");
        if (OwnerClientId != NetworkManager.ServerClientId) NetworkObject.RemoveOwnership();
        Assigned.Value = true;
        RefreshAssignment();
    }
    public void Release()
    {
        if (!IsServer) return;
        var game = CartStations.Instance;
        int keep = game.HoldsStation(StationAction.Cannon, this) ? (int)StationAction.Cannon : -1;
        game.ReleaseStations(NetworkObjectId, keep);
        if (game.Votes.Count > Slot.Value) game.Votes[Slot.Value] = -1;
        TakeBotControl(Slot.Value);
        game.TallyChoices();
    }
    public void RestoreSeat(SeatCheckpoint saved)
    {
        transform.localPosition = saved.Position;
        RestoreSeatRpc(saved.Position, saved.Aim, saved.DriverAim, saved.HasWhipAim || saved.DriverAim != Vector2.zero, saved.FacingRight);
    }

    [Rpc(SendTo.Owner)]
    private void RestoreSeatRpc(Vector3 localPosition, Vector2 aim, Vector2 whip, bool hasWhip, bool facingRight)
    {
        if (!IsOwner) return;
        Aim.Value = aim;
        FacingRight.Value = facingRight;
        if (hasWhip) DriverAim.Value = whip;
        transform.localPosition = localPosition;
        var sync = GetComponent<OwnerNetworkTransform>();
        if (sync != null && sync.CanCommitToTransform)
            sync.Teleport(sync.InLocalSpace ? localPosition : transform.position,
                sync.InLocalSpace ? transform.localRotation : transform.rotation, transform.localScale);
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
    [Rpc(SendTo.ClientsAndHost)]
    private void WorkResultRpc(bool loaded) { if (Local) timing.ShowResult(loaded); }
    public void ShowWorkResult(bool loaded) => WorkResultRpc(loaded);

    [Rpc(SendTo.Owner)]
    private void CookingImpactRpc(int percentLost)
    {
        if (Local) timing.ShowCookingImpact(percentLost);
    }

    internal void ShowCookingImpact(int percentLost)
    {
        if (!Bot.Value) CookingImpactRpc(percentLost);
    }

    public void Request(StationAction action, bool hold = false)
    {
        bool rateLimited = !hold && action != StationAction.CannonCleaning;
        if (!Local || (rateLimited && Time.unscaledTime < nextRequestTime)) return;
        if (rateLimited) nextRequestTime = Time.unscaledTime + 0.12f;
        var stations = CartStations.Instance;
        var care = TimingArea.IsRoyal(action) ? stations.AvailableCare(action, this) : CareAction.None;
        ActionRpc(action, NetworkManager.ServerTime.Time, hold, stations.StageNumber, care, DriverAim.Value);
    }

    public void RequestStoredShell()
    {
        if (Local && CartStations.Running) AddStoredShellRpc(CartStations.Instance.StageNumber);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void AddStoredShellRpc(int stage) => CartStations.Instance?.AddStoredShell(this, stage);
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ActionRpc(StationAction action, double inputTime, bool hold, int stage, CareAction care, Vector2 whipAim)
    {
        var stations = CartStations.Instance;
        if (!Assigned.Value || stations == null || stage != stations.StageNumber || !CartStations.Running) return;
        double now = NetworkManager.ServerTime.Time;
        if ((double.IsNaN(inputTime) || double.IsInfinity(inputTime)) || inputTime < now - .35 || inputTime > now + .1) return;
        if (hold)
        {
            if (Holding.Value && inputTime <= HoldDeadline.Value && inputTime >= HoldDeadline.Value - CartRules.Active.shellHoldSeconds)
                stations.ResolveShellHold(this, timing.TimingSuccess(action, true, inputTime));
            return;
        }
        if (Holding.Value) return;
        bool rateLimited = action != StationAction.CannonCleaning;
        if ((rateLimited && now-lastActionTime < 0.12) || !timing.IsInActionArea(action) || !stations.CanUse(action,this)) return;
        if (TimingArea.IsRoyal(action) && care != stations.AvailableCare(action, this)) return;
        if (rateLimited) lastActionTime = now;
        if (action == StationAction.ShellDeposit) stations.Enter(action,this);
        else if (action == StationAction.Cannon) stations.Ignite(this);
        else if (action == StationAction.Driver) stations.StrikeHorse(whipAim);
        else if (action != StationAction.ShellPile && action != StationAction.Reload && action != StationAction.WheelRepair
            || timing.TimingSuccess(action, false, inputTime)) stations.Succeed(action,this);
        else stations.Fail(action, this);
    }
    // TeamEmotes가 아이콘을 골라 호출한다.
    public void ShowEmoteBubble(Sprite icon)
    {
        if (emoteBubble == null) return;
        emoteIcon.sprite = icon;
        emoteHideTime = Time.unscaledTime + emoteBubbleSeconds;
        emoteBubble.SetActive(true);
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
        ShowLocalOutline(false);
        Assigned.OnValueChanged -= AssignmentChanged;
        Bot.OnValueChanged -= AssignmentChanged;
        EmoteTime.OnValueChanged -= EmoteReceived;
        if (LocalPlayer == this) LocalPlayer = null;
    }
}

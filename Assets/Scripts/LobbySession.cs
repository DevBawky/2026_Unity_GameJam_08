using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Owns Relay and NGO lifetime. UI is authored and referenced in the Lobby scene.</summary>
[RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
public sealed class LobbySession : MonoBehaviour
{
    public const int Capacity = 4;
    public const string GameScene = "mainGame";
    [SerializeField] private NetworkManager manager;
    [SerializeField] private UnityTransport transport;
    [SerializeField] private LobbyRoster roster;
    [SerializeField] private NetworkObject playerPrefab;
    private readonly Dictionary<ulong, (int slot, string name)> admitted = new();
    private bool leaving;
    private bool quitting;
    private static string returnMessage;
    private System.Threading.CancellationToken lifetime;
    public bool Busy { get; private set; }
    public string Status { get; private set; } = "";
    public string RoomCode { get; private set; } = "";
    public bool InRoom => manager != null && manager.IsConnectedClient && roster != null && roster.IsSpawned;
    public bool IsHost => InRoom && manager.IsHost;
    public LobbyRoster Roster => roster;

    private void Awake()
    {
        Application.runInBackground = true;
        lifetime = destroyCancellationToken;
        Status = returnMessage ?? "";
        returnMessage = null;
        DontDestroyOnLoad(gameObject);
        manager.ConnectionApprovalCallback = Approve;
        manager.OnClientConnectedCallback += Connected;
        manager.OnClientDisconnectCallback += Disconnected;
        manager.OnTransportFailure += TransportFailed;
    }

    private async Task Prepare(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname) || nickname.Trim().Length > 16 ||
            System.Linq.Enumerable.Any(nickname, char.IsControl))
            throw new InvalidOperationException("닉네임을 1~16자로 입력해주세요.");
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions();
#if UNITY_EDITOR
            // Separate anonymous identities in Multiplayer Play Mode editor processes.
            options.SetProfile("editor-" + System.Diagnostics.Process.GetCurrentProcess().Id);
#endif
            await WithTimeout(UnityServices.InitializeAsync(options));
        }
        if (!AuthenticationService.Instance.IsSignedIn)
            await WithTimeout(AuthenticationService.Instance.SignInAnonymouslyAsync());
        lifetime.ThrowIfCancellationRequested();
        manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(nickname.Trim());
    }

    public async Task CreateRoomAsync(string nickname)
    {
        if (Busy || manager.IsListening) return;
        Busy = true;
        Status = "방을 만드는 중…";
        try
        {
            await Prepare(nickname);
            Allocation allocation = await WithTimeout(RelayService.Instance.CreateAllocationAsync(Capacity - 1));
            string code = await WithTimeout(RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId));
            transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            RoomCode = code;
            if (!manager.StartHost()) throw new InvalidOperationException("방을 시작하지 못했습니다.");
            roster.Code.Value = new Unity.Collections.FixedString64Bytes(code);
            Status = "방 코드를 친구에게 공유하세요.";
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { await Failed(e); }
        finally { Busy = false; }
    }

    public async Task JoinRoomAsync(string nickname, string code)
    {
        if (Busy || manager.IsListening) return;
        Busy = true;
        Status = "방에 연결하는 중…";
        try
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length != 6 || !System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Z0-9]+$"))
                throw new InvalidOperationException("6자리 방 코드를 확인해주세요.");
            await Prepare(nickname);
            JoinAllocation allocation = await WithTimeout(RelayService.Instance.JoinAllocationAsync(code));
            transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            if (!manager.StartClient()) throw new InvalidOperationException("접속을 시작하지 못했습니다.");
            float until = Time.realtimeSinceStartup + 20f;
            while (!InRoom && manager.IsListening && Time.realtimeSinceStartup < until)
            {
                lifetime.ThrowIfCancellationRequested();
                await Task.Yield();
            }
            if (!InRoom) throw new InvalidOperationException(string.IsNullOrEmpty(manager.DisconnectReason)
                ? "연결 시간이 초과되었습니다. 방 코드를 확인해주세요." : manager.DisconnectReason);
            RoomCode = roster.Code.Value.ToString();
            Status = "방장이 게임을 시작할 때까지 기다려주세요.";
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { await Failed(e); }
        finally { Busy = false; }
    }

    private async Task Failed(Exception error)
    {
        leaving = true;
        if (manager.IsListening) manager.Shutdown();
        while (manager != null && manager.ShutdownInProgress)
        {
            if (lifetime.IsCancellationRequested) return;
            await Task.Yield();
        }
        if (lifetime.IsCancellationRequested) return;
        admitted.Clear();
        RoomCode = "";
        Status = "연결 실패: " + error.Message;
        leaving = false;
    }

    private async Task WithTimeout(Task operation)
    {
        using var delay = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(lifetime);
        if (await Task.WhenAny(operation, Task.Delay(30000, delay.Token)) != operation)
        {
            // Observe a late SDK failure even when the caller has timed out.
            _ = operation.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            lifetime.ThrowIfCancellationRequested();
            throw new TimeoutException("서버 응답 시간이 초과되었습니다. 다시 시도해주세요.");
        }
        delay.Cancel();
        await operation;
        lifetime.ThrowIfCancellationRequested();
    }

    private async Task<T> WithTimeout<T>(Task<T> operation)
    {
        await WithTimeout((Task)operation);
        return await operation;
    }

    private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.CreatePlayerObject = false; // Players spawn together after the network scene load.
        response.Pending = false;
        if (roster.Starting.Value) { response.Reason = "이미 게임이 시작된 방입니다."; return; }
        string nickname;
        try { nickname = new UTF8Encoding(false, true).GetString(request.Payload); }
        catch { response.Reason = "잘못된 닉네임입니다."; return; }
        if (string.IsNullOrWhiteSpace(nickname) || nickname.Length > 16 ||
            System.Linq.Enumerable.Any(nickname, char.IsControl))
        { response.Reason = "닉네임을 1~16자로 입력해주세요."; return; }
        int slot = 0;
        while (slot < Capacity && System.Linq.Enumerable.Any(admitted.Values, p => p.slot == slot)) slot++;
        if (slot == Capacity) { response.Reason = "방이 가득 찼습니다. (최대 4명)"; return; }
        admitted[request.ClientNetworkId] = (slot, nickname.Trim());
        response.Approved = true;
    }

    private void Connected(ulong id)
    {
        if (!manager.IsServer || !admitted.TryGetValue(id, out var player)) return;
        roster.Players.Add(new LobbyPlayerInfo { ClientId = id, Slot = player.slot,
            Nickname = new Unity.Collections.FixedString64Bytes(player.name) });
    }

    private void Disconnected(ulong id)
    {
        if (manager.IsServer)
        {
            admitted.Remove(id);
            if (roster.IsSpawned)
                for (int i = roster.Players.Count - 1; i >= 0; i--)
                    if (roster.Players[i].ClientId == id) roster.Players.RemoveAt(i);
        }
        if (id != manager.LocalClientId || leaving || Busy || quitting) return;
        _ = LeaveRoomAsync("방장과 연결이 끊어졌습니다.");
    }

    private void TransportFailed()
    {
        if (!Busy && !leaving && !quitting) _ = LeaveRoomAsync("네트워크 연결이 끊어졌습니다.");
    }

    public async Task LeaveRoomAsync(string message = "방에서 나왔습니다.")
    {
        if (leaving) return;
        leaving = true;
        Busy = true;
        returnMessage = message;
        manager.Shutdown();
        while (manager != null && manager.ShutdownInProgress)
        {
            if (lifetime.IsCancellationRequested) return;
            await Task.Yield();
        }
        if (quitting || lifetime.IsCancellationRequested) return;
        // Reload the authored scene to reset scene NetworkObjects and all references.
        Destroy(roster.gameObject);
        Destroy(gameObject);
        SceneManager.LoadScene("Lobby");
    }

    public void StartGame()
    {
        if (!IsHost || Busy || roster.Starting.Value) return;
        if (!Application.CanStreamedLevelBeLoaded(GameScene))
        {
            Status = "mainGame 씬이 빌드 목록에 없습니다.";
            return;
        }
        roster.Starting.Value = true;
        manager.SceneManager.OnLoadEventCompleted += Loaded;
        var result = manager.SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
        if (result != SceneEventProgressStatus.Started)
        {
            manager.SceneManager.OnLoadEventCompleted -= Loaded;
            roster.Starting.Value = false;
            Status = "씬을 불러오지 못했습니다: " + result;
        }
    }

    private void Loaded(string scene, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
    {
        if (scene != GameScene || !manager.IsServer) return;
        manager.SceneManager.OnLoadEventCompleted -= Loaded;
        foreach (ulong id in timedOut) manager.DisconnectClient(id);
        var spawn = FindFirstObjectByType<MultiplayerGameScene>();
        if (spawn == null) { Debug.LogError("mainGame에 MultiplayerGameScene 설정이 없습니다."); return; }
        foreach (var entry in admitted)
        {
            if (!completed.Contains(entry.Key) || !manager.ConnectedClients.ContainsKey(entry.Key)) continue;
            var player = Instantiate(playerPrefab, spawn.SpawnPosition(entry.Value.slot), Quaternion.identity);
            player.SpawnAsPlayerObject(entry.Key, true);
            player.GetComponent<LobbyNetworkPlayer>().Slot.Value = entry.Value.slot;
        }
    }

    private void OnApplicationQuit() { quitting = true; }
    private void OnDestroy()
    {
        if (manager == null) return;
        manager.OnClientConnectedCallback -= Connected;
        manager.OnClientDisconnectCallback -= Disconnected;
        manager.OnTransportFailure -= TransportFailed;
        manager.ConnectionApprovalCallback = null;
    }
}

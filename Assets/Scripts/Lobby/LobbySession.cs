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
public sealed partial class LobbySession : MonoBehaviour
{
    public static int LocalSeats { get; private set; }
    public static bool PracticeTest { get; private set; }
    public const int Capacity = 4;
    public const string GameScene = "mainGame";
    [SerializeField] private NetworkManager manager;
    [SerializeField] private UnityTransport transport;
    [SerializeField] private LobbyRoster rosterPrefab;
    private LobbyRoster roster;
    internal void BindRoster(LobbyRoster instance) => roster=instance;
    private bool StartHost()
    {
        if(!manager.StartHost()) return false;
        roster=Instantiate(rosterPrefab);
        roster.NetworkObject.Spawn(false);
        Connected(NetworkManager.ServerClientId);
        return true;
    }
    private readonly Dictionary<ulong, (int slot, string name)> admitted = new();
    private bool leaving;
    private bool quitting;
    private bool loadingGame;
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
        LocalSeats = 0;
        PracticeTest = false;
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
        manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(ConnectionIdentity(nickname));
    }

    public void StartPractice(int seats, bool endless = false)
    {
        if (Busy || manager.IsListening || seats < 1 || seats > 2) return;
        LocalSeats = seats;
        PracticeTest = endless;
        manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes("P1");
        transport.SetConnectionData("127.0.0.1",0,"127.0.0.1");
        if (!StartHost()) { LocalSeats = 0; Status = "연습을 시작하지 못했습니다."; SoundManager.PlayCue(GameSfx.UiReject); return; }
        SoundManager.PlayCue(GameSfx.UiConfirm);
        StartGame();
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
            transport.SetRelayServerData(allocation.ToRelayServerData(Protocol));
            checkpoint=null;
            await CreateDirectory(code);
            if (!StartHost()) throw new InvalidOperationException("방을 시작하지 못했습니다.");
            roster.Code.Value = new Unity.Collections.FixedString64Bytes(RoomCode);
            Status = "방 코드를 친구에게 공유하세요.";
            SoundManager.PlayCue(GameSfx.UiConfirm);
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
            if (!System.Text.RegularExpressions.Regex.IsMatch(code, "^[0-9]{3}$"))
                throw new InvalidOperationException("3자리 방 코드를 확인해주세요.");
            await Prepare(nickname);
            checkpoint=null;
            await JoinDirectory(code);
            JoinAllocation allocation = await WithTimeout(RelayService.Instance.JoinAllocationAsync(relayCode));
            transport.SetRelayServerData(allocation.ToRelayServerData(Protocol));
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
            SoundManager.PlayCue(GameSfx.UiConfirm);
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
        try { await CloseDirectory(true); } catch (Exception cleanup) { Debug.LogWarning(cleanup.Message); }
        checkpoint=null;
        admitted.Clear();
        RoomCode = "";
        Status = "연결 실패: " + error.Message;
        SoundManager.PlayCue(GameSfx.UiReject);
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
        if (loadingGame || (LocalSeats > 0 && request.ClientNetworkId != NetworkManager.ServerClientId)) { response.Reason = "현재 입장할 수 없습니다."; return; }
        string nickname;
        try { nickname = new UTF8Encoding(false, true).GetString(request.Payload); }
        catch { response.Reason = "잘못된 닉네임입니다."; return; }
        string key = null;
        if (LocalSeats == 0)
        {
            if (nickname.Length < 34 || nickname[32] != '|' || !Guid.TryParseExact(nickname.Substring(0,32),"N",out _)) { response.Reason="좌석 키 오류"; return; }
            key=Verifier(nickname.Substring(0,32));nickname=nickname.Substring(33);
        }
        if (string.IsNullOrWhiteSpace(nickname) || nickname.Length > 16 ||
            System.Linq.Enumerable.Any(nickname, char.IsControl))
        { response.Reason = "닉네임을 1~16자로 입력해주세요."; return; }
        int slot = key == null ? -1 : Array.IndexOf(seatKeys,key);
        if (slot >= 0 && System.Linq.Enumerable.Any(admitted.Values,p=>p.slot==slot)) { response.Reason="이미 연결된 좌석입니다."; return; }
        if (slot < 0)
        {
            slot=0;
            while(slot<Capacity && (seatKeys[slot]!=null || System.Linq.Enumerable.Any(admitted.Values,p=>p.slot==slot))) slot++;
            if(slot==Capacity){slot=0;while(slot<Capacity && System.Linq.Enumerable.Any(admitted.Values,p=>p.slot==slot)) slot++;}
        }
        if (slot == Capacity) { response.Reason = "방이 가득 찼습니다. (최대 4명)"; return; }
        seatKeys[slot]=key;
        admitted[request.ClientNetworkId] = (slot, nickname.Trim());
        response.Approved = true;
    }

    private void Connected(ulong id)
    {
        if (roster==null || !roster.IsSpawned || !manager.IsServer || !admitted.TryGetValue(id, out var player)) return;
        var game = FindFirstObjectByType<MultiplayerGameScene>();
        if (!loadingGame && roster.Starting.Value && game != null) game.Assign(id,player.slot,new Unity.Collections.FixedString64Bytes(player.name));
        roster.Players.Add(new LobbyPlayerInfo { ClientId = id, Slot = player.slot,
            Nickname = new Unity.Collections.FixedString64Bytes(player.name) });
    }

    private void Disconnected(ulong id)
    {
        if (leaving || quitting || (manager.IsServer && manager.ShutdownInProgress)) return;
        if (manager.IsServer)
        {
            var game=FindFirstObjectByType<MultiplayerGameScene>();
            if (game != null && admitted.TryGetValue(id,out var seat)) game.Players[seat.slot].Release();
            admitted.Remove(id);
            if (roster!=null && roster.IsSpawned)
                for (int i = roster.Players.Count - 1; i >= 0; i--)
                    if (roster.Players[i].ClientId == id) roster.Players.RemoveAt(i);
        }
        if (id != manager.LocalClientId || leaving || Busy || quitting || recovering) return;
        if(directory!=null && !manager.IsServer){_=BeginRecovery();return;}
        _ = LeaveRoomAsync("방장과 연결이 끊어졌습니다.");
    }

    private void TransportFailed()
    {
        if(directory!=null && !IsHost && !Busy && !leaving && !quitting){_=BeginRecovery();return;}
        if (!Busy && !leaving && !quitting && !recovering) _ = LeaveRoomAsync("네트워크 연결이 끊어졌습니다.");
    }

    public async Task LeaveRoomAsync(string message = "방에서 나왔습니다.")
    {
        if (leaving) return;
        leaving = true;
        Busy = true;
        returnMessage = message;
        if(!recovering)
        {
            if(IsHost && directory!=null) SendCheckpoint();
            try{await CloseDirectory(true);}catch(Exception e){Debug.LogWarning(e.Message);}
            checkpoint=null;
        }
        manager.Shutdown();
        while (manager != null && manager.ShutdownInProgress)
        {
            if (lifetime.IsCancellationRequested) return;
            await Task.Yield();
        }
        if (quitting || lifetime.IsCancellationRequested) return;
        // Reload the authored scene to reset scene NetworkObjects and all references.
        if(roster!=null) Destroy(roster.gameObject);
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
        loadingGame = true;
        manager.SceneManager.OnLoadEventCompleted += Loaded;
        var result = manager.SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
        if (result != SceneEventProgressStatus.Started)
        {
            manager.SceneManager.OnLoadEventCompleted -= Loaded;
            loadingGame = false;
            roster.Starting.Value = false;
            Status = "씬을 불러오지 못했습니다: " + result;
        }
    }

    private void Loaded(string scene, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
    {
        if (scene != GameScene || !manager.IsServer) return;
        loadingGame = false;
        manager.SceneManager.OnLoadEventCompleted -= Loaded;
        foreach (ulong id in timedOut) manager.DisconnectClient(id);
        var spawn = FindFirstObjectByType<MultiplayerGameScene>();
        if (spawn == null) { Debug.LogError("mainGame에 MultiplayerGameScene 설정이 없습니다."); return; }
        foreach (var entry in admitted)
        {
            if (!completed.Contains(entry.Key) || !manager.ConnectedClients.ContainsKey(entry.Key)) continue;
            spawn.Assign(entry.Key,entry.Value.slot,new Unity.Collections.FixedString64Bytes(entry.Value.name));
        }
        if (LocalSeats == 2) spawn.Assign(NetworkManager.ServerClientId,1,new Unity.Collections.FixedString64Bytes("P2"));
        CartStations.Instance.Begin();
        if (!string.IsNullOrEmpty(retryEntryJson)) { CartStations.Instance.Restore(JsonUtility.FromJson<RunCheckpoint>(retryEntryJson)); retryEntryJson=null; }
        if(restoreOnLoad!=null){CartStations.Instance.Restore(restoreOnLoad);restoreOnLoad=null;}
    }

    private string retryEntryJson;
    public bool ReloadGame(string entryJson = null)
    {
        if (!manager.IsHost || loadingGame) return false;
        loadingGame = true;
        manager.SceneManager.OnLoadEventCompleted += Loaded;
        var result = manager.SceneManager.LoadScene(GameScene,LoadSceneMode.Single);
        if (result == SceneEventProgressStatus.Started) { retryEntryJson=entryJson; return true; }
        manager.SceneManager.OnLoadEventCompleted -= Loaded;
        loadingGame = false;
        Status = "씬을 다시 불러오지 못했습니다: " + result;
        return false;
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

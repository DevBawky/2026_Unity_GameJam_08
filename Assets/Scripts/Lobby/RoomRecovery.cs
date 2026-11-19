using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lobby membership survives replacement of the Relay allocation. Only the elected
// lobby host publishes a replacement endpoint and restores the shared checkpoint.
public sealed partial class LobbySession
{
    private const string RelayKey="relay";
    private Lobby directory;
    private ILobbyEvents subscription;
    private string relayCode, displayName;
    private readonly string[] seatKeys=new string[Capacity];
    private float nextHeartbeat, nextCheckpoint;
    private bool heartbeatPending, recovering;
    private static byte[] checkpoint;
    private static Handoff pending;
    private sealed class Handoff { public Lobby Room; public string Code, Relay, Name; }
    private static string PlayerId=>AuthenticationService.Instance.PlayerId;
    private static string DirectoryId(string code)=>"b7cf086c-c030-49c5-a06d-545463160"+code;
    private static string Protocol=>Application.platform==RuntimePlatform.WebGLPlayer?"wss":"dtls";
    private static DataObject MemberData(string value)=>new(DataObject.VisibilityOptions.Member,value);

    private void Start()
    {
        if(pending!=null) _=ResumeRoom();
    }
    private void Update()
    {
        if(directory==null || recovering || leaving || !IsHost) return;
        if(!heartbeatPending && Time.unscaledTime>=nextHeartbeat) _=Heartbeat();
        if(CartStations.Instance==null || !CartStations.Instance.IsSpawned || manager.ConnectedClients.Count<2 || Time.unscaledTime<nextCheckpoint) return;
        nextCheckpoint=Time.unscaledTime+0.5f;
        SendCheckpoint();
    }
    private void SendCheckpoint()
    {
        if(CartStations.Instance==null || !CartStations.Instance.IsSpawned || CartStations.Instance.Votes.Count!=Capacity) return;
        var json=JsonUtility.ToJson(CartStations.Instance.Capture(seatKeys));
        using var stream=new MemoryStream();
        using(var zip=new GZipStream(stream,System.IO.Compression.CompressionLevel.Fastest,true))
        { var data=Encoding.UTF8.GetBytes(json);zip.Write(data,0,data.Length); }
        roster.CheckpointRpc(stream.ToArray());
    }
    internal static void Remember(byte[] bytes) => checkpoint=bytes;
    private static RunCheckpoint ReadCheckpoint()
    {
        if(checkpoint==null) return null;
        using var input=new MemoryStream(checkpoint);
        using var zip=new GZipStream(input,CompressionMode.Decompress);
        using var reader=new StreamReader(zip,Encoding.UTF8);
        return JsonUtility.FromJson<RunCheckpoint>(reader.ReadToEnd());
    }
    private async Task Heartbeat()
    {
        heartbeatPending=true;nextHeartbeat=Time.unscaledTime+12;
        try{await LobbyService.Instance.SendHeartbeatPingAsync(directory.Id);}
        catch(LobbyServiceException e){if(!leaving) Status=e.Message;}
        finally{heartbeatPending=false;}
    }
    private async Task Subscribe()
    {
        subscription=await WithTimeout(LobbyService.Instance.SubscribeToLobbyEventsAsync(directory.Id,new LobbyEventCallbacks()));
    }
    private async Task CreateDirectory(string relay)
    {
        for(int attempt=0;attempt<6;attempt++)
        {
            string shortCode=UnityEngine.Random.Range(0,1000).ToString("D3");
            var options=new CreateLobbyOptions{IsPrivate=true,Data=new Dictionary<string,DataObject>{{RelayKey,MemberData(relay)}}};
            Lobby room;
            try{room=await WithTimeout(LobbyService.Instance.CreateOrJoinLobbyAsync(DirectoryId(shortCode),"Carriage "+shortCode,Capacity,options));}
            catch(LobbyServiceException e) when(e.Reason==LobbyExceptionReason.LobbyFull){continue;}
            if(room.HostId==PlayerId && room.Data[RelayKey].Value==relay)
            {directory=room;RoomCode=shortCode;relayCode=relay;await Subscribe();return;}
            if(room.HostId!=PlayerId)await WithTimeout(LobbyService.Instance.RemovePlayerAsync(room.Id,PlayerId));
        }
        throw new InvalidOperationException("잠시 뒤 방 만들기를 다시 시도해주세요.");
    }
    private async Task JoinDirectory(string code)
    {
        string id=DirectoryId(code);
        var memberships=await WithTimeout(LobbyService.Instance.GetJoinedLobbiesAsync());
        directory=memberships.Contains(id) ? await WithTimeout(LobbyService.Instance.ReconnectToLobbyAsync(id)) : await WithTimeout(LobbyService.Instance.JoinLobbyByIdAsync(id));
        RoomCode=code;relayCode=directory.Data[RelayKey].Value;await Subscribe();
    }
    private async Task CloseDirectory(bool voluntary)
    {
        try{if(subscription!=null)await WithTimeout(subscription.UnsubscribeAsync());}
        catch(Exception){ }
        subscription=null;
        var room=directory;directory=null;
        if(!voluntary || room==null) return;
        try{await WithTimeout(LobbyService.Instance.RemovePlayerAsync(room.Id,PlayerId));}
        catch(LobbyServiceException e) when(e.Reason==LobbyExceptionReason.LobbyNotFound || e.Reason==LobbyExceptionReason.PlayerNotFound){ }
    }
    private string ConnectionIdentity(string nickname)
    {
        displayName=nickname.Trim();
        // Seat secrets never leave the transport in a snapshot. Peers get the verifier.
        string pref="Crew.Seat."+PlayerId;
        string secret=PlayerPrefs.GetString(pref,"");
        if(secret.Length!=32){secret=Guid.NewGuid().ToString("N");PlayerPrefs.SetString(pref,secret);PlayerPrefs.Save();}
        return secret+"|"+displayName;
    }
    private static string Verifier(string secret)
    {
        using var hash=SHA256.Create();
        return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(secret)));
    }
    private async Task BeginRecovery()
    {
        if(recovering || leaving || quitting) return;
        recovering=true;
        pending=new Handoff {Room=directory,Code=RoomCode,Relay=relayCode,Name=displayName};
        await CloseDirectory(false);
        await LeaveRoomAsync("방 연결을 복구하고 있습니다.");
    }
    private async Task ResumeRoom()
    {
        var handoff=pending;pending=null;
        recovering=true;Busy=true;
        try
        {
            directory=handoff.Room;RoomCode=handoff.Code;displayName=handoff.Name;
            await Prepare(displayName);
            await Subscribe();
            float end=Time.realtimeSinceStartup+150;
            float retryOldEndpoint=Time.realtimeSinceStartup+3;
            while(Time.realtimeSinceStartup<end)
            {
                lifetime.ThrowIfCancellationRequested();
                directory=await WithTimeout(LobbyService.Instance.GetLobbyAsync(directory.Id));
                if(directory.HostId==PlayerId)
                {
                    var snapshot=ReadCheckpoint();
                    if(snapshot!=null)Array.Copy(snapshot.SeatKeys,seatKeys,Capacity);
                    var allocation=await WithTimeout(RelayService.Instance.CreateAllocationAsync(Capacity-1));
                    relayCode=await WithTimeout(RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId));
                    transport.SetRelayServerData(allocation.ToRelayServerData(Protocol));
                    if(!StartHost())throw new InvalidOperationException("방장 승계에 실패했습니다.");
                    roster.Code.Value=new Unity.Collections.FixedString64Bytes(RoomCode);
                    directory=await WithTimeout(LobbyService.Instance.UpdateLobbyAsync(directory.Id,new UpdateLobbyOptions{Data=new Dictionary<string,DataObject>{{RelayKey,MemberData(relayCode)}}}));
                    Busy=false;
                    if(snapshot!=null){restoreOnLoad=snapshot;StartGame();}
                    Status="방장 권한을 이어받았습니다.";return;
                }
                string next=directory.Data[RelayKey].Value;
                if(next!=handoff.Relay || Time.realtimeSinceStartup > retryOldEndpoint)
                {
                    relayCode=next;
                    JoinAllocation allocation;
                    try { allocation=await WithTimeout(RelayService.Instance.JoinAllocationAsync(relayCode)); }
                    catch (RelayServiceException) when (next==handoff.Relay) { retryOldEndpoint=Time.realtimeSinceStartup+8;continue; }
                    transport.SetRelayServerData(allocation.ToRelayServerData(Protocol));
                    if(!manager.StartClient())throw new InvalidOperationException("새 방장과 연결하지 못했습니다.");
                    try { await WaitForRoom(); }
                    catch (InvalidOperationException) when (next==handoff.Relay)
                    {
                        manager.Shutdown();while(manager.ShutdownInProgress) await Task.Yield();
                        retryOldEndpoint=Time.realtimeSinceStartup+8;continue;
                    }
                    Status="운행에 다시 연결했습니다.";return;
                }
                await Task.Delay(1300,lifetime);
            }
            throw new TimeoutException("방장 승계 시간이 초과되었습니다.");
        }
        catch(OperationCanceledException){}
        catch(Exception e){await Failed(e);}
        finally{recovering=false;Busy=false;}
    }
    private RunCheckpoint restoreOnLoad;
    private async Task WaitForRoom()
    {
        float end=Time.realtimeSinceStartup+25;
        while(!InRoom && manager.IsListening && Time.realtimeSinceStartup<end){lifetime.ThrowIfCancellationRequested();await Task.Yield();}
        if(!InRoom)throw new InvalidOperationException(string.IsNullOrEmpty(manager.DisconnectReason)?"방에 연결할 수 없습니다.":manager.DisconnectReason);
    }
}


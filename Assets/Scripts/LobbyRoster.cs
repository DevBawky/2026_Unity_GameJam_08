using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct LobbyPlayerInfo : INetworkSerializable, IEquatable<LobbyPlayerInfo>
{
    public ulong ClientId;
    public int Slot;
    public FixedString64Bytes Nickname;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Slot);
        serializer.SerializeValue(ref Nickname);
    }
    public bool Equals(LobbyPlayerInfo other) => ClientId == other.ClientId && Slot == other.Slot && Nickname == other.Nickname;
}

[RequireComponent(typeof(NetworkObject))]
public sealed class LobbyRoster : NetworkBehaviour
{
    public NetworkList<LobbyPlayerInfo> Players;
    public NetworkVariable<FixedString64Bytes> Code = new();
    public NetworkVariable<bool> Starting = new(false);
    private void Awake() { Players = new NetworkList<LobbyPlayerInfo>(); }
    public override void OnNetworkSpawn() { DontDestroyOnLoad(gameObject); }
}

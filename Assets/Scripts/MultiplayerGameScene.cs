using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MultiplayerGameScene : MonoBehaviour
{
    [SerializeField] private NetworkPlayer[] players;
    [SerializeField] private Transform[] spawnPoints;
    public NetworkPlayer[] Players => players;
    private void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            SceneManager.LoadScene("Lobby");
    }
    public void Assign(ulong client, int slot, Unity.Collections.FixedString64Bytes name)
    {
        var player = players[slot];
        if (player.Slot.Value < 0) player.transform.position = spawnPoints[slot].position;
        player.Assign(client,slot,name);
    }
    public void FillEmptySeats()
    {
        for (int slot = 0; slot < players.Length; slot++)
        {
            var player = players[slot];
            if (player.Assigned.Value) continue;
            player.transform.position = spawnPoints[slot].position;
            player.TakeBotControl(slot);
        }
    }
    public void Release(ulong client)
    {
        foreach (var player in players)
            if (player.IsSpawned && player.Assigned.Value && player.OwnerClientId==client) player.Release();
    }
}

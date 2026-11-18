using Unity.Netcode;
using UnityEngine;

public sealed class MultiplayerGameScene : MonoBehaviour
{
    [SerializeField] private GameObject singlePlayer;
    [SerializeField] private Transform[] spawnPoints;
    private void Awake()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            singlePlayer.SetActive(false);
    }
    public Vector3 SpawnPosition(int slot) => spawnPoints[slot].position;
}

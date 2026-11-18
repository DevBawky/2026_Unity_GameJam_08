using Unity.Netcode;
using UnityEngine;

// 테스트 씬을 로비 없이 바로 Play할 때 혼자 호스트로 시작한다.
// 로비를 거쳐 들어오면 NetworkManager가 이미 있으므로 아무것도 하지 않는다.
[DefaultExecutionOrder(-100)]
public sealed class SoloTestBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager soloManager;
    [SerializeField] private MultiplayerGameScene gameScene;

    private void Start()
    {
        if (NetworkManager.Singleton != null) return;
        soloManager.gameObject.SetActive(true);
        if (!soloManager.StartHost()) { Debug.LogError("솔로 테스트 호스트를 시작하지 못했습니다."); return; }
        gameScene.Assign(soloManager.LocalClientId, 0, "Solo");
        CartStations.Instance.Begin();
    }
}

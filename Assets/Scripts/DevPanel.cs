using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 개발자 패널. F1로 열고 닫으며, 버튼은 씬에서 OnClick으로 아래 메서드에 연결한다.
// 상태를 바꾸는 버튼은 호스트에서만 동작한다. 개발 빌드(에디터 포함)에서만 켜진다.
public sealed class DevPanel : MonoBehaviour
{
    private static readonly float[] Speeds = { 1f, 2f, 4f };

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text status;
    [SerializeField] private CartStations stations;
    [SerializeField] private EnemySpawner spawner;

    private int speedIndex;

    private void Awake()
    {
        if (!Debug.isDebugBuild) gameObject.SetActive(false);
        panel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.f1Key.wasPressedThisFrame) return;
        panel.SetActive(!panel.activeSelf);
        Show();
    }

    private void OnDestroy() => Time.timeScale = 1f;

    public void Repair() { stations.DevRepair(); Show(); }
    public void Calm() { stations.DevCalm(); Show(); }
    public void RefillShells() { stations.DevRefillShells(); Show(); }
    public void ClearStage() { stations.DevClearStage(); Show(); }
    public void Fail() { stations.DevFail(); Show(); }
    public void ToggleInvincible() { stations.DevInvincible = !stations.DevInvincible; Show(); }
    public void ClearEnemies() { if (stations.IsServer) spawner.ClearEnemies(); Show(); }

    public void ToggleEnemies()
    {
        if (!stations.IsServer) return;
        spawner.enabled = !spawner.enabled;
        if (!spawner.enabled) spawner.ClearEnemies();
        Show();
    }

    public void CycleSpeed()
    {
        speedIndex = (speedIndex + 1) % Speeds.Length;
        Time.timeScale = Speeds[speedIndex];
        Show();
    }

    private void Show()
    {
        if (!panel.activeSelf) return;
        string host = stations.IsServer ? "" : "호스트 전용\n";
        status.text = $"{host}속도 x{Speeds[speedIndex]}\n적 스폰 {(spawner.enabled ? "켜짐" : "꺼짐")}\n무적 {(stations.DevInvincible ? "켜짐" : "꺼짐")}";
    }
}

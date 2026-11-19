using Unity.Netcode;

// 개발자 패널(F1) 전용 명령. 서버에서만 동작한다.
public sealed partial class CartStations
{
    public bool DevInvincible { get; set; }
    public void DevRepair() { if (!IsServer) return; durability = Capacity; RepairAllWheels(); Refresh(); }
    public void DevCalm()
    {
        if (!IsServer) return;
        Calm(Prince);
        Calm(Princess);
        princeAnxiety = princessAnxiety = 0f;
        Refresh();
    }
    private static void Calm(NetworkVariable<RiderState> rider)
    {
        var child = rider.Value;
        child.Attention = 100f;
        child.Upset = child.Bored = child.BoredomPending = false;
        child.Hunger = 0f;
        rider.Value = child;
    }
    public void DevRefillShells() { if (!IsServer) return; shellsInPile = StartShellsInPile; loaded = true; Refresh(); }
    public void DevClearStage() { if (IsServer && flow == StageFlow.Running) BeginUpgradeSelection(); }
    // 스테이지를 끝내고 마왕 보고도 건너뛰어 바로 상점을 연다.
    public void DevOpenShop()
    {
        if (!IsServer) return;
        if (flow == StageFlow.Running) BeginUpgradeSelection();
        if (flow != StageFlow.ChoosingUpgrade || Report.Value >= 0) return;
        Report.Value = 0;
        ShopRound.Value++;
    }
    public void DevAddPoints(int amount) { if (IsServer) Affection.Value += amount; }
    public void DevFail() { if (IsServer) FailStage("DEV FAIL"); }
}

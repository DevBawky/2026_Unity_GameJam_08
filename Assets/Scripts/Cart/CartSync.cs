using Unity.Collections;
using UnityEngine;

// 서버 상태를 CartNetworkState로 묶어 매 틱 보내고, 클라이언트는 받은 값으로 되살린다.
public sealed partial class CartStations
{
    public override void OnNetworkSpawn()
    {
        if (IsServer)
            for (int i = 0; i < StationCount; i++) StationOwners.Add(NoStationOwner);
        State.OnValueChanged += Changed;
        Failure.OnValueChanged += FailureChanged;
        NetworkManager.NetworkTickSystem.Tick += Publish;
        if (IsServer) Publish(); else Apply(State.Value);
    }
    public override void OnNetworkDespawn()
    {
        ClearWheelVisuals();
        State.OnValueChanged -= Changed;
        Failure.OnValueChanged -= FailureChanged;
        NetworkManager.NetworkTickSystem.Tick -= Publish;
    }
    private void Changed(CartNetworkState before, CartNetworkState after) { if (!IsServer) Apply(after); }
    private void FailureChanged(FixedString64Bytes before, FixedString64Bytes after) => FailureReason = after.ToString();
    private void Publish()
    {
        if (!IsSpawned || !IsServer) return;
        State.Value = new CartNetworkState { Flow=flow, Durability=durability, Prince=princeAnxiety, Princess=princessAnxiety,
            StageStart=stageStartDistance, Stage=stageNumber, Stored=depositedShells, Pile=shellsInPile, Lost=lostShells,
            RepairSteps=wheelRepairSteps, Tire=detachedTire != null ? tires.IndexOf(detachedTire) : -1,
            Cushion=cushionAxleLevel, Horse=horseLevel, Precision=precisionLevel,
            Loaded=loaded, Firing=fuseLit, Wheel=wheelDetached, EverWheel=wheelEverDetached };
        Failure.Value = new FixedString64Bytes(FailureReason);
    }
    private void Apply(CartNetworkState v)
    {
        flow=v.Flow; durability=v.Durability; princeAnxiety=v.Prince; princessAnxiety=v.Princess;
        stageStartDistance=v.StageStart; stageNumber=Mathf.Max(1,v.Stage); depositedShells=v.Stored; shellsInPile=v.Pile; lostShells=v.Lost;
        wheelRepairSteps=v.RepairSteps; cushionAxleLevel=v.Cushion; horseLevel=v.Horse; precisionLevel=v.Precision;
        loaded=v.Loaded; fuseLit=v.Firing; wheelDetached=v.Wheel; wheelEverDetached=v.EverWheel;
        for (int i = 0; i < tires.Count; i++)
        {
            if (missingWheelSprite != null) tires[i].sprite = mountedWheelSprite;
            var color = tires[i].color;
            color.a = tireAlphas[i];
            tires[i].color = color;
        }
        detachedTire=v.Tire>=0 && v.Tire<tires.Count ? tires[v.Tire] : null;
        if (detachedTire != null) SetTireAlpha(wheelRepairSteps==0 ? 0f : tireAlphas[v.Tire]);
        Refresh();
    }
}

using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// 마차 위 스테이션과 스테이지 상태.
// 말 강화, 왕족 달래기, 포탄 운반·장전·발사, 바퀴 수리 결과를 처리하고
// 내구도·왕족 불안·바퀴 빠짐·스테이지 완료(업그레이드 선택)·실패를 관리한다.
// 장애물은 ObstacleSpawner, 카메라 흔들림은 CameraRig, 화면 문구·경고는 StageHud가 맡는다.
public enum StageFlow : byte
{
    Waiting,
    Running,
    ChoosingUpgrade,
    Failed
}

public sealed class CartStations : NetworkBehaviour
{
    public const float MaxDurability = 100f;
    public const int WheelRepairStepsNeeded = 2;
    private const float HoldDuration = 1.6f;
    private const float StartPrinceAnxiety = 0.28f;
    private const float StartPrincessAnxiety = 0.42f;
    private const int StartShellsInPile = 17;
    private const int StartDepositedShells = 1;

    [SerializeField] private CartMotion cart;
    [SerializeField] private ObstacleSpawner obstacles;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private GameObject storedShell;
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private Transform cannonMuzzle;
    [SerializeField] private Image princeAnxietyBar;
    [SerializeField] private Image princessAnxietyBar;
    [SerializeField, Min(0f)] private float anxietyPerSecond = 0.015f;
    [SerializeField, Min(0f)] private float fuseDelay = 2.5f;
    [SerializeField, Min(0f)] private float projectileSpeed = 10f;

    [Header("Stage")]
    [SerializeField, Min(1)] private int stageNumber = 1;
    [SerializeField, Min(1)] private int baseStageGoal = 1000;
    [Tooltip("스테이지가 하나 오를 때마다 목표 거리 증가")]
    [SerializeField, Min(0)] private int stageGoalPerStage = 150;

    [Header("Royal family")]
    [Tooltip("이 값 이상이면 탈출 직전 상태 (달래기 효과 증가)")]
    [SerializeField, Range(0f, 1f)] private float escapingAnxiety = 0.75f;
    [SerializeField, Range(0f, 1f)] private float royalRelief = 0.42f;
    [SerializeField, Range(0f, 1f)] private float royalReliefEscaping = 0.63f;

    [Header("Impacts")]
    [SerializeField, Min(0f)] private float collisionAnxietyIncrease = 0.06f;
    [Tooltip("이 피해 이상이면 포탄을 든 플레이어가 버티기 판정")]
    [SerializeField, Min(0f)] private float shellHoldDamageThreshold = 6f;
    [SerializeField, Min(0f)] private float recoilAnxietyIncrease = 0.04f;
    [SerializeField, Min(0f)] private float impactShakeStrength = 0.3f;
    [SerializeField, Min(0f)] private float recoilShakeStrength = 0.2f;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.45f;
    [SerializeField, Min(0f)] private float aimJitterDegrees = 6f;

    [Header("Wheel")]
    [SerializeField, Range(0f, 1f)] private float wheelLossChance = 0.25f;
    [Tooltip("충돌로 바퀴가 빠질 수 있는 최소 진행 거리")]
    [SerializeField, Min(0f)] private float wheelLossMinDistance = 110f;
    [Tooltip("이 거리에서 한 번은 반드시 바퀴가 빠진다")]
    [SerializeField, Min(0f)] private float forcedWheelLossDistance = 155f;
    [SerializeField, Range(0f, 1f)] private float wheelLossSpeedMultiplier = 0.57f;

    [Header("Upgrades")]
    [SerializeField, Min(0)] private int cushionAxleLevel;
    [Tooltip("완충 차축 1레벨당 충격(불안·흔들림) 배율")]
    [SerializeField, Range(0f, 1f)] private float cushionImpactFactor = 0.78f;
    [Tooltip("정밀 조준 1레벨당 타이밍 판정 폭 증가 (원 둘레 비율)")]
    [SerializeField, Range(0f, 1f)] private float precisionPerLevel = 0.027f;

    private readonly List<SpriteRenderer> tires = new();
    private readonly List<float> tireAlphas = new();
    [SerializeField] private MultiplayerGameScene gameScene;
    public static CartStations Instance { get; private set; }
    public NetworkVariable<CartNetworkState> State = new();
    public NetworkVariable<FixedString64Bytes> Failure = new();
    // 게임 흐름: 시작 대기 → 진행 → 업그레이드 선택 또는 실패. 동시에 둘이 될 수 없어 enum 하나로 둔다.
    private StageFlow flow = StageFlow.Waiting;
    private NetworkPlayer gunner;
    public NetworkVariable<ulong> Gunner = new(ulong.MaxValue);
    // Indexed by StationAction. Only the server grants or releases a workstation.
    public NetworkList<ulong> StationOwners = new();
    private const int StationCount = 8;
    public const ulong NoStationOwner = ulong.MaxValue;
    public static bool Running => Instance != null && Instance.IsSpawned && Instance.flow == StageFlow.Running;
    private SpriteRenderer cartBody;
    private SpriteRenderer detachedTire;
    private float detachedTireAlpha;
    private float durability;
    private float princeAnxiety;
    private float princessAnxiety;
    private float stageStartDistance;
    private bool wheelDetached;
    private bool wheelEverDetached;
    private bool loaded;
    private bool fuseLit;
    private int wheelRepairSteps;
    private int depositedShells;
    private int shellsInPile;
    private int lostShells;
    private int horseLevel;
    private int precisionLevel;

    public CartMotion Cart => cart;
    public SpriteRenderer CartBody => cartBody;
    public bool Failed => flow == StageFlow.Failed;
    public string FailureReason { get; private set; } = "CART DESTROYED";
    public bool ChoosingUpgrade => flow == StageFlow.ChoosingUpgrade;
    public int StageNumber => stageNumber;
    public float StageDistance => Mathf.Max(0f, cart.Distance - stageStartDistance);
    public int StageGoal => baseStageGoal + (stageNumber - 1) * stageGoalPerStage;
    public float Durability => durability;
    public bool WheelDetached => wheelDetached;
    public SpriteRenderer DetachedTire => detachedTire;
    public int WheelRepairSteps => wheelRepairSteps;
    public float PrinceAnxiety => princeAnxiety;
    public float PrincessAnxiety => princessAnxiety;
    public bool PrinceEscaping => princeAnxiety >= escapingAnxiety;
    public bool PrincessEscaping => princessAnxiety >= escapingAnxiety;
    public int CushionAxleLevel => cushionAxleLevel;
    public float CushionImpactFactor => cushionImpactFactor;
    public int HorseLevel => horseLevel;
    public float SpeedPerHorseLevel => cart.SpeedPerHorseLevel;
    public int PrecisionLevel => precisionLevel;
    public float PrecisionPerLevel => precisionPerLevel;
    public float TimingBonusDegrees => precisionLevel * 360f * precisionPerLevel;
    public bool IsLoaded => loaded;
    public bool IsDeposited => depositedShells > 0;
    public int DepositedShells => depositedShells;
    public bool IsFiring => fuseLit;
    public RectTransform PrinceAnxietyUI => princeAnxietyBar != null ? princeAnxietyBar.transform.parent as RectTransform : null;
    public RectTransform PrincessAnxietyUI => princessAnxietyBar != null ? princessAnxietyBar.transform.parent as RectTransform : null;

    private void Awake()
    {
        Instance = this;
        foreach (SpriteRenderer renderer in cart.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.name == "CartBody") cartBody = renderer;
            else if (renderer.name.StartsWith("Tire ") && renderer.sprite != null)
            {
                tires.Add(renderer);
                tireAlphas.Add(renderer.color.a);
            }
        }
        ResetStageState();
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || flow == StageFlow.Waiting) return;
        ReconcileStations();
        switch (flow)
        {
            case StageFlow.Failed:
                if ((Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)) Restart();
                return;
            case StageFlow.ChoosingUpgrade:
                ReadUpgradeInput();
                return;
        }
        if (StageDistance >= StageGoal)
        {
            BeginUpgradeSelection();
            return;
        }
        if (!wheelEverDetached && StageDistance >= forcedWheelLossDistance) DetachWheel();
        princeAnxiety = Mathf.Clamp01(princeAnxiety + anxietyPerSecond * Time.deltaTime);
        princessAnxiety = Mathf.Clamp01(princessAnxiety + anxietyPerSecond * Time.deltaTime);
        if (princeAnxiety >= 1f || princessAnxiety >= 1f)
        {
            FailStage("PASSENGER ESCAPED");
            return;
        }
        Refresh();
    }

    private void ReadUpgradeInput()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) ChooseUpgrade(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) ChooseUpgrade(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) ChooseUpgrade(2);
        }
        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.wasPressedThisFrame) ChooseUpgrade(0);
            else if (Gamepad.current.buttonWest.wasPressedThisFrame) ChooseUpgrade(1);
            else if (Gamepad.current.buttonNorth.wasPressedThisFrame) ChooseUpgrade(2);
        }
    }

    public void Enter(StationAction action, NetworkPlayer actor)
    {
        if (!IsServer || action != StationAction.ShellDeposit || !actor.Carrying.Value) return;
        actor.Carrying.Value = false;
        depositedShells++;
        Refresh();
    }

    public void Succeed(StationAction action, NetworkPlayer actor)
    {
        if (!IsServer) return;
        switch (action)
        {
            case StationAction.RightHorse: cart.BoostHorse(true); break;
            case StationAction.LeftHorse: cart.BoostHorse(false); break;
            case StationAction.RoyalFamily: CalmRoyalFamily(); break;
            case StationAction.ShellPile:
                if (!actor.Carrying.Value && shellsInPile > 0) { shellsInPile--; actor.Carrying.Value = true; }
                break;
            case StationAction.Reload:
                if (depositedShells > 0 && !loaded && !fuseLit) { depositedShells--; loaded = true; }
                break;
            case StationAction.WheelRepair:
                if (!wheelDetached) break;
                wheelRepairSteps++;
                if (wheelRepairSteps == 1) SetTireAlpha(detachedTireAlpha);
                if (wheelRepairSteps >= WheelRepairStepsNeeded) RepairWheel();
                break;
        }
        Refresh();
    }

    // 둘 중 더 불안한 쪽을 달랜다. 탈출 직전이면 더 많이 내려간다.
    private void CalmRoyalFamily()
    {
        if (princeAnxiety >= princessAnxiety)
            princeAnxiety = Mathf.Clamp01(princeAnxiety - (PrinceEscaping ? royalReliefEscaping : royalRelief));
        else
            princessAnxiety = Mathf.Clamp01(princessAnxiety - (PrincessEscaping ? royalReliefEscaping : royalRelief));
    }

    public bool CanUse(StationAction action, NetworkPlayer actor)
    {
        if (!HoldsStation(action, actor)) return false;
        switch (action)
        {
            case StationAction.ShellPile: return !actor.Carrying.Value && shellsInPile > 0;
            case StationAction.Reload: return depositedShells > 0 && !loaded && !fuseLit;
            case StationAction.Cannon: return loaded && !fuseLit;
            case StationAction.ShellDeposit: return actor.Carrying.Value;
            case StationAction.WheelRepair: return wheelDetached;
            default: return true;
        }
    }

    public bool HoldsStation(StationAction action, NetworkPlayer actor)
    {
        int index = (int)action;
        return actor != null && actor.Assigned.Value && index >= 0 && index < StationOwners.Count
            && StationOwners[index] == actor.OwnerClientId;
    }

    public void ReserveStation(NetworkPlayer actor, int requested)
    {
        if (!IsServer || !actor.Assigned.Value) return;
        ReleaseStations(actor.OwnerClientId, requested);
        if (!Running || actor.Holding.Value || requested < 0 || requested >= StationCount) return;
        var action = (StationAction)requested;
        if (!actor.Timing.IsInActionArea(action)) return;
        if (StationOwners[requested] == NoStationOwner) StationOwners[requested] = actor.OwnerClientId;
    }

    public void ReleaseStations(ulong client, int keep = -1)
    {
        if (!IsServer) return;
        for (int i = 0; i < StationOwners.Count; i++)
        {
            if (StationOwners[i] != client || i == keep) continue;
            // A lit cannon belongs to its gunner until the shot finishes, even after departure.
            if (i == (int)StationAction.Cannon && fuseLit) continue;
            StationOwners[i] = NoStationOwner;
        }
    }

    private void ReconcileStations()
    {
        for (int i = 0; i < StationOwners.Count; i++)
        {
            ulong owner = StationOwners[i];
            if (owner == NoStationOwner || (i == (int)StationAction.Cannon && fuseLit)) continue;
            bool valid = false;
            if (Running)
                foreach (var actor in gameScene.Players)
                    if (actor.Assigned.Value && actor.OwnerClientId == owner && !actor.Holding.Value
                        && actor.Timing.IsInActionArea((StationAction)i)) { valid = true; break; }
            if (!valid) StationOwners[i] = NoStationOwner;
        }
    }

    private void ClearStations()
    {
        for (int i = 0; i < StationOwners.Count; i++) StationOwners[i] = NoStationOwner;
    }

    public void Fail(StationAction action)
    {
        if (!IsServer || action != StationAction.Reload || depositedShells <= 0 || loaded || fuseLit) return;
        depositedShells--;
        lostShells++;
        Refresh();
    }

    // 큰 충격(발사 반동, 장애물·몬스터 충돌) 때 포탄을 든 플레이어에게 버티기 판정을 건다.
    public void ApplyImpact()
    {
        if (!IsServer) return;
        foreach (var actor in gameScene.Players)
        {
            if (!actor.Assigned.Value || !actor.Carrying.Value) continue;
            actor.Holding.Value = true;
            actor.HoldDeadline.Value = NetworkManager.ServerTime.Time + HoldDuration;
        }
    }

    public void ResolveShellHold(NetworkPlayer actor, bool success)
    {
        if (!IsServer || !actor.Holding.Value) return;
        actor.Holding.Value = false;
        if (!success) { actor.Carrying.Value = false; lostShells++; }
        Refresh();
    }

    public void Ignite(NetworkPlayer actor)
    {
        if (!IsServer || !Running || !loaded || fuseLit || shellPrefab == null || cannonMuzzle == null) return;
        gunner = actor;
        Gunner.Value = actor.OwnerClientId;
        StartCoroutine(Fire());
    }
    private IEnumerator Fire()
    {
        fuseLit = true;
        float remaining = fuseDelay;
        while (remaining > 0f)
        {
            yield return null;
            if (Running) remaining -= Time.deltaTime;
        }
        loaded = false;
        fuseLit = false;
        Gunner.Value = ulong.MaxValue;
        StationOwners[(int)StationAction.Cannon] = NoStationOwner;
        ApplyImpact();
        AddImpactEffects(recoilAnxietyIncrease, recoilShakeStrength);
        GameObject shot = Instantiate(shellPrefab, cannonMuzzle.position, cannonMuzzle.rotation);
        shot.GetComponent<NetworkObject>().Spawn(true);
        if (shot.TryGetComponent(out Projectile projectile))
            projectile.Launch(gunner != null ? (Vector3)gunner.Aim.Value : cannonMuzzle.right, projectileSpeed);
        Refresh();
    }

    // 장애물·몬스터 공격이 마차에 닿았을 때.
    public void ReceiveCollision(float damage)
    {
        if (!IsServer || !Running) return;
        durability = Mathf.Max(0f, durability - Mathf.Max(0f, damage));
        if (damage >= shellHoldDamageThreshold) ApplyImpact();
        AddImpactEffects(collisionAnxietyIncrease, impactShakeStrength);
        if (StageDistance >= wheelLossMinDistance && !wheelDetached && Random.value < wheelLossChance) DetachWheel();
        if (durability <= 0f) FailStage("CART DESTROYED");
    }

    private void FailStage(string reason)
    {
        if (flow != StageFlow.Running) return;
        FailureReason = reason;
        flow = StageFlow.Failed;
        Publish();
    }

    private void AddImpactEffects(float anxiety, float shake)
    {
        float multiplier = Mathf.Pow(cushionImpactFactor, cushionAxleLevel);
        princeAnxiety = Mathf.Clamp01(princeAnxiety + anxiety * multiplier);
        princessAnxiety = Mathf.Clamp01(princessAnxiety + anxiety * multiplier);
        ImpactRpc(shake * multiplier, aimJitterDegrees * multiplier, shakeDuration);
    }

    private void SetTireAlpha(float alpha)
    {
        if (detachedTire == null) return;
        Color color = detachedTire.color;
        color.a = alpha;
        detachedTire.color = color;
    }

    public bool IsInsideDetachedTire(Vector3 position)
    {
        if (!wheelDetached || detachedTire == null || detachedTire.sprite == null) return false;
        Vector3 local = detachedTire.transform.InverseTransformPoint(position);
        Bounds spriteBounds = detachedTire.sprite.bounds;
        return local.x >= spriteBounds.min.x && local.x <= spriteBounds.max.x
            && local.y >= spriteBounds.min.y && local.y <= spriteBounds.max.y;
    }

    private void DetachWheel()
    {
        if (wheelDetached || tires.Count == 0) return;
        detachedTire = tires[Random.Range(0, tires.Count)];
        detachedTireAlpha = detachedTire.color.a;
        wheelDetached = true;
        wheelEverDetached = true;
        wheelRepairSteps = 0;
        SetTireAlpha(0f);
        cart.SpeedMultiplier = wheelLossSpeedMultiplier;
        Publish();
    }

    private void RepairWheel()
    {
        SetTireAlpha(detachedTireAlpha);
        detachedTire = null;
        wheelDetached = false;
        cart.SpeedMultiplier = 1f;
        Publish();
    }

    private void BeginUpgradeSelection()
    {
        flow = StageFlow.ChoosingUpgrade;
        Publish();
    }

    public void ChooseUpgrade(int upgrade)
    {
        if (!IsHost || flow != StageFlow.ChoosingUpgrade || upgrade < 0 || upgrade > 2) return;
        switch (upgrade)
        {
            case 0: cushionAxleLevel++; break;
            case 1: horseLevel++; break;
            case 2: precisionLevel++; break;
        }
        StopAllCoroutines();
        Gunner.Value = ulong.MaxValue;
        ClearStations();
        foreach (var actor in gameScene.Players) { actor.Carrying.Value = false; actor.Holding.Value = false; }
        if (wheelDetached) RepairWheel();
        stageNumber++;
        stageStartDistance = cart.Distance;
        cart.ResetForStage(horseLevel);
        ResetStageState();
        obstacles.ResetForStage();
        flow = StageFlow.Running;
        Refresh();
    }

    private void ResetStageState()
    {
        durability = MaxDurability;
        princeAnxiety = StartPrinceAnxiety;
        princessAnxiety = StartPrincessAnxiety;
        depositedShells = StartDepositedShells;
        loaded = true;
        shellsInPile = StartShellsInPile;
        lostShells = 0;
        fuseLit = false;
        wheelEverDetached = false;
        wheelRepairSteps = 0;
        Refresh();
    }

    // Keep networking alive while NGO reloads the same authored scene on every peer.
    public void Restart()
    {
        if (!IsHost || flow != StageFlow.Failed) return;
        flow = StageFlow.Waiting;
        Publish();
        // 솔로 테스트(로비 없음)에서는 재시작 불가 → Failed 유지, Play를 다시 누른다.
        var lobby = FindFirstObjectByType<LobbySession>();
        if (lobby == null || !lobby.ReloadGame()) { flow = StageFlow.Failed; Publish(); }
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    public void Begin()
    {
        if (!IsServer) return;
        flow = StageFlow.Running;
        obstacles.ResetForStage();
        Publish();
    }
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
            var color = tires[i].color;
            color.a = tireAlphas[i];
            tires[i].color = color;
        }
        detachedTire=v.Tire>=0 && v.Tire<tires.Count ? tires[v.Tire] : null;
        if (detachedTire != null) SetTireAlpha(wheelRepairSteps==0 ? 0f : tireAlphas[v.Tire]);
        Refresh();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void ImpactRpc(float shake, float aim, float duration)
    {
        cameraRig.Shake(shake,duration);
        if (NetworkPlayer.LocalPlayer != null) NetworkPlayer.LocalPlayer.Cannon.ApplyImpact(aim,duration);
    }
    private void Refresh()
    {
        Publish();
        if (storedShell != null) storedShell.SetActive(depositedShells > 0);
        if (princeAnxietyBar != null) princeAnxietyBar.fillAmount = princeAnxiety;
        if (princessAnxietyBar != null) princessAnxietyBar.fillAmount = princessAnxiety;
    }
}

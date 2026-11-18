using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
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
    Failed,
    Complete
}

public struct CartWorkShock : INetworkSerializeByMemcpy, System.IEquatable<CartWorkShock>
{
    public double Time;
    public float Strength, Phase;

    public readonly bool Equals(CartWorkShock other) =>
        Time == other.Time && Strength == other.Strength && Phase == other.Phase;
}

public sealed partial class CartStations : NetworkBehaviour
{
    public const float MaxDurability = 100f;
    public const int WheelRepairStepsNeeded = 2;
    [SerializeField] private ParticleSystem impactDust;
    [SerializeField] private AudioSource impactAudio;
    [SerializeField] private AudioClip cannonSound, hitSound;
    [SerializeField] private CameraRig secondCamera;
    private uint impactSequence, lastPlayedImpact;


    private const int StartShellsInPile = 17;
    private const int StartDepositedShells = 1;
    private const float ObstacleShock = .98f;
    private const float PursuerShock = 1.32f;
    private const float CatapultShock = 1.42f;
    private const float WheelShock = 1.2f;
    private const float CannonShock = 1.45f;
    private const float ShakeDecayPerSecond = 1.05f;
    private const float ShellSlipThreshold = .4f;

    [SerializeField] private CartMotion cart;
    [SerializeField] private ObstacleSpawner obstacles;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private GameObject storedShell;
    [SerializeField] private TMPro.TMP_Text rackCountLabel;
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private Transform cannonMuzzle;
    [SerializeField] private SpriteRenderer cannonVisual;
    [SerializeField] private Sprite cannonReady, cannonFuse;
    [SerializeField] private SpriteRenderer muzzleVisual;
    [SerializeField] private SpriteSequence muzzleFlash;
    [SerializeField] private Sprite looseWheelSprite;
    [SerializeField] private Sprite missingWheelSprite;
    private Sprite mountedWheelSprite;
    private float muzzleFlashStart = float.NegativeInfinity;

    [Header("Stage")]
    [SerializeField, Min(1)] private int stageNumber = 1;
    [SerializeField, Min(1)] private int baseStageGoal = 1000;

    [Header("Royal family")]
    [SerializeField] private bool failOnPassengerEscape;
    [Tooltip("이 값 이상이면 탈출 직전 상태 (달래기 효과 증가)")]
    [SerializeField, Range(0f, 1f)] private float escapingAnxiety = 0.75f;

    [Header("Impacts")]
    [SerializeField, Min(0f)] private float collisionAnxietyIncrease = 0.06f;
    [SerializeField, Min(0f)] private float recoilAnxietyIncrease = 0.04f;
    [SerializeField, Min(0f)] private float impactShakeStrength = 0.3f;
    [SerializeField, Min(0f)] private float recoilShakeStrength = 0.2f;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.45f;

    [Header("Wheel")]
    [SerializeField, Range(0f, 1f)] private float wheelLossChance = 0.25f;
    [Tooltip("충돌로 바퀴가 빠질 수 있는 최소 진행 거리")]
    [SerializeField, Min(0f)] private float wheelLossMinDistance = 110f;
    [Tooltip("이 거리에서 한 번은 반드시 바퀴가 빠진다")]
    [SerializeField, Min(0f)] private float forcedWheelLossDistance = 155f;
    [SerializeField, Range(0f, 1f)] private float wheelLossSpeedMultiplier = 0.57f;

    [Header("Upgrades")]
    [SerializeField, Min(0)] private int cushionAxleLevel;

    private readonly List<SpriteRenderer> tires = new();
    private readonly List<float> tireAlphas = new();
    [SerializeField] private MultiplayerGameScene gameScene;
    public static CartStations Instance { get; private set; }
    public NetworkVariable<CartNetworkState> State = new();
    public NetworkVariable<FixedString64Bytes> Failure = new();
    // 게임 흐름: 시작 대기 → 진행 → 업그레이드 선택 또는 실패. 동시에 둘이 될 수 없어 enum 하나로 둔다.
    private StageFlow flow = StageFlow.Waiting;
    private NetworkPlayer gunner;
    public NetworkVariable<double> FuseDeadline = new();
    public NetworkVariable<ulong> Gunner = new(ulong.MaxValue);
    // Indexed by StationAction. Only the server grants or releases a workstation.
    public NetworkList<ulong> StationOwners = new();
    private const int StationCount = (int)StationAction.Stairs;
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
    private float pendingCookingShock;
    private readonly NetworkVariable<CartWorkShock> workShock = new();
    private int wheelRepairSteps;
    private int depositedShells;
    private int shellsInPile;
    private int lostShells;
    private int horseLevel;
    private int precisionLevel;

    public CartMotion Cart => cart;
    // The reference deck spans 331 units. Use the authored deck width for world-space gameplay distances.
    public float WorldPerWebUnit => cartBody.bounds.size.x / 331f;
    public Vector3 RingOffset(NetworkPlayer player) =>
        player.SecondLocal && secondCamera != null && secondCamera.CameraActive ? secondCamera.ShakeOffset : cameraRig.ShakeOffset;
    public SpriteRenderer CartBody => cartBody;
    public bool Failed => flow == StageFlow.Failed;
    public string FailureReason { get; private set; } = "CART DESTROYED";
    public bool ChoosingUpgrade => flow == StageFlow.ChoosingUpgrade;
    public int StageNumber => stageNumber;
    public float StageDistance => Mathf.Max(0f, cart.Distance - stageStartDistance);
    public int StageGoal => baseStageGoal;
    public float Durability => durability;
    public bool WheelDetached => wheelDetached;
    public SpriteRenderer DetachedTire => detachedTire;
    public int WheelRepairSteps => wheelRepairSteps;
    public float PrinceAnxiety => princeAnxiety;
    public float PrincessAnxiety => princessAnxiety;
    public bool PrinceEscaping => princeAnxiety >= escapingAnxiety;
    public bool PrincessEscaping => princessAnxiety >= escapingAnxiety;
    public int CushionAxleLevel => cushionAxleLevel;
    public float CushionImpactFactor => CartRules.Active.cushion.amount;
    public int HorseLevel => horseLevel;
    public float SpeedPerHorseLevel => cart.SpeedPerHorseLevel;
    public int PrecisionLevel => precisionLevel;
    public float PrecisionPerLevel => CartRules.Active.precision.amount;
    public float TimingBonusDegrees => precisionLevel * 360f * PrecisionPerLevel;
    public NetworkVariable<bool> Endless = new();
    public bool IsLoaded => loaded;
    public bool IsDeposited => depositedShells > 0;
    public int DepositedShells => depositedShells;
    public bool IsFiring => fuseLit;

    private void Awake()
    {
        Instance = this;
        for (int i = 0; royalVisuals != null && i < Mathf.Min(royalVisuals.Length, seats.Length); i++)
            seats[i] = cart.transform.InverseTransformPoint(royalVisuals[i].position);
        foreach (SpriteRenderer renderer in cart.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.name == "CartBody") cartBody = renderer;
            else if (renderer.name.StartsWith("Tire ") && renderer.sprite != null)
            {
                tires.Add(renderer);
                tireAlphas.Add(renderer.color.a);
                if (mountedWheelSprite == null) mountedWheelSprite = renderer.sprite;
            }
        }
        ResetStageState();
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || flow == StageFlow.Waiting) return;
        if (flow == StageFlow.Complete) return;
        ReconcileStations();
        switch (flow)
        {
            case StageFlow.Failed:
                if ((Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)) Restart();
                return;
            case StageFlow.ChoosingUpgrade:
                return;
        }
        accumulator += Time.deltaTime;
        while (accumulator >= Step && Running)
        {
            accumulator -= Step;
            AdvanceGame();
        }
    }

    public static event System.Action<float> SimulationStep;
    private void AdvanceGame()
    {
        AdvanceBots();
        cart.Advance(Step);
        if (!wheelEverDetached && StageDistance >= forcedWheelLossDistance) DetachWheel();
        AdvanceFuse();
        waveSpawner.Advance(Step);
        Waves.Value = waveSpawner.SpawnedWaves;
        SimulationStep?.Invoke(Step);
        AdvanceWheels();
        if (!Running) return;
        AdvanceCare();
        if (Endless.Value && shellsInPile == 0) shellsInPile = StartShellsInPile;
        if (!Endless.Value && StageDistance >= StageGoal)
        {
            BeginUpgradeSelection();
            return;
        }
        princeAnxiety = 1f - Prince.Value.Attention / 100f;
        princessAnxiety = 1f - Princess.Value.Attention / 100f;
        Refresh();
    }

    public void Enter(StationAction action, NetworkPlayer actor)
    {
        if (!IsServer || action != StationAction.ShellDeposit || !actor.Carrying.Value) return;
        actor.Carrying.Value = false;
        depositedShells++;
        CompleteStation(action, actor);
        Refresh();
    }

    public void Succeed(StationAction action, NetworkPlayer actor)
    {
        if (!IsServer) return;
        switch (action)
        {
            case StationAction.RoyalFamily: CareFor(Prince, actor, 0); break;
            case StationAction.Princess: CareFor(Princess, actor, 1); break;
            case StationAction.WheelRack:
                actor.SpareWheel.Value = true;
                CompleteStation(action, actor);
                break;
            case StationAction.ShellPile:
                if (actor.Carrying.Value || shellsInPile <= 0) break;
                shellsInPile--;
                actor.Carrying.Value = true;
                CompleteStation(action, actor);
                break;
            case StationAction.Reload:
                if (depositedShells <= 0 || loaded || fuseLit) break;
                depositedShells--;
                loaded = true;
                CompleteStation(action, actor);
                actor.ShowWorkResult(true);
                break;
            case StationAction.WheelRepair:
                if (!wheelDetached || (wheelRepairSteps == 0 && !actor.SpareWheel.Value)) break;
                if (wheelRepairSteps == 0) actor.SpareWheel.Value = false;
                wheelRepairSteps++;
                if (wheelRepairSteps == 1) SetTireAlpha(detachedTireAlpha);
                if (wheelRepairSteps >= WheelRepairStepsNeeded)
                {
                    CompleteStation(action, actor);
                    RepairWheel();
                }
                break;
        }
        Refresh();
    }

    public bool CanUse(StationAction action, NetworkPlayer actor)
    {
        if (TimingArea.IsRoyal(action)) return AvailableCare(action, actor) != CareAction.None;
        return HoldsStation(action, actor) && HasWork(action, actor);
    }

    public bool HasWork(StationAction action, NetworkPlayer actor)
    {
        switch (action)
        {
            case StationAction.ShellPile: return actor.HandsFree && shellsInPile > 0;
            case StationAction.Kitchen: return actor.HandsFree;
            case StationAction.WheelRack: return wheelDetached && wheelRepairSteps == 0 && actor.HandsFree;
            case StationAction.Reload: return depositedShells > 0 && !loaded && !fuseLit;
            case StationAction.Cannon: return loaded && !fuseLit;
            case StationAction.ShellDeposit: return actor.Carrying.Value;
            case StationAction.WheelRepair: return wheelDetached && (wheelRepairSteps > 0 || actor.SpareWheel.Value);
            default: return true;
        }
    }

    public bool HoldsStation(StationAction action, NetworkPlayer actor)
    {
        int index = (int)action;
        return actor != null && actor.Assigned.Value && index >= 0 && index < StationOwners.Count
            && StationOwners[index] == actor.NetworkObjectId;
    }

    private void CompleteStation(StationAction action, NetworkPlayer actor)
    {
        if (!HoldsStation(action, actor)) return;
        if (actor.Timing.IsInActionArea(action)) actor.CompletedStation.Value = (int)action;
        StationOwners[(int)action] = NoStationOwner;
    }

    public void ReserveStation(NetworkPlayer actor, int requested)
    {
        if (!IsServer || !actor.Assigned.Value) return;
        ReleaseStations(actor.NetworkObjectId, requested);
        if (!Running || actor.Holding.Value || requested < 0 || requested >= StationCount) return;
        var action = (StationAction)requested;
        if (actor.CompletedStation.Value == requested || TimingArea.IsRoyal(action) || !HasWork(action, actor) || !actor.Timing.IsInActionArea(action)) return;
        if (!actor.Bot.Value && StationOwners[requested] != NoStationOwner &&
            !(action == StationAction.Cannon && fuseLit))
        {
            foreach (var other in gameScene.Players)
                if (other.Bot.Value && StationOwners[requested] == other.NetworkObjectId)
                    ReleaseStations(other.NetworkObjectId);
        }
        if (StationOwners[requested] == NoStationOwner) StationOwners[requested] = actor.NetworkObjectId;
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
                    if (actor.Assigned.Value && actor.NetworkObjectId == owner && !actor.Holding.Value
                        && HasWork((StationAction)i, actor) && actor.Timing.IsInActionArea((StationAction)i)) { valid = true; break; }
            if (!valid) StationOwners[i] = NoStationOwner;
        }
    }

    private void ClearStations()
    {
        for (int i = 0; i < StationOwners.Count; i++) StationOwners[i] = NoStationOwner;
    }

    public void Fail(StationAction action, NetworkPlayer actor)
    {
        if (!IsServer || action != StationAction.Reload || depositedShells <= 0 || loaded || fuseLit) return;
        depositedShells--;
        lostShells++;
        CompleteStation(action, actor);
        actor.ShowWorkResult(false);
        Refresh();
    }

    private void StartShellCatch(double time)
    {
        foreach (var actor in gameScene.Players)
        {
            if (!actor.Assigned.Value || !actor.Carrying.Value || actor.Holding.Value || actor.ShellSafeUntil > time) continue;
            actor.HoldDeadline.Value = time + CartRules.Active.shellHoldSeconds;
            actor.Holding.Value = true;
        }
    }

    public void ResolveShellHold(NetworkPlayer actor, bool success)
    {
        if (!IsServer || !actor.Holding.Value) return;
        actor.Holding.Value = false;
        if (success) actor.ShellSafeUntil = NetworkManager.ServerTime.Time + 1d;
        else { actor.Carrying.Value = false; lostShells++; }
        Refresh();
    }

    public void Ignite(NetworkPlayer actor)
    {
        if (!IsServer || !Running || !loaded || fuseLit || shellPrefab == null || cannonMuzzle == null) return;
        gunner = actor;
        Gunner.Value = actor.NetworkObjectId;
        StartFuse();
    }
    private float fuseRemaining;
    private void StartFuse(float initial = -1f)
    {
        fuseLit = true;
        fuseRemaining = initial < 0f ? CartRules.Active.fuseSeconds : initial;
        FuseDeadline.Value = NetworkManager.ServerTime.Time + fuseRemaining;
    }

    private void AdvanceFuse()
    {
        if (!fuseLit) return;
        fuseRemaining -= Step;
        if (fuseRemaining > 0.00001f) return;
        var target = CannonAim.AimPoint(gunner != null ? gunner.Aim.Value : Vector2.zero, cannonMuzzle.position,
            this, NetworkManager.ServerTime.Time, out var spread, out _);
        // Isolate shot scatter from child, horse and wave random rolls.
        uint seed = randomState ^ unchecked((uint)(careTicks+1)*1597334677u);
        seed = unchecked(seed*1664525u+1013904223u);
        float angle = 2f*Mathf.PI*(seed/4294967296f);
        seed = unchecked(seed*1664525u+1013904223u);
        float radius = Mathf.Sqrt(seed/4294967296f);
        target += Vector2.Scale(spread, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))) * radius;
        loaded = false;
        fuseLit = false;
        Gunner.Value = ulong.MaxValue;
        CompleteStation(StationAction.Cannon, gunner);
        StationOwners[(int)StationAction.Cannon] = NoStationOwner;
        AddImpactEffects(recoilAnxietyIncrease, recoilShakeStrength, CannonShock, .25f, cannon: true);
        Vector2 direction = target - (Vector2)cannonMuzzle.position;
        var rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        GameObject shot = Instantiate(shellPrefab, cannonMuzzle.position, rotation);
        shot.GetComponent<Projectile>().SetCaliber(Barrel.Value);
        shot.GetComponent<NetworkObject>().Spawn(true);
        if (shot.TryGetComponent(out Projectile projectile))
            projectile.Launch(target, 1750f * WorldPerWebUnit);
        Refresh();
    }

    // 장애물·몬스터 공격이 마차에 닿았을 때.
    public void ReceiveCollision(float damage) => ReceiveImpact(damage, ObstacleShock, .34f);
    public void ReceiveEnemyHit(float damage) => ReceiveImpact(damage, PursuerShock, 0f);
    public void ReceiveCatapultHit(float damage) => ReceiveImpact(damage, CatapultShock, .52f);

    private void ReceiveImpact(float damage, float strength, float fallChance)
    {
        if (!IsServer || !Running) return;
        if (!DevInvincible) durability = Mathf.Max(Endless.Value ? 1f : 0f, durability - Mathf.Max(0f, damage));
        AddImpactEffects(collisionAnxietyIncrease, impactShakeStrength, strength, fallChance);
        if (StageDistance >= wheelLossMinDistance && !wheelDetached && Sample() < wheelLossChance) DetachWheel();
        if (durability <= 0f) FailStage("CART DESTROYED");
    }

    private void FailStage(string reason)
    {
        if (flow != StageFlow.Running) return;
        FailureReason = reason;
        flow = StageFlow.Failed;
        Publish();
    }

    private void AddImpactEffects(float anxiety, float cameraStrength, float workStrength, float fallChance, bool cannon = false)
    {
        float multiplier = Mathf.Pow(CushionImpactFactor, cushionAxleLevel);
        princeAnxiety = Mathf.Clamp01(princeAnxiety + anxiety * multiplier);
        princessAnxiety = Mathf.Clamp01(princessAnxiety + anxiety * multiplier);
        if (fallChance > 0f && randomState != 0)
        {
            KnockDown(Prince, fallChance * multiplier);
            KnockDown(Princess, fallChance * multiplier);
        }

        float strength = Mathf.Min(CannonShock, workStrength * multiplier);
        // A hit and its wheel detachment can happen in the same simulation step.
        pendingCookingShock = Mathf.Max(pendingCookingShock, strength);
        double time = NetworkManager.ServerTime.Time;
        uint sequence = ++impactSequence;
        workShock.Value = new CartWorkShock
        {
            Time = time,
            Strength = strength,
            Phase = Mathf.Repeat(sequence * 2.3999632f, 2f * Mathf.PI)
        };
        if (strength > ShellSlipThreshold) StartShellCatch(time);
        ImpactRpc(sequence, cannon, cameraStrength * multiplier, shakeDuration);
    }

    internal float WorkShakeAt(double time, out float phase)
    {
        var shock = workShock.Value;
        phase = shock.Phase;
        double elapsed = System.Math.Max(0d, time - shock.Time);
        float steppedElapsed = (float)(System.Math.Floor(elapsed * 30d + 1e-6d) / 30d);
        return Mathf.Max(0f, shock.Strength - steppedElapsed * ShakeDecayPerSecond);
    }

    private void SetTireAlpha(float alpha)
    {
        if (detachedTire == null) return;
        Color color = detachedTire.color;
        if (missingWheelSprite != null)
        {
            bool missing = alpha <= 0f;
            detachedTire.sprite = missing ? missingWheelSprite : mountedWheelSprite;
            alpha = missing ? tireAlphas[tires.IndexOf(detachedTire)] : alpha;
        }
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
        detachedTire = tires[(int)(Sample()*tires.Count)];
        detachedTireAlpha = detachedTire.color.a;
        LaunchWheel(detachedTire);
        wheelDetached = true;
        wheelEverDetached = true;
        wheelRepairSteps = 0;
        SetTireAlpha(0f);
        cart.SpeedMultiplier = wheelLossSpeedMultiplier;
        AddImpactEffects(0f, impactShakeStrength, WheelShock, 0f);
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
        FinishCare();
        flow = stageNumber == 3 ? StageFlow.Complete : StageFlow.ChoosingUpgrade;
        Publish();
    }

    public void ChooseUpgrade(int upgrade)
    {
        if (NetworkPlayer.LocalPlayer != null) NetworkPlayer.LocalPlayer.SubmitChoice(upgrade);
    }

    private void NextStage()
    {
        StopAllCoroutines();
        Gunner.Value = ulong.MaxValue;
        ClearStations();
        foreach (var actor in gameScene.Players)
        {
            actor.Carrying.Value = false;
            actor.Holding.Value = false;
            actor.ShellSafeUntil = 0d;
            actor.SpareWheel.Value = false;
            actor.Snack.Value = false;
            actor.CookProgress.Value = 0f;
            actor.CompletedStation.Value = -1;
        }
        ClearTransientObjects();
        stageNumber++;
        stageStartDistance = cart.Distance;
        cart.ResetForStage(horseLevel);
        if (wheelDetached) cart.SpeedMultiplier = wheelLossSpeedMultiplier;
        ResetCare();
        ResetStageState();
        obstacles.ResetForStage();
        flow = StageFlow.Running;
        waveSpawner.RestartWaves();
        Refresh();
        RememberStageEntry();
    }

    private void ResetStageState()
    {
        pendingCookingShock = 0f;
        if (IsSpawned && IsServer) workShock.Value = default;
        if (stageNumber == 1) durability = MaxDurability;
        princeAnxiety = princessAnxiety = 0;
        depositedShells = StartDepositedShells;
        loaded = true;
        shellsInPile = StartShellsInPile;
        lostShells = 0;
        fuseLit = false;
        wheelEverDetached = wheelDetached;
        if (!wheelDetached) wheelRepairSteps = 0;
        Refresh();
    }

    // Keep networking alive while NGO reloads the same authored scene on every peer.
    public void Restart()
    {
        if (!IsHost || (flow != StageFlow.Failed && flow != StageFlow.Complete)) return;
        bool replayChapter = flow == StageFlow.Complete;
        var lobby = FindFirstObjectByType<LobbySession>();
        if (lobby != null)
        {
            if (!lobby.ReloadGame(replayChapter ? null : stageEntryJson)) return;
            flow = StageFlow.Waiting;
            Publish();
        }
        else if (!string.IsNullOrEmpty(stageEntryJson))
        {
            ClearTransientObjects();
            ClearStations();
            obstacles.ResetForStage();
            Restore(JsonUtility.FromJson<RunCheckpoint>(replayChapter ? chapterEntryJson : stageEntryJson));
        }
    }

    private string stageEntryJson, chapterEntryJson;
    private void RememberStageEntry()
    {
        stageEntryJson = null;
        stageEntryJson = JsonUtility.ToJson(Capture(null));
        if (stageNumber == 1) chapterEntryJson = stageEntryJson;
    }
    private void ClearTransientObjects()
    {
        LooseWheels.Clear();
        waveSpawner.ClearEnemies();
        foreach (var shot in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            if (shot.IsSpawned) shot.NetworkObject.Despawn(true);
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    public void Begin()
    {
        if (!IsServer) return;
        ResetCare();
        gameScene.FillEmptySeats();
        Endless.Value = LobbySession.LocalSeats > 0 && LobbySession.PracticeTest;
        flow = StageFlow.Running;
        obstacles.ResetForStage();
        Publish();
        RememberStageEntry();
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
    [Rpc(SendTo.ClientsAndHost)]
    private void ImpactRpc(uint sequence, bool cannon, float shake, float duration)
    {
        if (sequence == lastPlayedImpact) return;
        lastPlayedImpact = sequence;
        cameraRig.Shake(shake, duration);
        if (secondCamera != null) secondCamera.Shake(shake, duration);
        if (impactDust != null) impactDust.Emit(cannon ? 18 : 12);
        if (cannon && muzzleVisual != null) muzzleFlashStart = Time.time;
        var clip = cannon ? cannonSound : hitSound;
        if (impactAudio != null && clip != null) impactAudio.PlayOneShot(clip);
    }
    // 개발자 패널(F1) 전용. 서버에서만 동작한다.
    public bool DevInvincible { get; set; }
    public void DevRepair() { if (!IsServer) return; durability = Capacity; if (wheelDetached) RepairWheel(); Refresh(); }
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
    public void DevFail() { if (IsServer) FailStage("DEV FAIL"); }

    private void Refresh()
    {
        Publish();
        if (storedShell != null) storedShell.SetActive(depositedShells > 0);
        if (rackCountLabel != null) rackCountLabel.SetText("적재 {0}발", depositedShells);
    }
}

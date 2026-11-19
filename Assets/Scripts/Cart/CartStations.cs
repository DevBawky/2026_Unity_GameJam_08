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
    [SerializeField] private AudioSource music;
    [SerializeField] private AudioClip[] stageMusic;
    [SerializeField] private CameraRig secondCamera;
    private uint impactSequence, lastPlayedImpact;

    private const int StartShellsInPile = 17;
    private const int StartDepositedShells = 3;
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
    [Tooltip("빠져서 굴러가는 바퀴 그림 프리팹")]
    [SerializeField] private SpriteRenderer looseWheelPrefab;
    [SerializeField] private Sprite missingWheelSprite;
    private float muzzleFlashStart = float.NegativeInfinity;

    [Header("Stage")]
    [SerializeField, Min(1)] private int stageNumber = 1;
    [SerializeField, Min(1)] private int baseStageGoal = 1000;

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
    private readonly List<Sprite> tireSprites = new();
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
    private const int StationCount = (int)StationAction.CannonCleaning + 1;
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
                tireSprites.Add(renderer.sprite);
            }
        }
        ResetStageState();
    }

    private void Update()
    {
        UpdateMusic();
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

    private void UpdateMusic()
    {
        if (music == null || stageMusic == null || stageMusic.Length == 0) return;
        if (!IsSpawned || flow is not (StageFlow.Running or StageFlow.ChoosingUpgrade))
        {
            if (music.isPlaying) music.Stop();
            return;
        }
        AudioClip track = stageMusic[(stageNumber - 1) % stageMusic.Length];
        if (music.clip == track && music.isPlaying) return;
        music.clip = track;
        music.Play();
    }

    public static event System.Action<float> SimulationStep;
    private void AdvanceGame()
    {
        foreach (var player in gameScene.Players)
            if (player.Assigned.Value && player.Carrying.Value && !player.Holding.Value
                && player.Timing.IsInActionArea(StationAction.ShellDeposit)) Enter(StationAction.ShellDeposit, player);
        AdvanceBots();
        cart.Advance(Step);
        if (obstacles != null) obstacles.Advance(Step);
        if (!Running) return;
        RingCallBell();
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

    private void FailStage(string reason)
    {
        if (flow != StageFlow.Running) return;
        FailureReason = reason;
        flow = StageFlow.Failed;
        Publish();
    }

    private void BeginUpgradeSelection()
    {
        FinishCare();
        flow = stageNumber == 3 ? StageFlow.Complete : StageFlow.ChoosingUpgrade;
        if (flow == StageFlow.ChoosingUpgrade) RollShopOffer();
        Publish();
    }

    public void ChooseUpgrade(int upgrade)
    {
        if (NetworkPlayer.LocalPlayer != null) NetworkPlayer.LocalPlayer.SubmitChoice(upgrade);
    }

    private void NextStage()
    {
        StopAllCoroutines();
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
        if (wheelDetached) cart.SpeedMultiplier = WheelLossSpeed;
        ResetCare();
        ResetStageState();
        ApplyStageItems();
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
        loaded = false;
        cleaningTaps = 0;
        shellsInPile = StartShellsInPile;
        lostShells = 0;
        fuseLit = false;
        if (IsSpawned && IsServer)
        {
            ResetGunner();
            LastCannonShot.Value = new CannonShotVisual { Time = -1d };
            WheelMount.Value = default;
        }
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

    private void Refresh()
    {
        Publish();
        if (storedShell != null) storedShell.SetActive(depositedShells > 0);
        if (rackCountLabel != null) rackCountLabel.SetText(LocalizationTable.T("적재 {0}발"), depositedShells);
    }
}

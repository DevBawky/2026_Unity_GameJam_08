using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// 마차 위 스테이션과 스테이지 상태.
// 말 강화, 왕족 달래기, 포탄 운반·장전·발사, 바퀴 수리 결과를 처리하고
// 내구도·왕족 불안·바퀴 빠짐·스테이지 완료(업그레이드 선택)·실패를 관리한다.
// 장애물은 ObstacleSpawner, 카메라 흔들림은 CameraRig, 화면 문구·경고는 StageHud가 맡는다.
public sealed class CartStations : MonoBehaviour
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
    [SerializeField] private GameObject carriedShell;
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
    private PlayerTiming playerTiming;
    private CannonAim cannonAim;
    private SpriteRenderer cartBody;
    private SpriteRenderer detachedTire;
    private float detachedTireAlpha;
    private float durability;
    private float princeAnxiety;
    private float princessAnxiety;
    private float stageStartDistance;
    private float holdTimeRemaining;
    private bool failed;
    private bool choosingUpgrade;
    private bool wheelDetached;
    private bool wheelEverDetached;
    private bool carrying;
    private bool loaded;
    private bool holdingShell;
    private bool fuseLit;
    private int wheelRepairSteps;
    private int depositedShells;
    private int shellsInPile;
    private int lostShells;
    private int horseLevel;
    private int precisionLevel;

    public CartMotion Cart => cart;
    public SpriteRenderer CartBody => cartBody;
    public bool Failed => failed;
    public string FailureReason { get; private set; } = "CART DESTROYED";
    public bool ChoosingUpgrade => choosingUpgrade;
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
    public bool IsCarrying => carrying;
    public bool IsHoldingShell => holdingShell;
    public bool IsLoaded => loaded;
    public bool IsDeposited => depositedShells > 0;
    public int DepositedShells => depositedShells;
    public bool IsFiring => fuseLit;
    public RectTransform PrinceAnxietyUI => princeAnxietyBar != null ? princeAnxietyBar.transform.parent as RectTransform : null;
    public RectTransform PrincessAnxietyUI => princessAnxietyBar != null ? princessAnxietyBar.transform.parent as RectTransform : null;

    private void Awake()
    {
        cannonAim = GetComponent<CannonAim>();
        playerTiming = GetComponent<PlayerTiming>();
        foreach (SpriteRenderer renderer in cart.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.name == "CartBody") cartBody = renderer;
            else if (renderer.name.StartsWith("Tire ") && renderer.sprite != null) tires.Add(renderer);
        }
        ResetStageState();
    }

    private void Update()
    {
        if (failed)
        {
            if ((Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)) Restart();
            return;
        }
        if (choosingUpgrade)
        {
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
        if (holdingShell)
        {
            holdTimeRemaining -= Time.deltaTime;
            if (holdTimeRemaining <= 0f) ResolveShellHold(false);
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

    public void Enter(StationAction action)
    {
        if (action != StationAction.ShellDeposit || !carrying) return;
        carrying = false;
        depositedShells++;
        Refresh();
    }

    public void Succeed(StationAction action)
    {
        switch (action)
        {
            case StationAction.RightHorse: cart.BoostHorse(true); break;
            case StationAction.LeftHorse: cart.BoostHorse(false); break;
            case StationAction.RoyalFamily: CalmRoyalFamily(); break;
            case StationAction.ShellPile:
                if (!carrying && shellsInPile > 0) { shellsInPile--; carrying = true; }
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

    public bool CanUse(StationAction action)
    {
        switch (action)
        {
            case StationAction.ShellPile: return !carrying && shellsInPile > 0;
            case StationAction.Reload: return depositedShells > 0 && !loaded && !fuseLit;
            case StationAction.Cannon: return loaded && !fuseLit;
            case StationAction.ShellDeposit: return carrying;
            case StationAction.WheelRepair: return wheelDetached;
            default: return true;
        }
    }

    public void Fail(StationAction action)
    {
        if (action != StationAction.Reload || !CanUse(action)) return;
        depositedShells--;
        lostShells++;
        Refresh();
    }

    // 큰 충격(발사 반동, 장애물·몬스터 충돌) 때 포탄을 든 플레이어에게 버티기 판정을 건다.
    public void ApplyImpact()
    {
        if (!carrying) return;
        holdingShell = true;
        holdTimeRemaining = HoldDuration;
    }

    public void ResolveShellHold(bool success)
    {
        if (!holdingShell) return;
        holdingShell = false;
        if (!success)
        {
            carrying = false;
            lostShells++;
        }
        Refresh();
    }

    public void Ignite()
    {
        if (loaded && !fuseLit && shellPrefab != null && cannonMuzzle != null) StartCoroutine(Fire());
    }

    private IEnumerator Fire()
    {
        fuseLit = true;
        yield return new WaitForSeconds(fuseDelay);
        loaded = false;
        fuseLit = false;
        ApplyImpact();
        AddImpactEffects(recoilAnxietyIncrease, recoilShakeStrength);
        GameObject shot = Instantiate(shellPrefab, cannonMuzzle.position, cannonMuzzle.rotation);
        if (shot.TryGetComponent(out Projectile projectile))
            projectile.Launch(cannonAim != null ? (Vector3)cannonAim.Direction : cannonMuzzle.right, projectileSpeed);
        Refresh();
    }

    // 장애물·몬스터 공격이 마차에 닿았을 때.
    public void ReceiveCollision(float damage)
    {
        if (failed) return;
        durability = Mathf.Max(0f, durability - Mathf.Max(0f, damage));
        if (damage >= shellHoldDamageThreshold) ApplyImpact();
        AddImpactEffects(collisionAnxietyIncrease, impactShakeStrength);
        if (StageDistance >= wheelLossMinDistance && !wheelDetached && Random.value < wheelLossChance) DetachWheel();
        if (durability <= 0f) FailStage("CART DESTROYED");
    }

    private void FailStage(string reason)
    {
        if (failed) return;
        FailureReason = reason;
        failed = true;
        Time.timeScale = 0f;
    }

    private void AddImpactEffects(float anxiety, float shake)
    {
        float multiplier = Mathf.Pow(cushionImpactFactor, cushionAxleLevel);
        princeAnxiety = Mathf.Clamp01(princeAnxiety + anxiety * multiplier);
        princessAnxiety = Mathf.Clamp01(princessAnxiety + anxiety * multiplier);
        cameraRig.Shake(shake * multiplier, shakeDuration);
        if (cannonAim != null) cannonAim.ApplyImpact(aimJitterDegrees * multiplier, shakeDuration);
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
        playerTiming.SetWheelRepairActive(true);
    }

    private void RepairWheel()
    {
        SetTireAlpha(detachedTireAlpha);
        detachedTire = null;
        wheelDetached = false;
        cart.SpeedMultiplier = 1f;
        playerTiming.SetWheelRepairActive(false);
    }

    private void BeginUpgradeSelection()
    {
        choosingUpgrade = true;
        Time.timeScale = 0f;
    }

    public void ChooseUpgrade(int upgrade)
    {
        if (!choosingUpgrade || upgrade < 0 || upgrade > 2) return;
        switch (upgrade)
        {
            case 0: cushionAxleLevel++; break;
            case 1: horseLevel++; break;
            case 2: precisionLevel++; break;
        }
        StopAllCoroutines();
        if (wheelDetached) RepairWheel();
        stageNumber++;
        stageStartDistance = cart.Distance;
        cart.ResetForStage(horseLevel);
        ResetStageState();
        obstacles.ResetForStage();
        choosingUpgrade = false;
        Time.timeScale = 1f;
        Refresh();
    }

    private void ResetStageState()
    {
        durability = MaxDurability;
        princeAnxiety = StartPrinceAnxiety;
        princessAnxiety = StartPrincessAnxiety;
        carrying = false;
        depositedShells = StartDepositedShells;
        loaded = true;
        shellsInPile = StartShellsInPile;
        lostShells = 0;
        fuseLit = false;
        holdingShell = false;
        wheelEverDetached = false;
        wheelRepairSteps = 0;
        Refresh();
    }

    // 빌드 목록에 없는 테스트 씬에서도 재시작되도록 에디터에서는 경로로 다시 연다.
    public void Restart()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(scene.buildIndex);
#endif
    }

    private void OnDestroy()
    {
        if (failed || choosingUpgrade) Time.timeScale = 1f;
    }

    private void Refresh()
    {
        if (carriedShell != null) carriedShell.SetActive(carrying);
        if (storedShell != null) storedShell.SetActive(depositedShells > 0);
        if (princeAnxietyBar != null) princeAnxietyBar.fillAmount = princeAnxiety;
        if (princessAnxietyBar != null) princessAnxietyBar.fillAmount = princessAnxiety;
    }
}

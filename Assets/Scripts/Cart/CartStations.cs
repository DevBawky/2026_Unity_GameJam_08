using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine;

public sealed class CartStations : MonoBehaviour
{
    [SerializeField] private CartMotion cart;
    [SerializeField] private GameObject carriedShell;
    [SerializeField] private GameObject storedShell;
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private Transform cannonMuzzle;
    [SerializeField] private UnityEngine.UI.Image princeAnxietyBar;
    [SerializeField] private UnityEngine.UI.Image princessAnxietyBar;
    [SerializeField, Range(0f, 1f)] private float princeAnxiety = 0.28f;
    [SerializeField, Range(0f, 1f)] private float princessAnxiety = 0.42f;
    [SerializeField, Min(0f)] private float anxietyPerSecond = 0.015f;
    [SerializeField, Min(0f)] private float fuseDelay = 2.5f;
    [SerializeField, Min(0f)] private float projectileSpeed = 10f;
    [Header("Obstacles and impacts")]
    [SerializeField, Min(1)] private int stageNumber = 1;
    [SerializeField, Min(0.1f)] private float minSpawnInterval = 3f;
    [SerializeField, Min(0.1f)] private float maxSpawnInterval = 5f;
    [SerializeField, Range(0.1f, 1f)] private float spawnIntervalPerStage = 0.9f;
    [SerializeField, Min(1f)] private float spawnDistance = 45f;
    [SerializeField, Min(0f)] private float spawnSideRange = 4f;
    [SerializeField, Range(0f, 1f)] private float logProbability = 0.4f;
    [SerializeField] private Vector2 rockSize = new Vector2(0.8f, 0.8f);
    [SerializeField] private Vector2 logSize = new Vector2(1.6f, 1.1f);
    [SerializeField, Range(0.05f, 1f)] private float collisionHeightFraction = 0.65f;
    [SerializeField, Min(0f)] private float collisionAnxietyIncrease = 0.06f;
    [SerializeField, Min(0f)] private float shellHoldDamageThreshold = 6f;
    [SerializeField, Min(0f)] private float recoilAnxietyIncrease = 0.04f;
    [SerializeField, Range(0f, 1f)] private float wheelLossChance = 0.25f;
    [SerializeField, Min(0f)] private float impactShakeStrength = 0.3f;
    [SerializeField, Min(0f)] private float recoilShakeStrength = 0.2f;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.45f;
    [SerializeField, Min(0f)] private float aimJitterDegrees = 6f;
    [SerializeField, Min(0)] private int cushionAxleLevel;

    private sealed class Obstacle
    {
        public GameObject gameObject;
        public Vector2 size;
        public float damage;
    }
    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private float nextObstacleTime;
    private float durability = 100f;
    private bool failed;
    private bool wheelDetached;
    private bool wheelEverDetached;
    private int wheelRepairSteps;
    private readonly List<SpriteRenderer> tires = new List<SpriteRenderer>();
    private SpriteRenderer detachedTire;
    private float detachedTireAlpha;
    private float stageStartDistance;
    private bool choosingUpgrade;
    private int horseLevel;
    private int precisionLevel;
    public bool ChoosingUpgrade => choosingUpgrade;
    public int StageNumber => stageNumber;
    public float StageDistance => cart != null ? Mathf.Max(0f, cart.Distance - stageStartDistance) : 0f;
    public int StageGoal => 1000 + (Mathf.Max(1, stageNumber) - 1) * 150;
    public int CushionAxleLevel => cushionAxleLevel;
    public int HorseLevel => horseLevel;
    public int PrecisionLevel => precisionLevel;
    public float TimingBonusDegrees => precisionLevel * 360f * 0.027f;
    public SpriteRenderer DetachedTire => detachedTire;
    private PlayerTiming playerTiming;
    public CartMotion Cart => cart;
    public float Durability => durability;
    public bool Failed => failed;
    public string FailureReason { get; private set; } = "CART DESTROYED";
    public bool WheelDetached => wheelDetached;
    public int WheelRepairSteps => wheelRepairSteps;
    private bool carrying;
    private int depositedShells = 1;
    private bool loaded = true;
    private int shellsInPile = 17;
    private int lostShells;
    private bool holdingShell;
    private float holdTimeRemaining;
    private const float HoldDuration = 1.6f;
    public bool IsCarrying => carrying;
    public bool IsHoldingShell => holdingShell;
    public float HoldTimeRemaining => holdTimeRemaining;
    public int ShellsInPile => shellsInPile;
    public int LostShells => lostShells;
    public bool IsLoaded => loaded;
    public bool IsDeposited => depositedShells > 0;
    public int DepositedShells => depositedShells;
    public RectTransform PrinceAnxietyUI => princeAnxietyBar != null ? princeAnxietyBar.transform.parent as RectTransform : null;
    public RectTransform PrincessAnxietyUI => princessAnxietyBar != null ? princessAnxietyBar.transform.parent as RectTransform : null;
    public float PrinceAnxiety => princeAnxiety;
    public float PrincessAnxiety => princessAnxiety;
    public bool PrinceEscaping => princeAnxiety >= 0.75f;
    public bool PrincessEscaping => princessAnxiety >= 0.75f;
    private bool fuseLit;
    private CannonAim cannonAim;
    private SpriteRenderer cartBody;
    public SpriteRenderer CartBody => cartBody;
    public bool IsFiring => fuseLit;

    private void Awake()
    {
        cannonAim = GetComponent<CannonAim>();
        playerTiming = GetComponent<PlayerTiming>();
        stageNumber = Mathf.Max(1, stageNumber);
        if (cart != null)
        {
            foreach (SpriteRenderer renderer in cart.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name == "CartBody") cartBody = renderer;
                else if (renderer.name.StartsWith("Tire ") && renderer.sprite != null) tires.Add(renderer);
            }
        }
        // Serialized values in older scenes must not override the stage starting state.
        princeAnxiety = 0.28f;
        princessAnxiety = 0.42f;
        ScheduleObstacle();
        Refresh();
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
            return;
        }
        if (cart != null && StageDistance >= StageGoal) { BeginUpgradeSelection(); return; }
        UpdateObstacles();
        if (failed) return;
        if (cart != null && !wheelEverDetached && StageDistance >= 155f) DetachWheel();
        princeAnxiety = Mathf.Clamp01(princeAnxiety + anxietyPerSecond * Time.deltaTime);
        princessAnxiety = Mathf.Clamp01(princessAnxiety + anxietyPerSecond * Time.deltaTime);
        if (princeAnxiety >= 1f || princessAnxiety >= 1f) { FailStage("PASSENGER ESCAPED"); return; }
        if (holdingShell)
        {
            holdTimeRemaining -= Time.deltaTime;
            if (holdTimeRemaining <= 0f) ResolveShellHold(false);
        }
        Refresh();
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
            case StationAction.RightHorse: if (cart != null) cart.BoostHorse(true); break;
            case StationAction.LeftHorse: if (cart != null) cart.BoostHorse(false); break;
            case StationAction.RoyalFamily:
                if (princeAnxiety >= princessAnxiety)
                    princeAnxiety = Mathf.Clamp01(princeAnxiety - (PrinceEscaping ? 0.63f : 0.42f));
                else
                    princessAnxiety = Mathf.Clamp01(princessAnxiety - (PrincessEscaping ? 0.63f : 0.42f));
                break;
            case StationAction.ShellPile: if (!carrying && shellsInPile > 0) { shellsInPile--; carrying = true; } break;
            case StationAction.Reload:
                if (depositedShells > 0 && !loaded && !fuseLit) { depositedShells--; loaded = true; }
                break;
            case StationAction.WheelRepair:
                if (wheelDetached)
                {
                    wheelRepairSteps++;
                    if (wheelRepairSteps == 1) SetTireAlpha(detachedTireAlpha);
                    if (wheelRepairSteps >= 2) RepairWheel();
                }
                break;
        }
        Refresh();
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
        if (action == StationAction.Reload && CanUse(action))
        {
            depositedShells--;
            lostShells++;
            Refresh();
        }
    }
    // Called by recoil and available to a future collision system.
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
        if (!success) { carrying = false; lostShells++; }
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
        Projectile projectile = shot.GetComponent<Projectile>();
        if (projectile != null) projectile.Launch(cannonAim != null ? (Vector3)cannonAim.Direction : cannonMuzzle.right, projectileSpeed);
        Refresh();
    }
    private void ScheduleObstacle()
    {
        float factor = Mathf.Pow(Mathf.Clamp(spawnIntervalPerStage, 0.1f, 1f), Mathf.Max(0, stageNumber - 1));
        nextObstacleTime = Time.time + Random.Range(Mathf.Max(0.1f, minSpawnInterval), Mathf.Max(minSpawnInterval, maxSpawnInterval)) * factor;
    }

    private void SpawnObstacle()
    {
        if (cartBody == null) return;
        bool isLog = Random.value < logProbability;
        Vector2 size = isLog ? logSize : rockSize;
        GameObject item = new GameObject(isLog ? "Log obstacle" : "Rock obstacle");
        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = cartBody.sprite;
        renderer.sharedMaterial = cartBody.sharedMaterial;
        renderer.sortingLayerID = cartBody.sortingLayerID;
        renderer.sortingOrder = cartBody.sortingOrder + 1;
        renderer.color = isLog ? new Color(0.48f, 0.25f, 0.1f) : new Color(0.48f, 0.5f, 0.53f);
        Vector2 spriteSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector3.one;
        item.transform.localScale = new Vector3(size.x / Mathf.Max(spriteSize.x, 0.001f), size.y / Mathf.Max(spriteSize.y, 0.001f), 1f);
        Bounds bounds = cartBody.bounds;
        item.transform.position = new Vector3(bounds.min.x - spawnDistance, bounds.center.y + Random.Range(-spawnSideRange, spawnSideRange), bounds.center.z);
        obstacles.Add(new Obstacle { gameObject = item, size = size, damage = isLog ? 9f : 6f });
    }

    private void UpdateObstacles()
    {
        if (cart == null || cartBody == null) return;
        if (Time.time >= nextObstacleTime) { SpawnObstacle(); ScheduleObstacle(); }
        Bounds cartBounds = cartBody.bounds;
        float cartHalfHeight = cartBounds.extents.y * Mathf.Clamp01(collisionHeightFraction);
        float angle = cart.Heading * Mathf.Deg2Rad;
        Vector3 movement = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (cart.Speed * 5f * Time.deltaTime);
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            Obstacle obstacle = obstacles[i];
            if (obstacle.gameObject == null) { obstacles.RemoveAt(i); continue; }
            Vector3 previous = obstacle.gameObject.transform.position;
            Vector3 next = previous + movement;
            obstacle.gameObject.transform.position = next;
            float hitX = cartBounds.min.x - obstacle.size.x * 0.5f;
            bool crossed = previous.x <= cartBounds.max.x + obstacle.size.x * 0.5f && next.x >= hitX;
            float hitT = Mathf.Abs(next.x - previous.x) > 0.0001f ? Mathf.Clamp01((hitX - previous.x) / (next.x - previous.x)) : 0f;
            float hitY = Mathf.Lerp(previous.y, next.y, hitT);
            if (crossed && Mathf.Abs(hitY - cartBounds.center.y) <= cartHalfHeight + obstacle.size.y * 0.5f)
            {
                Destroy(obstacle.gameObject);
                obstacles.RemoveAt(i);
                ReceiveCollision(obstacle.damage);
                if (failed) return;
            }
            else if (next.x > cartBounds.max.x + spawnDistance || Mathf.Abs(next.y - cartBounds.center.y) > spawnDistance)
            {
                Destroy(obstacle.gameObject);
                obstacles.RemoveAt(i);
            }
        }
    }

    public void ReceiveCollision(float damage)
    {
        if (failed) return;
        durability = Mathf.Max(0f, durability - Mathf.Max(0f, damage));
        if (damage >= shellHoldDamageThreshold) ApplyImpact();
        AddImpactEffects(collisionAnxietyIncrease, impactShakeStrength);
        if (cart != null && StageDistance >= 110f && !wheelDetached && Random.value < wheelLossChance) DetachWheel();
        if (durability <= 0f)
        {
            FailStage("CART DESTROYED");
        }
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
        float multiplier = Mathf.Pow(0.78f, Mathf.Max(0, cushionAxleLevel));
        princeAnxiety = Mathf.Clamp01(princeAnxiety + anxiety * multiplier);
        princessAnxiety = Mathf.Clamp01(princessAnxiety + anxiety * multiplier);
        if (playerTiming != null) playerTiming.ShakeCamera(shake * multiplier, shakeDuration);
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
        return local.x >= spriteBounds.min.x && local.x <= spriteBounds.max.x &&
               local.y >= spriteBounds.min.y && local.y <= spriteBounds.max.y;
    }

    private void DetachWheel()
    {
        if (wheelDetached || playerTiming == null || tires.Count == 0) return;
        detachedTire = tires[Random.Range(0, tires.Count)];
        detachedTireAlpha = detachedTire.color.a;
        wheelDetached = true;
        wheelEverDetached = true;
        wheelRepairSteps = 0;
        SetTireAlpha(0f);
        if (cart != null) cart.SpeedMultiplier = 0.57f;
        playerTiming.SetWheelRepairActive(true);
    }

    private void RepairWheel()
    {
        SetTireAlpha(detachedTireAlpha);
        detachedTire = null;
        wheelDetached = false;
        if (cart != null) cart.SpeedMultiplier = 1f;
        if (playerTiming != null) playerTiming.SetWheelRepairActive(false);
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
        foreach (Obstacle obstacle in obstacles) if (obstacle.gameObject != null) Destroy(obstacle.gameObject);
        obstacles.Clear();
        stageNumber++;
        stageStartDistance = cart != null ? cart.Distance : 0f;
        if (cart != null) cart.ResetForStage(horseLevel);
        durability = 100f;
        princeAnxiety = 0.28f;
        princessAnxiety = 0.42f;
        carrying = false;
        depositedShells = 1;
        loaded = true;
        shellsInPile = 17;
        lostShells = 0;
        fuseLit = false;
        holdingShell = false;
        wheelEverDetached = false;
        wheelRepairSteps = 0;
        choosingUpgrade = false;
        Time.timeScale = 1f;
        ScheduleObstacle();
        Refresh();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        foreach (Obstacle obstacle in obstacles) if (obstacle.gameObject != null) Destroy(obstacle.gameObject);
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

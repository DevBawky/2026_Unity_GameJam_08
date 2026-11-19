using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 마차 그림 연출: 등불·깃발 프레임, 대포 포신 크기·각도·도화선, 바퀴 회전·먼지·장착 애니메이션.
public sealed class CartArtwork : MonoBehaviour
{
    [SerializeField] private SpriteRenderer frontLight, rearLight, flag;
    [SerializeField] private Sprite[] lightFrames, lowerLightFrames, flagFrames;
    [SerializeField] private Transform barrel;
    [SerializeField] private SpriteRenderer fuse;
    [SerializeField] private SpriteRenderer[] muzzleSmoke;
    private static readonly Vector4[] SmokePuffs =
    {
        new(0, 74, 27, -10), new(2, 58, 18, 12), new(4, 44, 22, -5), new(6, 32, 12, 6)
    };
    private static readonly float[] SmokeSizes = { 37, 30, 25, 20 };
    [SerializeField] private Sprite[] fuseFrames;
    [SerializeField] private SpriteRenderer[] shells;
    [SerializeField] private SpriteRenderer wheelMount;
    [SerializeField] private Sprite[] upperMountFrames, lowerMountFrames, wheelFrames, dustFrames;
    [SerializeField] private SpriteRenderer[] wheelDust;
    private Vector3 barrelScale;
    private SpriteRenderer obscuredTire;
    private readonly List<Transform> recoilingRoots = new();
    private Vector3 renderOffset;
    private void Awake() => barrelScale = barrel.localScale;

    private void OnEnable()
    {
        RenderPipelineManager.beginContextRendering += BeginRendering;
        RenderPipelineManager.endContextRendering += EndRendering;
    }

    // Shift the visible wagon for rendering, then restore every gameplay transform.
    private void BeginRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        var game = CartStations.Instance;
        if (!Application.isPlaying || !CartStations.Running || game == null || !game.IsSpawned) return;
        double now = game.NetworkManager.ServerTime.Time;
        renderOffset = game.CartArtworkOffset(now);
        if (renderOffset == Vector3.zero) return;
        Transform root = game.Cart.transform;
        recoilingRoots.Add(root);
        foreach (var actor in game.Crew)
            if (actor != null && actor.IsSpawned && !actor.transform.IsChildOf(root))
                recoilingRoots.Add(actor.transform);
        foreach (var item in recoilingRoots) item.position += renderOffset;
    }

    private void EndRendering(ScriptableRenderContext context, List<Camera> cameras) => RestoreRenderPositions();

    private void RestoreRenderPositions()
    {
        foreach (var item in recoilingRoots)
            if (item != null) item.position -= renderOffset;
        recoilingRoots.Clear();
    }

    private void LateUpdate()
    {
        var game = CartStations.Instance;
        if (game == null || !game.IsSpawned) return;
        double now = game.NetworkManager.ServerTime.Time;
        int tick = Mathf.Max(0, (int)System.Math.Floor(now * 30d));
        frontLight.sprite = lightFrames[(tick / 5) % lightFrames.Length];
        if (lowerLightFrames != null && lowerLightFrames.Length > 0)
            rearLight.sprite = lowerLightFrames[((tick + 8) / 5) % lowerLightFrames.Length];
        flag.sprite = flagFrames[(tick / 5) % flagFrames.Length];
        for (int i = 0; i < shells.Length; i++) shells[i].enabled = i < game.DepositedShells;
        DrawCannon(game, now, tick);
        DrawWheels(game, now);
    }

    private void DrawCannon(CartStations game, double now, int tick)
    {
        var lastShot = game.LastCannonShot.Value;
        float recoil = lastShot.Time < 0d ? -1f : (float)((now - lastShot.Time) * 30d);
        Vector2 scale = BarrelScale(game.IsFiring, (float)((game.FuseDeadline.Value - now) * 30d), recoil);
        float caliber = game.CannonCaliber;
        barrel.localScale = Vector3.Scale(barrelScale, new Vector3(scale.x * caliber, scale.y * caliber, 1f));
        var cannonOperator = game.CannonOperator;
        var aim = cannonOperator != null ? cannonOperator.Aim.Value : Vector2.zero;
        var pivot = game.CannonPivot.position;
        Vector2 target = CannonAim.AimPoint(aim, pivot, game, now, out _, out _);
        Vector2 origin = CannonAim.Origin(pivot, game, now, game.CannonTick);
        float angle = recoil >= 0f && recoil < 12f ? lastShot.Angle :
            Vector2.SignedAngle(Vector2.right, target - origin);
        barrel.rotation = Quaternion.Euler(0, 0, angle);
        fuse.enabled = game.IsFiring;
        if (fuse.enabled) fuse.sprite = fuseFrames[(tick / 3) % fuseFrames.Length];
        DrawMuzzleSmoke(game, now, lastShot.Angle, recoil, caliber);
    }

    private void DrawMuzzleSmoke(CartStations game, double now, float angle, float age, float caliber)
    {
        if (muzzleSmoke == null) return;
        const float duration = 24f;
        Vector3 axis = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
        Vector3 side = new(-axis.y, axis.x, 0f);
        Vector3 muzzle = game.RenderedMuzzle(axis, now);
        float sizeScale = game.WorldPerWebUnit * caliber;
        for (int i = 0; i < muzzleSmoke.Length; i++)
        {
            var puff = muzzleSmoke[i];
            var shape = SmokePuffs[i];
            puff.enabled = age >= shape.x && age < duration;
            if (!puff.enabled) continue;
            float p = (age - shape.x) / (duration - shape.x);
            float forward = shape.y * p * (2f - p);
            puff.transform.position = muzzle + (axis * forward - side * (shape.w * p) + Vector3.up * (shape.z * p * p)) * sizeScale;
            puff.transform.localScale = Vector3.one * ((12f + SmokeSizes[i] * p) * sizeScale / puff.sprite.bounds.size.x);
            puff.color = new Color(1f, 1f, 1f, .56f * Mathf.Pow(1f - p, 1.3f));
        }
    }

    internal static Vector2 BarrelScale(bool firing, float remainingTicks, float recoil)
    {
        if (SettingsPanel.ReducedMotion) return Vector2.one;
        if (firing)
        {
            float elapsed = Mathf.Clamp(75f - remainingTicks, 0f, 75f);
            float charge = elapsed / 75f;
            float lateTicks = Mathf.Max(0f, elapsed - 66f);
            float gather = Mathf.SmoothStep(0f, 1f, lateTicks / 9f);
            float pulse = (1f - Mathf.Cos((elapsed + lateTicks * .15f) * 2f * Mathf.PI / 14.4f))
                * .5f * (.4f + .6f * charge) * (1f - gather);
            return new Vector2(1f + .075f * charge + .075f * gather + .018f * pulse,
                1f - .09f * charge - .07f * gather - .028f * pulse);
        }
        if (recoil < 0f || recoil >= 12f) return Vector2.one;
        float p = recoil / 12f;
        return new Vector2(1f, 1f + .08f * (1f - p) * (1f - p) * Mathf.Cos(2f * Mathf.PI * p));
    }

    private void DrawWheels(CartStations game, double now)
    {
        var mount = game.WheelMount.Value;
        int age = mount.SlotKey == 0 ? -1 : (int)System.Math.Floor((now - mount.Time) * 30d + .00001d);
        var tire = !SettingsPanel.ReducedMotion && age >= 0 && age < 10 ? game.TireAt(mount.SlotKey - 1) : null;
        bool rolling = !SettingsPanel.ReducedMotion && CartStations.Running && game.Cart.Speed > 0f;
        int phase = rolling ? game.CannonTick : 0;
        var frame = wheelFrames[(phase / 3) % wheelFrames.Length];
        for (int i = 0; i < wheelDust.Length; i++)
        {
            var wheel = game.TireAt(i);
            var dust = wheelDust[i];
            bool mounted = game.IsWheelMounted(i);
            if (mounted) wheel.sprite = frame;
            if (dust == null) continue;
            dust.enabled = rolling && mounted && wheel != tire;
            if (dust.enabled) dust.sprite = dustFrames[((phase + i) / 3) % dustFrames.Length];
        }
        wheelMount.enabled = tire != null;
        if (obscuredTire != tire)
        {
            if (obscuredTire != null) obscuredTire.enabled = true;
            obscuredTire = tire;
        }
        if (tire == null) return;
        tire.enabled = false;
        bool top = game.IsUpperWheel(tire);
        var frames = top ? upperMountFrames : lowerMountFrames;
        wheelMount.sprite = frames[Mathf.Min(3, Mathf.FloorToInt(age * .85f / 2f))];
        wheelMount.sortingOrder = tire.sortingOrder;
        wheelMount.transform.position = tire.transform.position + Vector3.down * (top ? 4f : -6f) * game.WorldPerWebUnit;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= BeginRendering;
        RenderPipelineManager.endContextRendering -= EndRendering;
        RestoreRenderPositions();
        if (obscuredTire != null) obscuredTire.enabled = true;
    }
}

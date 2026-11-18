using UnityEngine;

public sealed class CartArtwork : MonoBehaviour
{
    [SerializeField] private SpriteRenderer frontLight, rearLight, flag;
    [SerializeField] private Sprite[] lightFrames, flagFrames;
    [SerializeField] private Transform barrel;
    [SerializeField] private SpriteRenderer fuse;
    [SerializeField] private Sprite[] fuseFrames;
    [SerializeField] private SpriteRenderer[] shells;
    [SerializeField] private SpriteRenderer wheelMount;
    [SerializeField] private Sprite[] upperMountFrames, lowerMountFrames, wheelFrames, dustFrames;
    [SerializeField] private SpriteRenderer[] wheelDust;
    private Vector3 barrelScale;
    private SpriteRenderer obscuredTire;
    private void Awake() => barrelScale = barrel.localScale;

    private void LateUpdate()
    {
        var game = CartStations.Instance;
        if (game == null || !game.IsSpawned) return;
        double now = game.NetworkManager.ServerTime.Time;
        int tick = Mathf.Max(0, (int)System.Math.Floor(now * 30d));
        frontLight.sprite = lightFrames[(tick / 5) % lightFrames.Length];
        rearLight.sprite = lightFrames[((tick + 8) / 5) % lightFrames.Length];
        flag.sprite = flagFrames[(tick / 5) % flagFrames.Length];
        for (int i = 0; i < shells.Length; i++) shells[i].enabled = i < game.DepositedShells;
        DrawCannon(game, now, tick);
        DrawWheels(game, now, tick);
    }

    private void DrawCannon(CartStations game, double now, int tick)
    {
        var lastShot = game.LastCannonShot.Value;
        float recoil = lastShot.Time < 0d ? -1f : (float)((now - lastShot.Time) * 30d);
        Vector2 scale = BarrelScale(game.IsFiring, (float)((game.FuseDeadline.Value - now) * 30d), recoil);
        float caliber = 1f + .17f * game.Barrel.Value;
        barrel.localScale = Vector3.Scale(barrelScale, new Vector3(scale.x * caliber, scale.y * caliber, 1f));
        var aim = game.CannonOperator?.Aim.Value ?? Vector2.zero;
        var origin = game.CannonPivot.position;
        Vector2 target = CannonAim.AimPoint(aim, origin, game, now, out _, out _);
        float angle = !game.IsLoaded && recoil >= 0f && recoil < 12f ? lastShot.Angle :
            Vector2.SignedAngle(Vector2.right, target - (Vector2)origin);
        barrel.rotation = Quaternion.Euler(0, 0, angle);
        fuse.enabled = game.IsFiring;
        if (fuse.enabled) fuse.sprite = fuseFrames[(tick / 3) % fuseFrames.Length];
    }

    internal static Vector2 BarrelScale(bool firing, float remainingTicks, float recoil)
    {
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

    private void DrawWheels(CartStations game, double now, int tick)
    {
        var mount = game.WheelMount.Value;
        int age = mount.SlotKey == 0 ? -1 : (int)System.Math.Floor((now - mount.Time) * 30d + .00001d);
        var tire = age >= 0 && age < 10 ? game.TireAt(mount.SlotKey - 1) : null;
        bool rolling = CartStations.Running && game.Cart.Speed > 0f;
        int phase = rolling ? tick : 0;
        var frame = wheelFrames[(phase / 3) % wheelFrames.Length];
        for (int i = 0; i < wheelDust.Length; i++)
        {
            var wheel = game.TireAt(i);
            var dust = wheelDust[i];
            bool broken = wheel == game.DetachedTire;
            if (!broken || game.WheelRepairSteps > 0) wheel.sprite = frame;
            if (dust == null) continue;
            dust.enabled = rolling && !broken && wheel != tire;
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
        if (obscuredTire != null) obscuredTire.enabled = true;
    }
}

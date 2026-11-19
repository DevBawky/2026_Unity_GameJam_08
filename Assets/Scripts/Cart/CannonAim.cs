using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CannonAim : MonoBehaviour
{
    private const float AimCenter = 269f;
    private const float NearSpan = 107f;
    private const float FarSpan = 400.2f;
    private const float AimHeight = 154f;
    [SerializeField] private Transform muzzle;
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private SpriteRenderer crosshair;
    [SerializeField] private SpriteRenderer spreadRing;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private MobileJoystick aimJoystick;
    private NetworkPlayer actor;
    private Vector2 cursor;
    private float nextSend;
    public Transform Muzzle => muzzle;
    public Vector2 Direction { get; private set; } = Vector2.right;
    public float LineLength { get; private set; } = 8f;
    public float Accuracy { get; private set; }
    public Bounds LandingBounds => spreadRing != null && spreadRing.enabled
        ? spreadRing.bounds : new Bounds(muzzle.position + (Vector3)Direction * LineLength, Vector3.zero);

    private void Awake()
    {
        actor = GetComponent<NetworkPlayer>();
        if (gameCamera == null) gameCamera = Camera.main;
    }

    public static Vector2 AimPoint(Vector2 cursor, Vector2 origin, CartStations game, double time, out Vector2 spread, out float accuracy)
    {
        float scale = game.WorldPerWebUnit;
        double tick = System.Math.Floor(time * 30);
        float wave = .105f*Sin(tick*.105) + .045f*Sin(tick*.21+.9);
        float shake = game.WorkShakeAt(time, out float shakePhase);
        float roll = .017f*Sin(tick*.28) + .012f*Sin(tick*.53);
        if (game.IsFiring)
        {
            double end = System.Math.Round(game.FuseDeadline.Value*30);
            double lit = tick-end+75;
            roll += Mathf.Clamp01((float)lit/8f)*(.23f*Sin(lit*.31+end*.618)+.085f*Sin(lit*.67-end*.618));
        }
        float motion = .3f*(roll+shake*.18f*Sin(tick*.53+shakePhase))*Mathf.Pow(.78f,game.Stabilizer.Value);
        float x = AimCenter + Mathf.Clamp(cursor.x, -1f, 1f) * (cursor.x < 0f ? NearSpan : FarSpan);
        float y = Mathf.Clamp(cursor.y, -1f, 1f) * AimHeight;
        float falloff = Mathf.Clamp01((new Vector2(x,y).magnitude - 310f) / (AimCenter + FarSpan - 310f));
        accuracy = Mathf.Clamp(.86f+wave-Mathf.Abs(motion)*.6f-shake*.2f,.32f,.98f)
            * (1f - .72f * Mathf.Pow(falloff, 1.5f));
        float miss = (1f-accuracy)/.68f;
        spread = new Vector2(12+38*miss,9+34*miss)*scale;
        float originShake = shake * Sin(tick * .37 * 3) * 3f;
        float angle = Mathf.Clamp(Mathf.Atan2(y - originShake,x)+motion,-1.1f,1.1f);
        float edge = game.Cart.LateralLimit+134*scale-spread.y-6*scale;
        return new Vector2(origin.x+x*scale,Mathf.Clamp(origin.y+(originShake+Mathf.Tan(angle)*x)*scale,-edge,edge));
    }

    private static float Sin(double radians) => (float)System.Math.Sin(radians);
    public static Vector2 CursorForOffset(Vector2 offset)
    {
        float x = offset.x - AimCenter;
        return new Vector2(Mathf.Clamp(x / (x < 0 ? NearSpan : FarSpan), -1, 1),
            Mathf.Clamp(offset.y / AimHeight, -1, 1));
    }

    private void Update()
    {
        var game = CartStations.Instance;
        bool aiming = game != null && CartStations.Running && actor.IsSpawned && actor.Assigned.Value && muzzle != null &&
            game.CannonAction == StationAction.Cannon && game.HoldsStation(StationAction.Cannon, actor);
        if (crosshair != null) crosshair.enabled = aiming;
        if (aimLine != null) aimLine.enabled = aiming;
        if (spreadRing != null) spreadRing.enabled = aiming;
        if (!aiming) { if (actor.IsSpawned) cursor = actor.Aim.Value; return; }

        if (actor.Local)
        {
            bool mobile = actor.UsesMobileControls;
            var stick = mobile && aimJoystick != null ? aimJoystick.Direction : Vector2.zero;
            var keys = mobile ? null : Keyboard.current;
            if (keys != null)
            {
                if (actor.SecondLocal || LobbySession.LocalSeats != 2)
                    stick += new Vector2((keys.rightArrowKey.isPressed ? 1 : 0) - (keys.leftArrowKey.isPressed ? 1 : 0),
                        (keys.upArrowKey.isPressed ? 1 : 0) - (keys.downArrowKey.isPressed ? 1 : 0));
                if (!actor.SecondLocal) stick += new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                    (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            }
            if (!mobile && !actor.SecondLocal && Gamepad.current != null) stick += Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > .01f) cursor += Vector2.ClampMagnitude(stick, 1) * (1.8f * Time.deltaTime);
            else if (!mobile && !actor.SecondLocal && Mouse.current != null &&
                gameCamera != null && Mouse.current.delta.ReadValue().sqrMagnitude > .01f)
            {
                Vector2 d = ((Vector2)gameCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - (Vector2)muzzle.position) / game.WorldPerWebUnit;
                cursor = CursorForOffset(d);
            }
            cursor = new Vector2(Mathf.Clamp(cursor.x,-1,1), Mathf.Clamp(cursor.y,-1,1));
            if (Time.unscaledTime >= nextSend && actor.Aim.Value != cursor)
            {
                actor.Aim.Value = cursor;
                nextSend = Time.unscaledTime + 1f/30f;
            }
        }
        else cursor = actor.Aim.Value;
        var target = AimPoint(cursor, muzzle.position, game, actor.NetworkManager.ServerTime.Time, out var spread, out var accuracy);
        Accuracy = accuracy;
        Vector2 delta = target - (Vector2)muzzle.position;
        Direction = delta.normalized;
        LineLength = delta.magnitude;
        if (aimLine != null) { aimLine.SetPosition(0,muzzle.position + (Vector3)Direction * game.CannonBarrelLength); aimLine.SetPosition(1,target); }
        if (crosshair != null) crosshair.transform.position = target;
        if (spreadRing != null)
        {
            spreadRing.transform.position = target;
            var parentScale = spreadRing.transform.parent.lossyScale;
            var size = spreadRing.sprite.bounds.size;
            spreadRing.transform.localScale = new Vector3(2*spread.x/(size.x*parentScale.x),2*spread.y/(size.y*parentScale.y),1);
        }
    }
}

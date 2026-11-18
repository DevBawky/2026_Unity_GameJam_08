using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class CannonAim : MonoBehaviour
{
    [SerializeField] private Transform muzzle;
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private SpriteRenderer crosshair;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private MobileJoystick aimJoystick;
    [SerializeField, Min(1f)] private float lineLength = 8f;
    private PlayerTiming timing;
    private LineRenderer crosshairHorizontal;
    private LineRenderer crosshairVertical;
    private Vector2 direction = Vector2.right;
    private Vector2 displayedDirection = Vector2.right;
    private float impactTime;
    private float impactDuration;
    private float impactDegrees;
    public Vector2 Direction => displayedDirection;
    public void ApplyImpact(float degrees, float duration)
    {
        impactDegrees = Mathf.Max(0f, degrees);
        impactDuration = Mathf.Max(0.01f, duration);
        impactTime = impactDuration;
    }
    public Transform Muzzle => muzzle;
    public float LineLength => lineLength;

    private void Awake()
    {
        timing = GetComponent<PlayerTiming>();
        if (gameCamera == null) gameCamera = Camera.main;
        if (crosshair == null && aimLine != null)
        {
            crosshairHorizontal = CreateCrosshairLine("Crosshair Horizontal");
            crosshairVertical = CreateCrosshairLine("Crosshair Vertical");
        }
    }
    private LineRenderer CreateCrosshairLine(string objectName)
    {
        LineRenderer line = Instantiate(aimLine, transform);
        line.name = objectName;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.widthMultiplier = 1.5f;
        line.enabled = false;
        return line;
    }
    private void Update()
    {
        bool aiming = timing != null && timing.IsCannonFocus && muzzle != null;
        if (aimLine != null) aimLine.enabled = aiming;
        if (crosshair != null) crosshair.enabled = aiming;
        if (crosshairHorizontal != null) crosshairHorizontal.enabled = aiming;
        if (crosshairVertical != null) crosshairVertical.enabled = aiming;
        if (!aiming)
        {
            displayedDirection = direction;
            if (impactTime > 0f) impactTime = Mathf.Max(0f, impactTime - Time.deltaTime);
            return;
        }
        if (aimJoystick != null && aimJoystick.gameObject.activeInHierarchy && aimJoystick.Direction.sqrMagnitude > 0.04f)
            SetDirection(aimJoystick.Direction);
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            Vector3 screen = Mouse.current.position.ReadValue();
            screen.z = Mathf.Abs(muzzle.position.z - gameCamera.transform.position.z);
            SetDirection(gameCamera.ScreenToWorldPoint(screen) - muzzle.position);
        }
        else
        {
            Vector2 input = Vector2.zero;
            if (Keyboard.current != null)
                input = new Vector2((Keyboard.current.rightArrowKey.isPressed ? 1 : 0) - (Keyboard.current.leftArrowKey.isPressed ? 1 : 0),
                    (Keyboard.current.upArrowKey.isPressed ? 1 : 0) - (Keyboard.current.downArrowKey.isPressed ? 1 : 0));
            if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.04f)
                input = Gamepad.current.rightStick.ReadValue();
            if (input.sqrMagnitude > 0.04f) SetDirection(input);
        }
        float jitter = impactTime > 0f ? Random.Range(-impactDegrees, impactDegrees) * (impactTime / impactDuration) : 0f;
        displayedDirection = Quaternion.Euler(0f, 0f, jitter) * direction;
        if (impactTime > 0f) impactTime = Mathf.Max(0f, impactTime - Time.deltaTime);
        Vector3 origin = muzzle.position;
        Vector3 endpoint = origin + (Vector3)displayedDirection * lineLength;
        if (aimLine != null)
        {
            aimLine.SetPosition(0, origin);
            aimLine.SetPosition(1, origin + (Vector3)displayedDirection * (lineLength * 0.95f));
        }
        if (crosshair != null) crosshair.transform.position = endpoint;
        if (crosshairHorizontal != null)
        {
            crosshairHorizontal.SetPosition(0, endpoint + Vector3.left * 0.2f);
            crosshairHorizontal.SetPosition(1, endpoint + Vector3.right * 0.2f);
            crosshairVertical.SetPosition(0, endpoint + Vector3.down * 0.2f);
            crosshairVertical.SetPosition(1, endpoint + Vector3.up * 0.2f);
        }
    }
    private void SetDirection(Vector2 value)
    {
        if (value.sqrMagnitude < 0.001f) return;
        // Aim from the cart towards the ground on the right.
        value.x = Mathf.Max(value.x, 0.05f);
        direction = value.normalized;
    }
}

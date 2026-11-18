using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

// 대포 조준. 대포 스테이션에 있는 동안 조준선과 조준점을 보여 주고,
// 모바일 조이스틱·마우스·방향키·게임패드로 방향을 정한다(오른쪽 땅 쪽으로만).
// 충격을 받으면 잠시 조준이 흔들린다.
public sealed class CannonAim : MonoBehaviour
{
    private const float MinInputSqr = 0.04f;
    private const float MinAimX = 0.05f;
    private const float LineDrawRatio = 0.95f;

    [SerializeField] private Transform muzzle;
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private SpriteRenderer crosshair;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private MobileJoystick aimJoystick;
    [SerializeField, Min(1f)] private float lineLength = 8f;

    private PlayerTiming timing;
    private Vector2 direction = Vector2.right;
    private Vector2 displayedDirection = Vector2.right;
    private float impactTime;
    private float impactDuration;
    private float impactDegrees;

    public Vector2 Direction => displayedDirection;
    public Transform Muzzle => muzzle;
    public float LineLength => lineLength;

    private void Awake()
    {
        timing = GetComponent<PlayerTiming>();
        if (gameCamera == null) gameCamera = Camera.main;
    }

    public void ApplyImpact(float degrees, float duration)
    {
        impactDegrees = Mathf.Max(0f, degrees);
        impactDuration = Mathf.Max(0.01f, duration);
        impactTime = impactDuration;
    }

    private void Update()
    {
        bool aiming = timing.IsCannonFocus && muzzle != null;
        aimLine.enabled = aiming;
        crosshair.enabled = aiming;
        if (!aiming)
        {
            displayedDirection = direction;
            impactTime = Mathf.Max(0f, impactTime - Time.deltaTime);
            return;
        }

        ReadInput();
        float jitter = impactTime > 0f ? Random.Range(-impactDegrees, impactDegrees) * (impactTime / impactDuration) : 0f;
        displayedDirection = Quaternion.Euler(0f, 0f, jitter) * direction;
        impactTime = Mathf.Max(0f, impactTime - Time.deltaTime);

        Vector3 origin = muzzle.position;
        aimLine.SetPosition(0, origin);
        aimLine.SetPosition(1, origin + (Vector3)displayedDirection * (lineLength * LineDrawRatio));
        crosshair.transform.position = origin + (Vector3)displayedDirection * lineLength;
    }

    private void ReadInput()
    {
        if (aimJoystick != null && aimJoystick.gameObject.activeInHierarchy && aimJoystick.Direction.sqrMagnitude > MinInputSqr)
        {
            SetDirection(aimJoystick.Direction);
            return;
        }
        if (Mouse.current != null && Mouse.current.leftButton.isPressed && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            Vector3 screen = Mouse.current.position.ReadValue();
            screen.z = Mathf.Abs(muzzle.position.z - gameCamera.transform.position.z);
            SetDirection(gameCamera.ScreenToWorldPoint(screen) - muzzle.position);
            return;
        }

        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
            input = new Vector2((Keyboard.current.rightArrowKey.isPressed ? 1 : 0) - (Keyboard.current.leftArrowKey.isPressed ? 1 : 0),
                (Keyboard.current.upArrowKey.isPressed ? 1 : 0) - (Keyboard.current.downArrowKey.isPressed ? 1 : 0));
        if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > MinInputSqr)
            input = Gamepad.current.rightStick.ReadValue();
        if (input.sqrMagnitude > MinInputSqr) SetDirection(input);
    }

    // 마차에서 오른쪽 땅을 향해서만 조준한다.
    private void SetDirection(Vector2 value)
    {
        if (value.sqrMagnitude < 0.001f) return;
        value.x = Mathf.Max(value.x, MinAimX);
        direction = value.normalized;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField] private GameObject visuals;
    [SerializeField, Min(1f)] private float radius = 65f;
    private RectTransform area;
    private int pointerId = int.MinValue;

    public Vector2 Direction { get; private set; }
    public bool IsMobileMode { get; private set; }

    private void Awake()
    {
        area = (RectTransform)transform;
        SetMobileMode(Application.isMobilePlatform && !Application.isEditor);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (UnityEngine.InputSystem.Keyboard.current?.f6Key.wasPressedThisFrame == true)
            SetMobileMode(!IsMobileMode);
#endif
    }

    public void SetMobileMode(bool enabled)
    {
        IsMobileMode = enabled;
        ResetJoystick();
        if (visuals != null) visuals.SetActive(enabled);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerId != int.MinValue) return;
        pointerId = eventData.pointerId;
        UpdatePointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId) UpdatePointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId) ResetJoystick();
    }

    private void OnDisable() => ResetJoystick();

    private void UpdatePointer(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 point)) return;
        Vector2 offset = Vector2.ClampMagnitude(point - area.rect.center, radius);
        Direction = offset / radius;
        if (handle != null) handle.anchoredPosition = offset;
    }

    private void ResetJoystick()
    {
        pointerId = int.MinValue;
        Direction = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}

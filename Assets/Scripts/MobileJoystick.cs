using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 화면 조이스틱. 누른 손가락 하나만 따라가며 Direction(-1~1)을 내고, 손을 떼면 Released를 알린다.
// 에디터에서는 F6으로 모바일 모드를 켜고 끌 수 있다.
public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField] private GameObject visuals;
    [SerializeField, Min(1f)] private float radius = 65f;
    private RectTransform area;
    private Vector2 origin;
    [SerializeField] private bool floating;
    [SerializeField] private bool previewOnDesktop;
    private bool acceptsInput = true;
    public void SetInputActive(bool active)
    {
        if (acceptsInput == active) return;
        acceptsInput = active;
        if (!active) ResetJoystick();
    }
    private int pointerId = int.MinValue;

    public event Action Released;
    public Vector2 Direction { get; private set; }
    public bool IsMobileMode { get; private set; }
    public void SetRadius(float value) => radius = value;
    public void ShowDesktopPreview(bool show)
    {
        if (previewOnDesktop && !IsMobileMode && visuals != null) visuals.SetActive(show);
    }

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
        if (visuals != null) visuals.SetActive(enabled ? !floating : previewOnDesktop);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!acceptsInput || !IsMobileMode || pointerId != int.MinValue) return;
        if (floating && RectTransformUtility.ScreenPointToLocalPointInRectangle(area,eventData.position,eventData.pressEventCamera,out origin))
        {
            visuals.SetActive(true);
            visuals.transform.localPosition=origin;
        }
        pointerId = eventData.pointerId;
        UpdatePointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId) UpdatePointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId) { ResetJoystick(); Released?.Invoke(); }
    }

    private void OnDisable() => ResetJoystick();

    private void UpdatePointer(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 point)) return;
        Vector2 offset = Vector2.ClampMagnitude(point - (floating ? origin : area.rect.center), radius);
        Direction = offset / radius;
        if (handle != null) handle.anchoredPosition = offset;
    }

    private void ResetJoystick()
    {
        pointerId = int.MinValue;
        if(floating && visuals != null) visuals.SetActive(!IsMobileMode && previewOnDesktop);
        Direction = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

// 화면 조이스틱. 누른 손가락 하나만 따라가며 Direction(-1~1)을 낸다.
public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private const float FollowRadiusRatio = .3f;
    [SerializeField] private RectTransform handle;
    [SerializeField] private GameObject visuals;
    [SerializeField, Min(1f)] private float radius = 65f;
    [SerializeField] private bool floating;
    private RectTransform area;
    private Vector2 origin;
    private Vector2 followVertex;
    private float followRadiusPixels;
    private Vector2 pointerOffset;
    private Vector3 restingPosition;
    private bool acceptsInput = true;
    private int pointerId = int.MinValue;

    public Vector2 Direction { get; private set; }

    private void Awake() => area = (RectTransform)transform;

    public void SetInputActive(bool active)
    {
        if (acceptsInput == active) return;
        acceptsInput = active;
        if (!active) ResetJoystick();
    }

    public void SetRadius(float value)
    {
        ResetJoystick();
        radius = Mathf.Max(1f, value);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!acceptsInput || !GameplayControlMode.IsMobile || pointerId != int.MinValue
            || eventData.button != PointerEventData.InputButton.Left) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position,
            eventData.pressEventCamera, out Vector2 point)) return;

        origin = visuals != null ? (Vector2)area.InverseTransformPoint(visuals.transform.position) : area.rect.center;
        pointerOffset = Vector2.zero;
        if (floating && visuals != null)
        {
            restingPosition = visuals.transform.localPosition;
            ConfigureFollowSector((RectTransform)visuals.transform, eventData.pressEventCamera);
            origin = ClampOrigin(point, eventData.pressEventCamera);
            // The wider touch area should still start at neutral when pressed outside the follow range.
            pointerOffset = point - origin;
            visuals.transform.localPosition = origin;
        }
        pointerId = eventData.pointerId;
        UpdatePointer(point, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            area, eventData.position, eventData.pressEventCamera, out Vector2 point))
            UpdatePointer(point, eventData.pressEventCamera);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId) ResetJoystick();
    }

    private void OnDisable() => ResetJoystick();

    private void ConfigureFollowSector(RectTransform visual, Camera eventCamera)
    {
        followVertex = RectTransformUtility.WorldToScreenPoint(eventCamera, visual.position);
        var topRight = RectTransformUtility.WorldToScreenPoint(eventCamera, visual.TransformPoint(visual.rect.max));
        Vector2 room = Screen.safeArea.max - topRight;
        followRadiusPixels = Mathf.Max(0f, Mathf.Min(Screen.width * FollowRadiusRatio, Mathf.Min(room.x, room.y)));
    }

    private Vector2 ClampOrigin(Vector2 point, Camera eventCamera)
    {
        // The two straight edges extend right and up from the resting position.
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, area.TransformPoint(point));
        Vector2 displacement = Vector2.Max(screenPoint - followVertex, Vector2.zero);
        Vector2 clamped = followVertex + Vector2.ClampMagnitude(displacement, followRadiusPixels);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(area, clamped, eventCamera, out Vector2 local)
            ? local : origin;
    }

    private void UpdatePointer(Vector2 point, Camera eventCamera)
    {
        point -= pointerOffset;
        Vector2 offset = Vector2.ClampMagnitude(point - origin, radius);
        if (floating && visuals != null)
        {
            // The handle moves first. The base follows the part of the drag beyond its radius.
            origin = ClampOrigin(point - offset, eventCamera);
            visuals.transform.localPosition = origin;
            offset = Vector2.ClampMagnitude(point - origin, radius);
        }
        Direction = offset / radius;
        if (handle != null) handle.anchoredPosition = offset;
    }

    private void ResetJoystick()
    {
        if (pointerId != int.MinValue && floating && visuals != null)
            visuals.transform.localPosition = restingPosition;
        pointerId = int.MinValue;
        Direction = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}

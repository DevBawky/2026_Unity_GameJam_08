using UnityEngine;
using UnityEngine.EventSystems;

// 화면 조이스틱. 누른 손가락 하나만 따라가며 Direction(-1~1)을 낸다.
public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private const float FollowWidthRatio = .2f;
    [SerializeField] private RectTransform handle;
    [SerializeField] private GameObject visuals;
    [SerializeField, Min(1f)] private float radius = 65f;
    [SerializeField] private bool floating;
    private RectTransform area;
    private Vector2 origin;
    private Rect followBounds;
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
            followBounds = GetFollowBounds((RectTransform)visuals.transform, eventData.pressEventCamera);
            origin = ClampOrigin(point);
            // The wider touch area should still start at neutral when pressed outside the follow range.
            pointerOffset = point - origin;
            visuals.transform.localPosition = origin;
        }
        pointerId = eventData.pointerId;
        UpdatePointer(point);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            area, eventData.position, eventData.pressEventCamera, out Vector2 point)) UpdatePointer(point);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId) ResetJoystick();
    }

    private void OnDisable() => ResetJoystick();

    private Rect GetFollowBounds(RectTransform visual, Camera eventCamera)
    {
        var safe = Screen.safeArea;
        var bottomLeft = RectTransformUtility.WorldToScreenPoint(eventCamera, visual.TransformPoint(visual.rect.min));
        var topRight = RectTransformUtility.WorldToScreenPoint(eventCamera, visual.TransformPoint(visual.rect.max));
        Vector2 halfSize = (topRight - bottomLeft) * .5f;
        Vector2 center = bottomLeft + halfSize;
        Vector2 min = safe.min + halfSize;
        Vector2 max = safe.max - halfSize;
        float side = Mathf.Max(0f, Mathf.Min(Screen.width * FollowWidthRatio, Mathf.Min(max.x - min.x, max.y - min.y)));
        var start = new Vector2(Mathf.Clamp(center.x - side * .5f, min.x, Mathf.Max(min.x, max.x - side)),
            Mathf.Clamp(center.y - side * .5f, min.y, Mathf.Max(min.y, max.y - side)));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, start, eventCamera, out Vector2 localMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, start + Vector2.one * side, eventCamera, out Vector2 localMax);
        return Rect.MinMaxRect(localMin.x, localMin.y, localMax.x, localMax.y);
    }

    private Vector2 ClampOrigin(Vector2 point) => new(Mathf.Clamp(point.x, followBounds.xMin, followBounds.xMax),
        Mathf.Clamp(point.y, followBounds.yMin, followBounds.yMax));

    private void UpdatePointer(Vector2 point)
    {
        point -= pointerOffset;
        Vector2 offset = Vector2.ClampMagnitude(point - origin, radius);
        if (floating && visuals != null)
        {
            // The handle moves first. The base follows the part of the drag beyond its radius.
            origin = ClampOrigin(point - offset);
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

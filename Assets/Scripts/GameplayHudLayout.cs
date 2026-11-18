using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
[DefaultExecutionOrder(50)]
public sealed class GameplayHudLayout : MonoBehaviour
{
    [SerializeField] private Camera primary, secondary;
    [SerializeField] private RectTransform campaign, track, marker, destination;
    [SerializeField] private TMP_Text campaignLabel;
    [SerializeField] private RectTransform action, exit, emote, emoteMenu;
    [SerializeField] private RectTransform exitGlyph, emoteGlyph;
    [SerializeField] private TMP_Text actionLabel, exitLabel, emoteLabel;
    [SerializeField] private MobileJoystick movement, aim;
    [SerializeField] private RectTransform movementPreview, movementBase, movementHandle, aimHandle;
    [SerializeField] private CatapultStrike catapult;
    [SerializeField] private Image[] catapultIcons;

    private Canvas canvas;
    private Rect previousArea, previousPane;
    private float previousScale;
    private bool previousMenu;

    private void Awake() => canvas = GetComponent<Canvas>();

    private void LateUpdate()
    {
        if (Screen.width == 0 || Screen.height == 0) return;
        var safe = Screen.safeArea;
        var pane = primary.pixelRect;
        float scale = canvas.scaleFactor;
        bool menuOpen = emoteMenu.gameObject.activeSelf;
        if (safe != previousArea || pane != previousPane || !Mathf.Approximately(scale, previousScale) || menuOpen != previousMenu)
        {
            previousArea = safe;
            previousPane = pane;
            previousScale = scale;
            previousMenu = menuOpen;
            Layout(safe, pane, scale, menuOpen);
        }
        ShowCatapultIcons();
    }

    private void Layout(Rect safe, Rect pane, float scale, bool menuOpen)
    {
        float width = Screen.width;
        float height = Screen.height;
        float vmin = Mathf.Min(width, height);
        float top = Mathf.Min(safe.yMax, height - 14f);
        float bottom = Mathf.Max(safe.yMin, 16f);
        float left = Mathf.Max(safe.xMin, 16f);
        float right = Mathf.Min(safe.xMax, width - 16f);
        float markerSize = Mathf.Clamp(vmin * .0325f, 20f, 35f);
        float hudFont = Mathf.Clamp(vmin * .016f, 11f, 16f);
        float gutter = Mathf.Max(.23f * width, Mathf.Max(left, width - right) + 148f);
        float hudWidth = width <= 420f ? right - left : Mathf.Min(560f, width - 2f * gutter);
        float hudHeight = markerSize + hudFont * 1.25f + 10f;
        if (width <= 420f) top -= 94f;
        var hudCenter = new Vector2((left + right) * .5f, top - hudHeight * .5f);
        Place(campaign, hudCenter, new Vector2(hudWidth, hudHeight), scale);
        float routeWidth = Mathf.Max(1f, hudWidth - markerSize - 2f * Mathf.Clamp(width * .015f, 6f, 14f) - 2f);
        Place(track, new Vector2(hudCenter.x, top - hudHeight + markerSize * .5f + 5f),
            new Vector2(routeWidth, Mathf.Clamp(vmin * .013f, 6f, 10f)), scale);
        marker.sizeDelta = Vector2.one * (markerSize / scale);
        destination.sizeDelta = Vector2.one * (hudFont / scale);
        destination.anchoredPosition = Vector2.zero;
        Place(campaignLabel.rectTransform, new Vector2(hudCenter.x, top - 5f - .625f * hudFont),
            new Vector2(hudWidth - 18f, 1.25f * hudFont), scale);
        campaignLabel.fontSize = hudFont / scale;

        bool landscape = width >= height;
        float controlGap = landscape ? Mathf.Clamp(height * .01f, 7f, 11f) : 7f;
        float inset = Mathf.Min(width * .015f, 22f);
        float limit = LobbySession.LocalSeats == 2
            ? (right - left - 16f - 4f * (inset + controlGap)) / 6f
            : right - left;
        float stick = Mathf.Min(limit, landscape ? Mathf.Clamp(height * .24f, 80f, 192f) : Mathf.Clamp(vmin * .12f, 64f, 88f));
        float diameter = Mathf.Min(limit, landscape ? Mathf.Clamp(height * .26f, 88f, 208f) : stick);
        float support = diameter * 2f / 3f;
        float step = diameter + controlGap;
        float menuHeight = Mathf.Clamp(height * .12f, 102f, 126f);
        Place(emoteMenu, new Vector2((left + right) * .5f, bottom + menuHeight * .5f),
            new Vector2(Mathf.Min(1120f, right - left), menuHeight), scale);
        if (emoteMenu.TryGetComponent(out GridLayoutGroup grid))
        {
            float padding = Mathf.Clamp(vmin * .01f, 4f, 9f);
            float gap = Mathf.Clamp(width * .0075f, 3f, 9f);
            int insetPixels = Mathf.RoundToInt(padding / scale);
            grid.padding = new RectOffset(insetPixels, insetPixels, insetPixels, insetPixels);
            grid.spacing = Vector2.one * (gap / scale);
            grid.cellSize = new Vector2((Mathf.Min(1120f, right - left) - 2f * padding - 6f * gap) / 7f,
                menuHeight - 2f * padding) / scale;
        }
        if (menuOpen) bottom += menuHeight + 8f;
        float paneRight = Mathf.Min(right, pane.xMax - (pane.width < width ? 8f : 0f)) - inset;
        float x = paneRight - diameter * .5f;
        float y = bottom + diameter * .5f;
        Place(action, new Vector2(x, y), Vector2.one * diameter, scale);
        Place(exit, new Vector2(x - step * .5f, y + step * .866f), Vector2.one * support, scale);
        Place(emote, new Vector2(x - step, y), Vector2.one * support, scale);

        SizeControlLabel(exitLabel, exitGlyph, support, vmin, scale);
        SizeControlLabel(emoteLabel, emoteGlyph, support, vmin, scale);
        actionLabel.fontSize = (landscape ? Mathf.Clamp(vmin * .023f, 13f, 25f) : Mathf.Clamp(vmin * .02f, 11f, 15f)) / scale;
        float stickX = left + inset + stick * .5f;
        float stickY = bottom + stick * .5f;
        var movementArea = (RectTransform)movement.transform;
        movementArea.anchorMax = new Vector2(pane.width / (2f * width), movementArea.anchorMax.y);
        Place(movementPreview, new Vector2(stickX, stickY), Vector2.one * stick, scale);
        movementBase.sizeDelta = Vector2.one * (stick / scale);
        movementHandle.sizeDelta = Vector2.one * (stick * .52f / scale);
        Place((RectTransform)aim.transform, new Vector2(stickX, stickY), Vector2.one * stick, scale);
        aimHandle.sizeDelta = movementHandle.sizeDelta;
        movement.SetRadius(stick * .36f / scale);
        aim.SetRadius(stick * .36f / scale);
    }

    private static void SizeControlLabel(TMP_Text label, RectTransform glyph, float diameter, float vmin, float scale)
    {
        float font = Mathf.Min(Mathf.Clamp(vmin * .0155f, 9f, 16f), diameter / 4.4f);
        float icon = Mathf.Clamp(diameter * .34f, 10f, 42f);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = Vector2.one * .5f;
        labelRect.sizeDelta = new Vector2(diameter, font * 1.25f) / scale;
        labelRect.anchoredPosition = Vector2.down * ((icon + 4f) * .5f / scale);
        label.fontSize = font / scale;
        glyph.sizeDelta = Vector2.one * (icon / scale);
        glyph.anchoredPosition = Vector2.up * ((font + 4f) * .5f / scale);
    }

    private void Place(RectTransform rect, Vector2 screenPoint, Vector2 pixels, float scale)
    {
        var parent = (RectTransform)rect.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, canvas.worldCamera, out var point);
        rect.anchorMin = rect.anchorMax = parent.pivot;
        rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = point;
        rect.sizeDelta = pixels / scale;
    }

    private void ShowCatapultIcons()
    {
        if (catapult == null) return;
        bool visible = catapult.TryGetWarning(out var point);
        float size = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * .042f, 26f, 44f);
        ProjectIcon(catapultIcons[0], primary, visible, point, size);
        if (secondary != null) ProjectIcon(catapultIcons[1], secondary, visible, point, size);
    }

    private void ProjectIcon(Image icon, Camera camera, bool visible, Vector3 point, float size)
    {
        Vector3 projected = camera.WorldToScreenPoint(point);
        var pane = camera.pixelRect;
        pane.xMin += size * .5f;
        pane.xMax -= size * .5f;
        pane.yMin += size * .5f;
        pane.yMax -= size * .5f;
        bool show = visible && camera.enabled && projected.z > 0f && pane.Contains(projected);
        icon.enabled = show;
        if (show) Place(icon.rectTransform, projected, Vector2.one * size, canvas.scaleFactor);
    }
}

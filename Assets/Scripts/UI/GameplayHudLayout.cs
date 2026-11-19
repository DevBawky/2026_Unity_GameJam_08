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
    [SerializeField] private Image[] pursuitIcons, pursuitGlows;
    [SerializeField] private RectTransform[] topRightControls;

    private Canvas canvas;
    private CameraRig primaryRig, secondaryRig;
    private readonly Vector3[] controlCorners = new Vector3[4];
    private Rect previousArea, previousPane;
    private float previousScale;
    private bool previousMenu, previousMobile;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        primaryRig = primary.GetComponent<CameraRig>();
        if (secondary != null) secondaryRig = secondary.GetComponent<CameraRig>();
    }

    private void LateUpdate()
    {
        if (Screen.width == 0 || Screen.height == 0) return;
        var safe = Screen.safeArea;
        var pane = primary.pixelRect;
        float scale = canvas.scaleFactor;
        bool menuOpen = emoteMenu.gameObject.activeSelf;
        bool mobile = GameplayControlMode.IsMobile;
        if (safe != previousArea || pane != previousPane || !Mathf.Approximately(scale, previousScale)
            || menuOpen != previousMenu || mobile != previousMobile)
        {
            previousArea = safe;
            previousPane = pane;
            previousScale = scale;
            previousMenu = menuOpen;
            previousMobile = mobile;
            Layout(safe, pane, scale, menuOpen, mobile);
        }
        var game = CartStations.Instance;
        bool routeVisible = game != null && (CartStations.Running || game.ChoosingUpgrade || game.ChapterComplete);
        track.gameObject.SetActive(routeVisible);
        if (campaignLabel != null) campaignLabel.gameObject.SetActive(routeVisible);
        Vector3 landing = default;
        bool danger = catapult != null && catapult.TryGetWarning(out landing);
        ShowWarnings(0, primary, primaryRig, safe, danger, landing);
        ShowWarnings(1, secondary, secondaryRig, safe, danger, landing);
    }

    private void Layout(Rect safe, Rect pane, float scale, bool menuOpen, bool mobile)
    {
        float width = Screen.width;
        float height = Screen.height;
        float vmin = Mathf.Min(width, height);
        Rect webSafe = WebSafeArea;
        float bottom = webSafe.yMin;
        float left = webSafe.xMin;
        float right = webSafe.xMax;
        float markerSize = CampaignMarkerSize;
        Rect route = CampaignArea(WebSafeArea);
        float routePadding = CampaignPadding;
        Place(campaign, route.center, route.size, scale);
        float trackY = route.yMax - 5f - markerSize * .5f;
        Place(track, new Vector2(route.center.x + .25f * markerSize, trackY),
            CampaignTrackSize(route), scale);
        marker.sizeDelta = Vector2.one * (markerSize / scale);
        destination.sizeDelta = Vector2.one * (.52f * markerSize / scale);
        destination.anchorMin = destination.anchorMax = new Vector2(0f, .5f);
        destination.anchoredPosition = Vector2.left * (.74f * markerSize / scale);
        if (campaignLabel != null)
        {
            float labelHeight = CampaignDistanceFont * 1.25f;
            campaignLabel.fontSize = CampaignDistanceFont / scale;
            Place(campaignLabel.rectTransform, new Vector2(route.center.x, route.yMin + 5f + labelHeight * .5f),
                new Vector2(route.width - 2f * routePadding, labelHeight), scale);
        }

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
        float paneRight = Mathf.Min(right, pane.xMax - (pane.width < width ? 8f : 0f)) - inset;
        float actionX = (mobile ? right - inset : paneRight) - diameter * .5f;
        float actionY = bottom + diameter * .5f;
        Place(action, new Vector2(actionX, actionY), Vector2.one * diameter, scale);

        // v14: 이모티콘 목록은 이동 스틱 오른쪽~행동 버튼 왼쪽 사이 아래쪽에 4열(최대 520px)로 뜨고, 열린 동안 이모티콘 버튼은 숨긴다.
        const float emoteGap = 8f, menuPadding = 4f, cellGap = 4f, cellHeight = 56f;
        int menuColumns = 4, menuRows = Mathf.CeilToInt(emoteMenu.childCount / (float)menuColumns);
        float menuRight = actionX - diameter * .5f - (controlGap + support) * .5f - emoteGap;
        float menuStart = left + inset + Mathf.Max(stick, 120f) + emoteGap;
        float menuWidth = Mathf.Max(1f, Mathf.Min(520f, menuRight - menuStart));
        float menuHeight = 2f * menuPadding + menuRows * cellHeight + (menuRows - 1) * cellGap;
        float menuBottom = bottom + (diameter - support) * .5f;
        Place(emoteMenu, new Vector2(menuRight - menuWidth * .5f, menuBottom + menuHeight * .5f),
            new Vector2(menuWidth, menuHeight), scale);
        if (emoteMenu.TryGetComponent(out GridLayoutGroup grid))
        {
            int insetPixels = Mathf.RoundToInt(menuPadding / scale);
            grid.padding = new RectOffset(insetPixels, insetPixels, insetPixels, insetPixels);
            grid.spacing = Vector2.one * (cellGap / scale);
            grid.constraintCount = menuColumns;
            grid.cellSize = new Vector2((menuWidth - 2f * menuPadding - (menuColumns - 1) * cellGap) / menuColumns,
                cellHeight) / scale;
        }
        emote.gameObject.SetActive(!menuOpen);
        float y = bottom + diameter * .5f;
        Place(exit, new Vector2(actionX - step * .5f, y + step * .866f), Vector2.one * support, scale);
        Place(emote, new Vector2(actionX - step, y), Vector2.one * support, scale);

        SizeControlLabel(exitLabel, exitGlyph, support, vmin, scale);
        SizeControlLabel(emoteLabel, emoteGlyph, support, vmin, scale);
        actionLabel.fontSize = (landscape ? Mathf.Clamp(vmin * .023f, 13f, 25f) : Mathf.Clamp(vmin * .02f, 11f, 15f)) / scale;
        float stickX = left + inset + stick * .5f;
        float stickY = bottom + stick * .5f;
        movement.SetRadius(stick * .36f / scale);
        aim.SetRadius(stick * .36f / scale);
        var movementArea = (RectTransform)movement.transform;
        movementArea.anchorMax = new Vector2(pane.width / (2f * width), movementArea.anchorMax.y);
        Place(movementPreview, new Vector2(stickX, stickY), Vector2.one * stick, scale);
        movementBase.sizeDelta = Vector2.one * (stick / scale);
        movementHandle.sizeDelta = Vector2.one * (stick * .52f / scale);
        Place((RectTransform)aim.transform, new Vector2(stickX, stickY), Vector2.one * stick, scale);
        aimHandle.sizeDelta = movementHandle.sizeDelta;
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

    internal static Rect WebSafeArea
    {
        get
        {
            Rect safe = Screen.safeArea;
            float horizontal = Screen.width <= 540 ? 10f : 16f;
            return Rect.MinMaxRect(Mathf.Max(horizontal, safe.xMin), Mathf.Max(16f, safe.yMin),
                Mathf.Min(Screen.width - horizontal, safe.xMax), Mathf.Min(Screen.height - 14f, safe.yMax));
        }
    }

    private const float PlayToolsWidth = 148f;
    private const int RankingSlots = 4;
    internal const float RankingWidthScale = .85f;
    private static float RankingBaseGap => Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * .007f, 3f, 6f);
    internal static float RankingGap => RankingBaseGap * RankingWidthScale;
    internal static float RankingCardScale(int rank) => rank == 0 ? 3f : 2f;
    private static float RankingScaleSum => RankingCardScale(0) + (RankingSlots - 1) * RankingCardScale(1);

    internal static Vector2 RankingCardSize(Rect safe) => new(
        Mathf.Max(0f, (Mathf.Min(safe.width - PlayToolsWidth, 320f) - (RankingSlots - 1) * RankingBaseGap) / RankingSlots),
        Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * .088f, 54f, 70f));

    private static bool RankingBelowTools => Screen.width <= 540 && Screen.width < Screen.height;
    internal static float RankingTop(Rect safe, float campaignBottom)
    {
        float top = safe.yMax - (RankingBelowTools ? 54f : 0f);
        return StackTopHud(safe) ? Mathf.Min(top, campaignBottom - 8f) : top;
    }

    internal static float RankingFit(Rect safe, float cardWidth) => cardWidth > 0f
        ? Mathf.Clamp01((safe.width - (RankingBelowTools ? 0f : PlayToolsWidth) - (RankingSlots - 1) * RankingBaseGap)
            / (cardWidth * RankingScaleSum))
        : 0f;

    private static float RankingWidth(Rect safe)
    {
        Vector2 card = RankingCardSize(safe);
        float fit = RankingFit(safe, card.x);
        return fit > 0f ? (card.x * fit * RankingScaleSum + (RankingSlots - 1) * RankingBaseGap) * RankingWidthScale : 0f;
    }

    private static float LeftOfCampaign(Rect safe) => safe.xMin + RankingWidth(safe) + 8f;
    private static float RightOfCampaign(Rect safe) => Mathf.Max(safe.center.x, safe.xMax - PlayToolsWidth);
    private static bool StackTopHud(Rect safe) => RightOfCampaign(safe) - LeftOfCampaign(safe) < 300f;

    private static float CampaignDistanceFont => Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * .021f, 12f, 17f);
    private static float CampaignPadding => Mathf.Clamp(Screen.width * .015f, 6f, 14f);
    internal static float CampaignMarkerSize => Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * .0325f, 20f, 35f);

    internal static Vector2 CampaignTrackSize(Rect route) => new(
        Mathf.Max(1f, route.width - 1.5f * CampaignMarkerSize - 2f * CampaignPadding - 2f),
        Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * .013f, 6f, 10f));

    internal static Rect CampaignArea(Rect safe)
    {
        float height = CampaignMarkerSize + 10f + CampaignDistanceFont * 1.25f;
        float minX = StackTopHud(safe) ? safe.xMin : LeftOfCampaign(safe);
        float maxX = RightOfCampaign(safe);
        float routeWidth = Mathf.Min(560f, maxX - minX);
        float left = Mathf.Clamp(safe.center.x - routeWidth * .5f, minX, maxX - routeWidth);
        return new Rect(left, safe.yMax - height, routeWidth, height);
    }

    internal static float NoticeTop(Rect safe, float campaignBottom)
    {
        float height = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * .11f, 66f, 88f);
        float top = Mathf.Min(safe.yMax - 94f, campaignBottom - 6f, Screen.height * .78f + height);
        float widestNotice = Mathf.Min(1350f, safe.width);
        float rankingWidth = RankingWidth(safe);
        if (rankingWidth <= 0f || safe.center.x - widestNotice * .5f >= safe.xMin + rankingWidth) return top;
        Vector2 card = RankingCardSize(safe);
        float rankingHeight = card.y * RankingFit(safe, card.x) * RankingCardScale(0);
        return Mathf.Min(top, RankingTop(safe, campaignBottom) - rankingHeight - 6f);
    }

    private void Place(RectTransform rect, Vector2 screenPoint, Vector2 pixels, float scale)
    {
        rect.anchorMin = rect.anchorMax = ((RectTransform)rect.parent).pivot;
        rect.pivot = Vector2.one * .5f;
        PlacePixels(rect, screenPoint, pixels, canvas);
    }

    internal static void PlacePixels(RectTransform rect, Vector2 screenPoint, Vector2 pixels, Canvas owner)
    {
        var parent = (RectTransform)rect.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, owner.worldCamera, out var point);
        rect.anchoredPosition = point;
        rect.sizeDelta = pixels / owner.scaleFactor;
    }

    private void ShowWarnings(int index, Camera view, CameraRig rig, Rect safe, bool danger, Vector3 landing)
    {
        var actor = rig != null ? rig.FollowedActor : null;
        bool onRoad = view != null && view.enabled && actor != null && !actor.InCellar && CartStations.Running;
        if (catapultIcons != null && index < catapultIcons.Length)
            ShowCatapultIcon(catapultIcons[index], view, onRoad && danger, landing);

        if (pursuitIcons == null || index >= pursuitIcons.Length || pursuitGlows == null || index >= pursuitGlows.Length) return;
        var icon = pursuitIcons[index];
        var glow = pursuitGlows[index];
        if (icon == null || glow == null) return;
        Vector3 closest = default;
        bool free = onRoad && !actor.Carrying.Value && !actor.Holding.Value && !actor.Timing.HasStationAccess
            && !CartStations.Instance.IsPostShotGunner(actor);
        bool show = onRoad && (free || actor.Timing.IsCannonFocus || actor.Timing.HasStationAccess && actor.Timing.IsAt(StationAction.Driver))
            && TryGetPursuer(view, out closest);
        icon.enabled = glow.enabled = show;
        if (!show) return;

        var pane = view.pixelRect;
        var target = view.WorldToScreenPoint(closest);
        float radius = 2f * Mathf.Clamp(pane.height * .05f, 14f, 22f);
        float inset = Mathf.Min(radius * 1.4f + 4f, Mathf.Min(pane.width, pane.height) * .5f);
        float x = Mathf.Min(safe.xMax - inset, pane.xMax - Mathf.Clamp(pane.xMax - target.x, inset, Mathf.Max(inset, pane.width * .28f)));
        float yMax = Mathf.Min(safe.yMax, pane.yMax) - inset;
        foreach (var control in topRightControls ?? System.Array.Empty<RectTransform>())
        {
            if (control == null || !control.gameObject.activeInHierarchy) continue;
            control.GetWorldCorners(controlCorners);
            var left = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, controlCorners[0]);
            if (x + inset >= left.x) yMax = Mathf.Min(yMax, left.y - inset);
        }

        var point = new Vector2(x, Mathf.Clamp(target.y, Mathf.Max(safe.yMin, pane.yMin) + inset,
            Mathf.Max(Mathf.Max(safe.yMin, pane.yMin) + inset, yMax)));
        float time = (float)actor.NetworkManager.ServerTime.Time;
        float pulse = .5f + .5f * Mathf.Sin(time * 5.7f);
        Place(glow.rectTransform, point + Vector2.down * (radius / 20f), Vector2.one * ((46f + 10f * pulse) * radius / 20f), canvas.scaleFactor);
        Place(icon.rectTransform, point + Vector2.up * (radius * .15f), Vector2.one * (2.25f * radius), canvas.scaleFactor);
    }

    private static bool TryGetPursuer(Camera view, out Vector3 closest)
    {
        closest = default;
        float unit = CartStations.Instance.WorldPerWebUnit;
        float span = 2f * view.orthographicSize * view.aspect;
        float start = view.transform.position.x + span * .5f - Mathf.Min(100f * unit, span * .1f);
        float nearestX = float.PositiveInfinity;
        foreach (var enemy in Enemy.Active)
        {
            if (enemy == null || !enemy.IsApproaching) continue;
            Vector3 position = enemy.transform.position + Vector3.right * (CavalryArtwork.RiderOffset * unit);
            if (position.x < start || position.x >= nearestX) continue;
            closest = position;
            nearestX = position.x;
        }
        return !float.IsPositiveInfinity(nearestX);
    }

    private void ShowCatapultIcon(Image icon, Camera view, bool danger, Vector3 landing)
    {
        if (icon == null) return;
        var projected = danger ? view.WorldToScreenPoint(landing) : Vector3.zero;
        var pane = danger ? view.pixelRect : default;
        bool show = danger && projected.z > 0f && pane.Contains(projected);
        icon.enabled = show;
        if (!show) return;
        float size = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * .042f, 26f, 44f);
        if (icon.transform.parent.TryGetComponent(out RectMask2D mask))
            Place(mask.rectTransform, pane.center, pane.size, canvas.scaleFactor);
        Place(icon.rectTransform, projected, Vector2.one * size, canvas.scaleFactor);
    }
}

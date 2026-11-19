using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
[DefaultExecutionOrder(60)]
public sealed class PlayerWorkHud : MonoBehaviour
{
    [Serializable]
    private sealed class SeatHud
    {
        public RectTransform bubble, progressRow;
        public Image background, fill;
        public TMP_Text hint, progressName, percent;
        public TMP_Text princeStatus, princessStatus;
        [NonSerialized] public Vector2 DefaultBubbleSize;
        [NonSerialized] public int? HintKey;
    }

    [SerializeField] private Camera primary, secondary;
    [SerializeField] private NetworkPlayer secondPlayer;
    [SerializeField] private RectTransform campaign;
    [SerializeField] private bool useTopAimNotice;
    [SerializeField] private SeatHud[] seats;

    private Canvas canvas;
    // 문구·글자 크기·폭이 바뀐 프레임에만 문자열과 크기를 다시 구한다(매 프레임 문자열 할당 방지).
    private readonly Dictionary<TMP_Text, (int key, Vector2 size)> measured = new();

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        foreach (var seat in seats)
        {
            seat.DefaultBubbleSize = seat.bubble.rect.size;
            seat.hint.enableAutoSizing = true;
            seat.hint.textWrappingMode = TextWrappingModes.Normal;
            seat.hint.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private void LateUpdate()
    {
        if (primary == null) return;
        var game = CartStations.Instance;
        bool running = CartStations.Running;
        var first = running ? NetworkPlayer.LocalPlayer : null;
        var second = running && LobbySession.LocalSeats == 2 && secondPlayer != null && secondPlayer.Local ? secondPlayer : null;
        bool split = second != null && secondary != null && secondary.enabled;

        PresentHint(seats[0], first, game, primary);
        PresentHint(seats[1], second, game, split ? secondary : primary);
        UpdateRoyalWatch(seats[0], first, split ? null : second, game, primary);
        UpdateRoyalWatch(seats[1], split ? second : null, null, game, secondary);
    }

    private void PresentHint(SeatHud hud, NetworkPlayer actor, CartStations game, Camera view)
    {
        if (actor == null || useTopAimNotice && actor.Timing.IsCannonFocus)
        {
            Show(hud.bubble, false);
            return;
        }
        var guide = WorkGuide.For(actor, game);
        UpdateHint(hud, actor, view, guide, game.WorldPerWebUnit);
    }

    private void UpdateHint(SeatHud hud, NetworkPlayer actor, Camera view, WorkGuide guide, float worldUnit)
    {
        var timing = actor.Timing;
        bool timed = guide.Timed && timing.ActiveTimingRect != null;
        bool ready = timed && timing.TimingReady;
        string result = timing.VisibleResult;
        bool hasText = result != null || timed || !string.IsNullOrEmpty(guide.Hint);
        Show(hud.bubble, hasText);
        if (!hasText) return;

        float scale = canvas.scaleFactor;
        float vmin = Mathf.Min(Screen.width, Screen.height);
        float font = Mathf.Clamp(vmin * .023f, 12f, 17f);
        float subFont = Mathf.Clamp(vmin * .017f, 10f, 13f);
        hud.hint.fontSizeMin = 10f / scale;
        hud.hint.fontSizeMax = font / scale;
        hud.progressName.fontSize = hud.percent.fontSize = subFont / scale;
        hud.hint.color = ready ? new Color32(208, 255, 189, 255) : new Color32(255, 247, 239, 255);
        hud.background.color = ready ? new Color32(212, 255, 193, 255) : new Color32(255, 255, 255, 240);

        var pane = view.pixelRect;
        var safe = Screen.safeArea;
        float campaignBottom = campaign == null ? pane.yMax - 52f
            : ScreenPoint(campaign, new Vector2(0, campaign.rect.yMin)).y - 4f;
        var bounds = Rect.MinMaxRect(Mathf.Max(pane.xMin, safe.xMin) + 6f, Mathf.Max(pane.yMin, safe.yMin) + 4f,
            Mathf.Min(pane.xMax, safe.xMax) - 6f, Mathf.Min(Mathf.Min(pane.yMax, safe.yMax), campaignBottom));
        Vector2 size = Vector2.Min(hud.DefaultBubbleSize, new Vector2(bounds.width, bounds.height * .28f));
        if (size.x < 64f || size.y < 24f) { Show(hud.bubble, false); return; }

        int tenths = guide.Seconds >= 0f ? Mathf.RoundToInt(guide.Seconds * 10f) : -1;
        int key = HashCode.Combine(result, timed, ready, guide.Action, guide.Hint, tenths);
        if (hud.HintKey != key)
        {
            string text = result ?? (timed ? LocalizationTable.T(ready ? "지금! " : "초록 구간에서 ") + guide.Action : guide.Hint);
            if (tenths >= 0) text += LocalizationTable.F(" · {0:0.0}초", tenths / 10f);
            hud.hint.text = text;
            hud.HintKey = key;
        }
        bool progress = guide.HasProgress && !timed;
        float progressHeight = progress ? Mathf.Min(16f, size.y * .32f) : 0f;
        float verticalPadding = Mathf.Min(6f, size.y * .12f);
        hud.hint.rectTransform.sizeDelta = new Vector2(-20f, size.y - 2f * verticalPadding - progressHeight) / scale;
        hud.hint.rectTransform.anchoredPosition = Vector2.down * (verticalPadding / scale);
        Show(hud.progressRow, progress);
        if (progress)
        {
            float value = Mathf.Clamp01(guide.Progress);
            hud.fill.fillAmount = value;
            hud.progressName.color = hud.percent.color = guide.Color;
            hud.percent.SetText("{0:0}%", Mathf.Floor(value * 100f));
            hud.progressName.text = guide.ProgressName ?? "";
            float rowWidth = size.x - 20f;
            float nameWidth = !string.IsNullOrEmpty(guide.ProgressName) && rowWidth >= 110f ? 40f : 0f;
            float percentWidth = Mathf.Min(34f, rowWidth * .4f);
            Show(hud.progressName, nameWidth > 0f);
            var bar = (RectTransform)hud.fill.transform.parent;
            float barHeight = Mathf.Min(progressHeight, Mathf.Clamp(vmin * .017f, 8f, 12f)) / scale;
            hud.progressRow.sizeDelta = new Vector2(-20f, progressHeight) / scale;
            hud.progressRow.anchoredPosition = Vector2.up * (verticalPadding / scale);
            bar.offsetMin = new Vector2(nameWidth > 0f ? (nameWidth + 2f) / scale : 0f, -barHeight * .5f);
            bar.offsetMax = new Vector2(-(percentWidth + 4f) / scale, barHeight * .5f);
            hud.percent.fontSize = Mathf.Min(subFont, percentWidth * .4f) / scale;
            hud.percent.rectTransform.sizeDelta = new Vector2(percentWidth, progressHeight) / scale;
            hud.progressName.rectTransform.sizeDelta = new Vector2(nameWidth, progressHeight) / scale;
        }

        Vector3 projected = view.WorldToScreenPoint(actor.transform.position);
        if (projected.z < 0f || !pane.Contains(projected))
            projected = new Vector3(pane.center.x, pane.yMin + pane.height * .38f);
        float offset = Mathf.Clamp(pane.height * worldUnit * 28f / (2f * view.orthographicSize), 23f, 75f);
        if (timed) offset = Mathf.Max(offset, pane.height * worldUnit * 29f / (2f * view.orthographicSize) + 5f);
        Vector2 half = size * .5f;
        float x = Mathf.Clamp(projected.x, bounds.xMin + half.x, bounds.xMax - half.x);
        float y = projected.y + offset + half.y;
        if (y + half.y > bounds.yMax) y = projected.y - offset - half.y;
        y = Mathf.Clamp(y, bounds.yMin + half.y, bounds.yMax - half.y);
        Place(hud.bubble, new Vector2(x, y), size);
    }

    private void UpdateRoyalWatch(SeatHud hud, NetworkPlayer viewer, NetworkPlayer sharedViewer, CartStations game, Camera view)
    {
        if (viewer == null && sharedViewer == null)
        {
            Show(hud.princeStatus.rectTransform.parent, false);
            Show(hud.princessStatus.rectTransform.parent, false);
            return;
        }

        var viewport = view.pixelRect;
        var safe = Screen.safeArea;
        var bounds = Rect.MinMaxRect(
            Mathf.Max(viewport.xMin, safe.xMin) + 6f, Mathf.Max(viewport.yMin, safe.yMin) + 6f,
            Mathf.Min(viewport.xMax, safe.xMax) - 6f, Mathf.Min(viewport.yMax, safe.yMax) - 6f);
        float widthLimit = Mathf.Min(350f, bounds.width);
        float font = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * .016f, 11f, 14f);
        Rect prince = LayoutRoyalRow(hud.princeStatus, "왕자", "#b5d8f5", game.Prince.Value, 0);
        Rect princess = LayoutRoyalRow(hud.princessStatus, "공주", "#f5c8e0", game.Princess.Value, 1);
        if (prince.height > 0f && princess.height > 0f && prince.Overlaps(princess))
        {
            if (prince.yMax < princess.yMax)
                prince.y = princess.yMin - prince.height - 4f;
            else
                princess.y = prince.yMin - princess.height - 4f;
        }
        ShowRoyalRow(hud.princeStatus, prince, bounds);
        ShowRoyalRow(hud.princessStatus, princess, bounds);

        Rect LayoutRoyalRow(TMP_Text label, string name, string color, RiderState child, int index)
        {
            bool nearby = IsRoyalNearby(viewer, game, child) || IsRoyalNearby(sharedViewer, game, child);
            // 쌍둥이 상태판: 지하층에서는 아이가 멀리 있어도 내 머리 위에 상태를 띄운다.
            bool board = !nearby && viewer != null && viewer.InCellar && !child.InCellar && game.Owns(ShopPerk.TwinStatusBoard);
            if (widthLimit < 36f || (!nearby && !board))
                return default;

            Vector3 anchor = board ? viewer.transform.position + Vector3.up * ((70f + 24f * index) * game.WorldPerWebUnit)
                : game.RoyalStatusAnchor(child, index);
            Vector3 feet = view.WorldToScreenPoint(anchor);
            if (feet.z <= 0f || !viewport.Contains(feet)) return default;

            Vector2 size = MeasureRoyalRow(label, name, color, child, font, widthLimit);
            float left = Mathf.Clamp(feet.x - size.x * .5f, bounds.xMin, bounds.xMax - size.x);
            float top = Mathf.Min(feet.y - 5f, bounds.yMax);
            return new Rect(left, top - size.y, size.x, size.y);
        }
    }

    private void ShowRoyalRow(TMP_Text label, Rect rect, Rect bounds)
    {
        var row = (RectTransform)label.transform.parent;
        bool visible = rect.height > 0f && rect.yMin >= bounds.yMin;
        Show(row, visible);
        if (visible) Place(row, rect.center, rect.size);
    }

    private static bool IsRoyalNearby(NetworkPlayer viewer, CartStations game, RiderState child)
    {
        if (viewer == null || viewer.InCellar != child.InCellar) return false;
        Vector2 position = game.Cart.transform.TransformPoint(child.Position);
        return Vector2.Distance(position, viewer.transform.position) <= 52f * game.WorldPerWebUnit;
    }

    private Vector2 MeasureRoyalRow(TMP_Text label, string name, string nameColor, RiderState child, float font, float widthLimit)
    {
        string mood = child.Bored ? "지루해함"
            : child.Upset ? child.Pose == RiderPose.Carried ? "안겨서 우는 중" : "놀라서 우는 중"
            : child.Crying ? "배고파서 우는 중"
            : child.Pose switch
            {
                RiderPose.Fallen => "넘어짐",
                RiderPose.Wandering => "돌아다니는 중",
                RiderPose.Carried => "안겨 있음",
                _ => "착석 중"
            };
        int attention = Mathf.FloorToInt(child.Attention), hunger = Mathf.FloorToInt(child.Hunger);
        float scale = canvas.scaleFactor;
        label.fontSize = font / scale;
        bool alarm = child.Crying || child.Pose == RiderPose.Fallen;
        int key = HashCode.Combine(name, attention, hunger, mood, alarm, font, widthLimit, scale);
        if (!measured.TryGetValue(label, out var cached) || cached.key != key)
        {
            string moodColor = alarm ? "#ffcea0" : "#e2d4ed";
            string stats = LocalizationTable.F("관심 {0}% · 배고픔 {1}%", attention, hunger);
            string text = $"<color={nameColor}>{LocalizationTable.T(name)}</color>  {stats}  <color={moodColor}>{LocalizationTable.T(mood)}</color>";
            label.text = text;
            cached = (key, label.GetPreferredValues(text, (widthLimit - 16f) / scale, 1000f) * scale);
            measured[label] = cached;
        }
        return new Vector2(Mathf.Min(widthLimit, cached.size.x + 16f), cached.size.y + 8f);
    }

    private static Vector2 ScreenPoint(RectTransform rect, Vector2 local) =>
        RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local));

    private void Place(RectTransform rect, Vector2 screenPoint, Vector2 pixels)
    {
        var parent = (RectTransform)rect.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out var point);
        rect.anchorMin = rect.anchorMax = parent.pivot;
        rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = point;
        rect.sizeDelta = pixels / canvas.scaleFactor;
    }

    private static void Show(Component component, bool visible)
    {
        if (component.gameObject.activeSelf != visible) component.gameObject.SetActive(visible);
    }
}

using System;
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
    }

    [SerializeField] private Camera primary, secondary;
    [SerializeField] private NetworkPlayer secondPlayer;
    [SerializeField] private RectTransform campaign;
    [SerializeField] private SeatHud[] seats;

    private Canvas canvas;

    private void Awake() => canvas = GetComponent<Canvas>();

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
        if (actor == null)
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
        string text = timing.VisibleResult ?? (timed
            ? (ready ? "지금! " : "초록 구간에서 ") + guide.Action
            : guide.Hint);
        Show(hud.bubble, !string.IsNullOrEmpty(text));
        if (string.IsNullOrEmpty(text)) return;
        if (guide.Seconds >= 0f) text += $" · {guide.Seconds:0.0}초";

        float scale = canvas.scaleFactor;
        float vmin = Mathf.Min(Screen.width, Screen.height);
        float font = Mathf.Clamp(vmin * .023f, 12f, 17f);
        float subFont = Mathf.Clamp(vmin * .017f, 10f, 13f);
        hud.hint.fontSize = font / scale;
        hud.progressName.fontSize = hud.percent.fontSize = subFont / scale;
        hud.hint.text = text;
        hud.hint.color = ready ? new Color32(25, 41, 31, 255) : new Color32(255, 247, 239, 255);
        hud.background.color = ready ? new Color32(189, 242, 153, 255) : new Color32(41, 40, 49, 238);

        var pane = view.pixelRect;
        float widthLimit = Mathf.Min(timed ? 200f : 272f, pane.width - 12f);
        if (widthLimit < 32f) { Show(hud.bubble, false); return; }
        var preferred = hud.hint.GetPreferredValues(text, (widthLimit - 20f) / scale, 1000f) * scale;
        bool progress = guide.HasProgress && !timed;
        float width = Mathf.Min(widthLimit, Mathf.Max(preferred.x + 20f, progress ? 165f : 32f));
        float textHeight = Mathf.Max(font * 1.25f, preferred.y);
        float height = textHeight + (progress ? 31f : 14f);
        hud.hint.rectTransform.sizeDelta = new Vector2(-20f, textHeight) / scale;
        hud.hint.rectTransform.anchoredPosition = Vector2.down * (6f / scale);
        Show(hud.progressRow, progress);
        if (progress)
        {
            float value = Mathf.Clamp01(guide.Progress);
            hud.fill.fillAmount = value;
            hud.fill.color = hud.percent.color = hud.progressName.color = guide.Color;
            hud.percent.SetText("{0:0}%", Mathf.Floor(value * 100f));
            hud.progressName.text = guide.ProgressName ?? "";
            var bar = (RectTransform)hud.fill.transform.parent;
            float barHeight = Mathf.Clamp(vmin * .017f, 8f, 12f) / scale;
            hud.progressRow.sizeDelta = new Vector2(-20f, 16f) / scale;
            hud.progressRow.anchoredPosition = Vector2.up * (6f / scale);
            bar.offsetMin = new Vector2(string.IsNullOrEmpty(guide.ProgressName) ? 0f : 42f / scale, -barHeight * .5f);
            bar.offsetMax = new Vector2(-38f / scale, barHeight * .5f);
            hud.percent.rectTransform.sizeDelta = new Vector2(34f, 16f) / scale;
            hud.progressName.rectTransform.sizeDelta = new Vector2(40f, 16f) / scale;
        }

        Vector3 projected = view.WorldToScreenPoint(actor.transform.position);
        if (projected.z < 0f || !pane.Contains(projected))
            projected = new Vector3(pane.center.x, pane.yMin + pane.height * .38f);
        float offset = Mathf.Clamp(pane.height * worldUnit * 28f / (2f * view.orthographicSize), 23f, 75f);
        if (timed) offset = Mathf.Max(offset, pane.height * worldUnit * 29f / (2f * view.orthographicSize) + 5f);
        float inset = Mathf.Min(width * .5f + 6f, pane.width * .5f);
        float x = Mathf.Clamp(projected.x, pane.xMin + inset, pane.xMax - inset);
        float campaignBottom = campaign == null ? pane.yMax - 52f
            : ScreenPoint(campaign, new Vector2(0, campaign.rect.yMin)).y - 4f;
        float y = projected.y + offset + height * .5f;
        if (y + height * .5f > campaignBottom) y = projected.y - offset - height * .5f;
        y = Mathf.Clamp(y, pane.yMin + height * .5f + 4f, campaignBottom - height * .5f);
        Place(hud.bubble, new Vector2(x, y), new Vector2(width, height));
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
            if (widthLimit < 36f || (!IsRoyalNearby(viewer, game, child) && !IsRoyalNearby(sharedViewer, game, child)))
                return default;

            Vector3 feet = view.WorldToScreenPoint(game.RoyalStatusAnchor(child, index));
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
        string moodColor = child.Crying || child.Pose == RiderPose.Fallen ? "#ffcea0" : "#e2d4ed";
        string text = $"<color={nameColor}>{name}</color>  관심 {Mathf.FloorToInt(child.Attention)}% · 배고픔 {Mathf.FloorToInt(child.Hunger)}%  <color={moodColor}>{mood}</color>";
        float scale = canvas.scaleFactor;
        label.fontSize = font / scale;
        label.text = text;
        Vector2 size = label.GetPreferredValues(text, (widthLimit - 16f) / scale, 1000f) * scale;
        return new Vector2(Mathf.Min(widthLimit, size.x + 16f), size.y + 8f);
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

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
    }

    [Serializable]
    private sealed class RoyalRow
    {
        public RectTransform panel;
        public Image face, border;
        public TMP_Text heading, meters, condition;
    }

    [Serializable]
    private sealed class WorldHud
    {
        public RectTransform pane;
        public Image crosshair, rope, remaining, flame, dash;
        public Image[] hearts;
    }

    [SerializeField] private Camera primary, secondary;
    [SerializeField] private NetworkPlayer secondPlayer;
    [SerializeField] private RectTransform campaign;
    [SerializeField] private TMP_Text testModeBadge;
    [SerializeField] private SeatHud[] seats;
    [SerializeField] private RoyalRow[] royalRows;
    [SerializeField] private TMP_Text[] cries;
    [SerializeField] private WorldHud[] worlds;
    [SerializeField] private Sprite[] crosshairFrames, flameFrames;
    [SerializeField] private Sprite edgeArrow;
    [SerializeField] private TeamEmotes teamEmotes;
    private static readonly Vector2[] CryPositions = { new(.27f,.34f), new(.73f,.38f), new(.49f,.51f), new(.25f,.68f), new(.72f,.67f) };
    private static readonly float[] CrySizes = { .13f, .12f, .18f, .12f, .14f };
    private static readonly float[] CryAngles = { 10f, -9f, 5f, 9f, -10f };
    // 웹과 같이 문구 4개를 0.8초(24틱)마다 돌리고, 글자마다 19틱씩 어긋나게 시작한다.
    private static readonly string[] CryPhrases = { "으앵----", "으앙---", "앙----", "에에엥---" };
    private Canvas canvas;
    private float Scale => canvas.scaleFactor;
    private float Vmin => Mathf.Min(Screen.width, Screen.height);

    private void Awake() => canvas = GetComponent<Canvas>();

    private void LateUpdate()
    {
        if (primary == null || royalRows == null || royalRows.Length != 2 || Screen.height == 0) return;
        var game = CartStations.Instance;
        bool running = CartStations.Running;
        var first = running ? NetworkPlayer.LocalPlayer : null;
        var second = running && LobbySession.LocalSeats == 2 && secondPlayer != null && secondPlayer.Local ? secondPlayer : null;
        bool split = second != null && secondary != null && secondary.enabled;
        var safe = GameplayHudLayout.WebSafeArea;
        var route = GameplayHudLayout.CampaignArea(safe);
        bool test = running && game.Endless.Value;
        float badgeHeight = 0f;
        if (test)
        {
            float labelFont = Mathf.Clamp(Vmin * .016f, 11f, 16f);
            testModeBadge.SetText(LocalizationTable.T("테스트 모드"));
            testModeBadge.fontSize = labelFont / Scale;
            badgeHeight = labelFont * 1.25f + 2f;
            route.yMin = route.yMax - badgeHeight - 10f;
        }
        if (testModeBadge != null) testModeBadge.transform.parent.gameObject.SetActive(test);
        campaign.gameObject.SetActive(running || game != null && (game.ChoosingUpgrade || game.ChapterComplete));
        float royalHeight = first != null ? PresentRoyals(game, first, second, route, safe) : 0f;
        if (first == null) foreach (var row in royalRows) row.panel.gameObject.SetActive(false);
        if (royalHeight <= 0f)
        {
            Vector2 size = route.size;
            if (test) size.x = testModeBadge.preferredWidth * Scale + 24f;
            Place(campaign, route.center, size);
        }
        if (test)
            Place((RectTransform)testModeBadge.transform.parent, new Vector2(route.center.x, route.yMax - 5f - badgeHeight * .5f),
                new Vector2(testModeBadge.preferredWidth * Scale + 10f, badgeHeight));

        PresentCries(running ? game.CryingCount : 0,
            running && !SettingsPanel.ReducedMotion ? (int)(game.NetworkManager.ServerTime.Time * 30d) : 0);
        float baseTop = GameplayHudLayout.NoticeTop(safe, route.yMin - royalHeight - 6f);
        float noticeHeight = Mathf.Clamp(Vmin * .11f, 66f, 88f);
        float workWidth = Mathf.Min(760f, safe.width);
        float gap = 10f;
        var one = first != null ? WorkGuide.ForNotice(first, game) : default;
        var two = second != null ? WorkGuide.ForNotice(second, game) : default;
        bool showOne = HasNotice(one), showTwo = HasNotice(two);
        bool twoNotices = showOne && showTwo;
        bool columns = twoNotices && Screen.width > Screen.height && Screen.width >= 540f && Screen.width <= 750f;
        if (twoNotices && Screen.width <= 750f) gap = columns ? 4f : 6f;
        int emoteRows = teamEmotes != null ? teamEmotes.LayoutNotices(safe, baseTop, noticeHeight) : 0;
        float top = baseTop - emoteRows * (noticeHeight + gap);
        float width = columns ? (workWidth - gap) * .5f : workWidth;
        float font = twoNotices && Screen.width <= 750f ? Mathf.Clamp(Vmin * .038f, 13f, 24f) : Mathf.Clamp(Vmin * .043f, 14f, 32f);
        float shift = columns ? (width + gap) * .5f : 0f;
        PresentNotice(seats[0], one, second != null ? "P1" : null, safe.center.x - shift, top, width, noticeHeight, font);
        PresentNotice(seats[1], two, second != null ? "P2" : null, safe.center.x + shift,
            top - (showOne && !columns ? noticeHeight + gap : 0f), width, noticeHeight, font);

        PresentWorld(worlds[0], primary, first, split ? null : second, game);
        PresentWorld(worlds[1], secondary, split ? second : null, null, game);
    }

    private static bool HasNotice(WorkGuide guide) => !string.IsNullOrEmpty(guide.Hint) || guide.Timed;

    private void PresentNotice(SeatHud hud, WorkGuide guide, string seat, float x, float top, float width, float height, float font)
    {
        bool show = HasNotice(guide);
        hud.bubble.gameObject.SetActive(show);
        if (!show) return;
        Place(hud.bubble, new Vector2(x, top - height * .5f), new Vector2(width, height));
        bool progress = guide.HasProgress;
        hud.progressRow.gameObject.SetActive(progress);
        hud.percent.gameObject.SetActive(false);
        float barHeight = Mathf.Clamp(Vmin * .017f, 8f, 12f);
        float progressHeight = progress ? barHeight + 4f : 0f;
        float padding = Mathf.Clamp(Vmin * .0225f, 8f, 22f);
        float labelFont = progress ? Mathf.Min(font, Mathf.Max(18f, width * .052f)) : font;
        string hint = guide.Hint ?? LocalizationTable.T("초록 구간에서 ") + guide.Action;
        string user = seat == null ? "" : $"<size=55%><color=#ffe1a8>{seat}</color></size>  ";
        string countdown = guide.Seconds < 0 ? "" : $"<color=#ffe1a8> · {guide.Seconds:0.0}{LocalizationTable.T("초")}</color>";
        hud.hint.SetText(user + hint + countdown);
        hud.hint.fontSizeMax = labelFont / Scale;
        hud.hint.fontSizeMin = 12f / Scale;
        Place(hud.hint.rectTransform, new Vector2(x, top - (height - progressHeight) * .5f),
            new Vector2(width - 2f * padding, Mathf.Min(height - progressHeight, labelFont * 2.3f)));
        if (!progress) return;

        float nameFont = Mathf.Clamp(Vmin * .017f, 10f, 13f);
        bool named = !string.IsNullOrEmpty(guide.ProgressName);
        hud.progressName.gameObject.SetActive(named);
        hud.progressName.SetText(guide.ProgressName);
        hud.progressName.fontSize = nameFont / Scale;
        hud.progressName.color = guide.Color;
        hud.fill.color = guide.Color;
        hud.fill.fillAmount = Mathf.Clamp01(guide.Progress);
        float centerY = top - height * .5f - Mathf.Min(height * .28f, labelFont * .5f + barHeight * .5f + 2f);
        Place(hud.progressRow, new Vector2(x, centerY), new Vector2(width - 2f * padding, barHeight));
        float nameWidth = named ? hud.progressName.preferredWidth * Scale + 6f : 0f;
        if (named) Place(hud.progressName.rectTransform, new Vector2(x - width * .5f + padding + nameWidth * .5f, centerY), new Vector2(nameWidth, barHeight + 4f));
        Place((RectTransform)hud.fill.transform.parent, new Vector2(x + nameWidth * .5f, centerY),
            new Vector2(Mathf.Max(1f, width - 2f * padding - nameWidth), barHeight));
    }

    private float PresentRoyals(CartStations game, NetworkPlayer first, NetworkPlayer second, Rect route, Rect safe)
    {
        var prince = game.Prince.Value;
        var princess = game.Princess.Value;
        bool showPrince = ShowRoyal(royalRows[0], game, prince, first, second, "왕자", "#b5d8f5");
        bool showPrincess = ShowRoyal(royalRows[1], game, princess, first, second, "공주", "#f5c8e0");
        int count = (showPrince ? 1 : 0) + (showPrincess ? 1 : 0);
        if (count == 0) return 0f;
        float font = Mathf.Clamp(Vmin * .0165f, 11f, 14f);
        float portrait = Mathf.Clamp(Vmin * .042f, 24f, 40f);
        float padding = Mathf.Clamp(Screen.width * .01f, 4f, 8f);
        float outerPadding = Mathf.Clamp(Screen.width * .015f, 6f, 14f) + 1f;
        float width = Mathf.Min(242f, (route.width - 2f * outerPadding - (count - 1) * 5f) / count);
        int baseLines = width < 184f ? 4 : 3;
        float baseHeight = Mathf.Max(portrait, baseLines * 1.2f * font + 4f) + 8f;
        float height = .7f * GameplayHudLayout.RankingCardHeight(safe, 1);
        if (height <= 0f) height = baseHeight;
        float sizeRatio = height / baseHeight;
        width = Mathf.Min(width * sizeRatio, safe.width);
        bool stacked = count == 2 && 2f * width + 5f > safe.width;
        float totalWidth = stacked ? width : count * width + (count - 1) * 5f;
        float totalHeight = stacked ? 2f * height + 5f : height;
        float left = Mathf.Clamp(route.center.x - totalWidth * .5f, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - totalWidth));
        float top = route.yMin - 3f;
        Rect ranking = GameplayHudLayout.RankingArea(safe);
        if (top > ranking.yMin - 6f && left < ranking.xMax + 6f)
        {
            if (ranking.xMax + 6f + totalWidth <= safe.xMax) left = ranking.xMax + 6f;
            else top = ranking.yMin - 6f;
        }
        var area = new Rect(left, top - totalHeight, totalWidth, totalHeight);
        float royalHeight = route.yMin - area.yMin + 1f;
        float campaignLeft = Mathf.Min(route.xMin, area.xMin - 6f);
        float campaignRight = Mathf.Max(route.xMax, area.xMax + 6f);
        float campaignHeight = route.height + royalHeight;
        Place(campaign, new Vector2((campaignLeft + campaignRight) * .5f, route.yMax - campaignHeight * .5f),
            new Vector2(campaignRight - campaignLeft, campaignHeight));
        Vector2 offset = count == 1 ? Vector2.zero : stacked
            ? Vector2.up * ((height + 5f) * .5f) : Vector2.left * ((width + 5f) * .5f);
        if (showPrince) LayoutRoyal(royalRows[0], area.center + offset, width, height, portrait, font, padding, sizeRatio);
        if (showPrincess) LayoutRoyal(royalRows[1], area.center - offset, width, height, portrait, font, padding, sizeRatio);
        return royalHeight;
    }

    private static bool ShowRoyal(RoyalRow row, CartStations game, RiderState child, NetworkPlayer first,
        NetworkPlayer second, string name, string color)
    {
        bool nearFirst = game.RoyalNearby(first, child);
        bool nearSecond = game.RoyalNearby(second, child);
        // 쌍둥이 상태판: 지하층에 있으면 아이가 멀리 있어도 상태를 띄운다.
        bool board = game.Owns(ShopPerk.TwinStatusBoard) && !child.InCellar
            && (first.InCellar || second != null && second.InCellar);
        bool visible = nearFirst || nearSecond || board;
        row.panel.gameObject.SetActive(visible);
        if (!visible) return false;
        string viewer = second == null ? "" : nearFirst && nearSecond ? "  P1 · P2" : nearFirst ? "  P1" : "  P2";
        row.heading.SetText($"<color={color}>{LocalizationTable.T(name)}</color>{viewer}");
        row.meters.SetText($"{LocalizationTable.T("관심 필요")} <color=#ffe4aa>{Mathf.CeilToInt(100f - child.Attention)}%</color>   " +
            $"{LocalizationTable.T("배고픔")} <color=#ffe4aa>{Mathf.FloorToInt(child.Hunger)}%</color>");
        string mood = child.Bored ? child.Crying ? "지루해서 우는 중" : "지루해함"
            : child.Upset ? child.Pose == RiderPose.Carried ? "안겨서 우는 중" : "놀라서 우는 중"
            : child.Crying ? "배고파서 우는 중"
            : child.Pose switch
            {
                RiderPose.Fallen => "넘어짐", RiderPose.Wandering => "돌아다니는 중", RiderPose.Carried => "안겨 있음", _ => "착석 중"
            };
        bool needsCare = child.Crying || child.Pose == RiderPose.Fallen;
        row.condition.SetText(LocalizationTable.T(mood));
        row.condition.color = needsCare ? new Color32(255, 206, 160, 255) : new Color32(226, 212, 237, 255);
        row.border.color = needsCare ? new Color32(223, 146, 127, 255) : new Color32(134, 119, 132, 255);
        return true;
    }

    private void LayoutRoyal(RoyalRow row, Vector2 center, float width, float height, float portrait, float font, float padding,
        float sizeRatio)
    {
        portrait *= sizeRatio;
        font *= sizeRatio;
        padding *= sizeRatio;
        float gap = 5f * sizeRatio;
        Place(row.panel, center, new Vector2(width, height));
        Place(row.face.rectTransform, center + Vector2.left * (width * .5f - padding - portrait * .5f), Vector2.one * portrait);
        float textWidth = width - 2f * padding - portrait - gap;
        float x = center.x + (portrait + gap) * .5f;
        float line = font * 1.2f;
        row.heading.fontSizeMax = row.meters.fontSizeMax = row.condition.fontSizeMax = font / Scale;
        Place(row.heading.rectTransform, new Vector2(x, center.y + line), new Vector2(textWidth, line));
        Place(row.meters.rectTransform, new Vector2(x, center.y), new Vector2(textWidth, line));
        Place(row.condition.rectTransform, new Vector2(x, center.y - line), new Vector2(textWidth, line));
    }

    private void PresentCries(int count, int tick)
    {
        for (int i = 0; i < cries.Length; i++)
        {
            var label = cries[i];
            bool visible = count == 2 || count == 1 && i == 0;
            label.gameObject.SetActive(visible);
            if (!visible) continue;
            label.text = CryPhrases[(tick + i * 19) / 24 % CryPhrases.Length];
            Vector2 pos = count == 1 ? new Vector2(.49f, .53f) : CryPositions[i];
            float size = Mathf.Clamp(Vmin * (count == 1 ? .08f : CrySizes[i]), 24f, count == 1 ? 68f : 134f);
            float motion = SettingsPanel.ReducedMotion ? 0f : .5f + .5f * Mathf.Sin(Time.unscaledTime * 15f + i);
            label.fontSize = size / Scale;
            label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (count == 1 ? 7f : CryAngles[i]) - 2f * motion);
            Place(label.rectTransform, new Vector2(pos.x * Screen.width, (1f - pos.y) * Screen.height + 2f * motion),
                new Vector2(Mathf.Min(Screen.width, size * 5f), size * 1.2f));
        }
    }

    private void PresentWorld(WorldHud hud, Camera view, NetworkPlayer viewer, NetworkPlayer shared, CartStations game)
    {
        hud.pane.gameObject.SetActive(viewer != null && view != null);
        if (!hud.pane.gameObject.activeSelf) return;
        Rect pane = view.pixelRect;
        Place(hud.pane, pane.center, pane.size);
        float unit = game.WorldPerWebUnit * pane.height / (2f * view.orthographicSize);
        double now = game.NetworkManager.ServerTime.Time;
        int tick = SettingsPanel.ReducedMotion ? 0 : Mathf.Max(0, (int)(now * 30d));
        var gunner = viewer.Timing.IsCannonFocus ? viewer : shared != null && shared.Timing.IsCannonFocus ? shared : null;
        DrawAim(hud, view, pane, gunner, game, now, tick, unit);
        DrawHeart(hud.hearts[0], view, game, game.Prince.Value, game.PrinceHeart.Value, now, unit, viewer);
        DrawHeart(hud.hearts[1], view, game, game.Princess.Value, game.PrincessHeart.Value, now, unit, viewer);
    }

    private void DrawAim(WorldHud hud, Camera view, Rect pane, NetworkPlayer gunner, CartStations game, double now, int tick, float unit)
    {
        bool visible = gunner != null && gunner.Cannon != null;
        Vector2 target = visible ? view.WorldToScreenPoint(gunner.Cannon.Target) : Vector2.zero;
        bool onScreen = visible && pane.Contains(target);
        bool firing = onScreen && game.IsFiring;
        hud.crosshair.enabled = visible;
        hud.rope.enabled = hud.remaining.enabled = hud.flame.enabled = firing;
        hud.dash.enabled = visible;
        if (!visible) return;
        Vector2 muzzle = view.WorldToScreenPoint(game.RenderedMuzzle(gunner.Cannon.Direction, now));
        Vector2 direction = (target - muzzle).normalized;
        float length = Vector2.Distance(muzzle, target);
        float angle = Vector2.SignedAngle(Vector2.right, direction);
        hud.dash.pixelsPerUnitMultiplier = hud.dash.sprite.rect.width * Scale * canvas.referencePixelsPerUnit
            / (12f * unit * hud.dash.sprite.pixelsPerUnit);
        hud.dash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        Place(hud.dash.rectTransform, (muzzle + target) * .5f, new Vector2(length, 1.5f * unit));

        var aim = hud.crosshair;
        aim.sprite = onScreen ? crosshairFrames[(tick / 4) % crosshairFrames.Length] : edgeArrow;
        var mark = Rect.MinMaxRect(pane.xMin + 12f * unit, pane.yMin + 12f * unit, pane.xMax - 12f * unit, pane.yMax - 12f * unit);
        Vector2 cross = onScreen ? target : new Vector2(Mathf.Clamp(target.x, mark.xMin, mark.xMax), Mathf.Clamp(target.y, mark.yMin, mark.yMax));
        aim.rectTransform.localRotation = Quaternion.Euler(0f, 0f, onScreen ? 0f : Vector2.SignedAngle(Vector2.right, target - cross));
        Place(aim.rectTransform, cross, aim.sprite.rect.size * (onScreen ? 22f / 12f * unit : unit));
        if (!firing) return;
        float artScale = 3f * 42f / 32f * unit;
        float left = Mathf.Clamp01((float)(game.FuseDeadline.Value - now) / game.FuseDuration);
        Vector2 ropeSize = hud.rope.sprite.rect.size * artScale;
        Place(hud.rope.rectTransform, target, ropeSize);
        Place(hud.remaining.rectTransform, target, ropeSize);
        hud.remaining.fillAmount = left;
        float burn = Mathf.PI * .5f + left * 2f * Mathf.PI;
        var flame = hud.flame;
        flame.sprite = flameFrames[(tick / 3) % flameFrames.Length];
        Place(flame.rectTransform, target + new Vector2(Mathf.Cos(burn), Mathf.Sin(burn)) * (13.5f * artScale) + Vector2.up * (8f * unit),
            new Vector2(8f, 16f) * unit);
    }

    private void DrawHeart(Image heart, Camera view, CartStations game, RiderState child, CartWorkShock effect, double now, float unit, NetworkPlayer viewer)
    {
        float age = (float)(now - effect.Time) / .6f;
        heart.enabled = effect.Strength > 0f && age >= 0f && age < 1f && child.Pose == RiderPose.Seated && viewer.InCellar == child.InCellar;
        if (!heart.enabled) return;
        float travel = SettingsPanel.ReducedMotion ? 1f : Mathf.Clamp01(age / .65f);
        Vector3 from = game.Cart.transform.TransformPoint(child.AttentionHeartFrom) + Vector3.up * (12f * game.WorldPerWebUnit);
        Vector3 to = game.Cart.transform.TransformPoint(child.Position) + Vector3.up * (19f * game.WorldPerWebUnit);
        Vector3 at = Vector3.Lerp(from, to, travel) + Vector3.up * (15f * game.WorldPerWebUnit * Mathf.Sin(travel * Mathf.PI));
        heart.color = new Color(1f, 1f, 1f, Mathf.Min(1f, (1f - age) * 5f));
        Place(heart.rectTransform, view.WorldToScreenPoint(at), new Vector2(18f, 14f) * unit);
    }

    private void Place(RectTransform rect, Vector2 center, Vector2 size) => GameplayHudLayout.PlacePixels(rect, center, size, canvas);
}

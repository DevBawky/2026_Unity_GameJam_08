using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public sealed class TeamEmotes : MonoBehaviour
{
    [Serializable]
    private sealed class Notice
    {
        public RectTransform panel;
        public Image icon;
        public TMP_Text label;
        [NonSerialized] public float HideAt;
    }

    [SerializeField] private EmoteSet emotes;
    [SerializeField] private Notice[] notices;
    [SerializeField, Min(0f)] private float noticeSeconds = 4.8f;
    private Canvas canvas;

    private void Awake() => canvas = GetComponent<Canvas>();
    private void OnEnable() => NetworkPlayer.Emoted += Show;
    private void OnDisable() => NetworkPlayer.Emoted -= Show;

    internal int LayoutNotices(Rect safe, float top, float height)
    {
        if (notices == null) return 0;
        float time = Time.unscaledTime;
        int count = 0;
        foreach (var notice in notices)
        {
            bool show = CartStations.Running && time < notice.HideAt;
            notice.panel.gameObject.SetActive(show);
            if (show) count++;
        }
        bool landscape = Screen.width > Screen.height;
        bool wide = landscape && Screen.width >= 460f && count >= 3;
        int columns = wide || landscape && Screen.width >= 400f && Screen.width <= 750f && count >= 2 ? 2 : 1;
        float width = Mathf.Min(wide ? 1350f : 760f, safe.width);
        float gap = count >= 2 && Screen.width <= 750f ? (columns == 2 ? 4f : 6f) : 10f;
        float cell = (width - (columns - 1) * gap) / columns;
        float vmin = Mathf.Min(Screen.width, Screen.height);
        float font = count >= 2 && Screen.width <= 750f ? Mathf.Clamp(vmin * .038f, 13f, 24f) : Mathf.Clamp(vmin * .043f, 14f, 32f);
        float iconSize = font * 1.4f;
        float padding = Mathf.Clamp(vmin * .0225f, 8f, 22f);
        float inset = Mathf.Clamp(vmin * .018f, 8f, 17.5f);
        int index = 0;
        foreach (var notice in notices)
        {
            if (!notice.panel.gameObject.activeSelf) continue;
            float x = safe.center.x - width * .5f + cell * .5f + index % columns * (cell + gap);
            float y = top - height * .5f - index / columns * (height + gap);
            GameplayHudLayout.PlacePixels(notice.panel, new Vector2(x, y), new Vector2(cell, height), canvas);
            notice.label.fontSizeMax = font / canvas.scaleFactor;
            notice.label.fontSizeMin = 12f / canvas.scaleFactor;
            float textWidth = Mathf.Min(notice.label.GetPreferredValues(notice.label.text).x * canvas.scaleFactor,
                cell - 2f * padding - iconSize - inset);
            GameplayHudLayout.PlacePixels(notice.label.rectTransform, new Vector2(x + (iconSize + inset) * .5f, y),
                new Vector2(textWidth, height - 10f), canvas);
            GameplayHudLayout.PlacePixels(notice.icon.rectTransform, new Vector2(x - (textWidth + inset) * .5f, y),
                Vector2.one * iconSize, canvas);
            index++;
        }
        return (count + columns - 1) / columns;
    }

    private void Show(NetworkPlayer player, int key)
    {
        if (key < 0 || key >= emotes.Count) return;
        var icon = emotes[key].icon;
        if (icon == null) return;
        player.ShowEmoteBubble(icon);
        SoundManager.PlayCue(GameSfx.TeamBell);
        int slot = player.Slot.Value;
        if (notices == null || slot < 0 || slot >= notices.Length) return;
        var notice = notices[slot];
        notice.icon.sprite = icon;
        notice.label.SetText(LocalizationTable.F("P{0} 플레이어가 {1}", slot + 1, LocalizationTable.T(emotes[key].message)));
        notice.HideAt = Time.unscaledTime + noticeSeconds;
    }
}

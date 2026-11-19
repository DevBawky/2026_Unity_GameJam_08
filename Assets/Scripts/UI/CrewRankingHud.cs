using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
[DefaultExecutionOrder(65)]
public sealed class CrewRankingHud : MonoBehaviour
{
    [Serializable]
    private sealed class Card
    {
        public RectTransform panel;
        public Image border, background, face;
        public TMP_Text rank, score, nickname;
    }

    [SerializeField] private Card[] cards;
    [SerializeField] private Sprite[] portraits;
    private readonly List<NetworkPlayer> ranked = new(4);
    private Canvas canvas;
    private PlayerWorkHud workHud;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        workHud = GetComponent<PlayerWorkHud>();
    }

    private void LateUpdate()
    {
        ranked.Clear();
        var game = CartStations.Instance;
        if (CartStations.Running)
        {
            foreach (var crew in game.Crew)
                if (crew != null && crew.IsSpawned && crew.Assigned.Value) ranked.Add(crew);
            ranked.Sort(Compare);
        }
        Rect safe = GameplayHudLayout.WebSafeArea;
        Vector2 baseSize = GameplayHudLayout.RankingCardSize(safe);
        float vmin = Mathf.Min(Screen.width, Screen.height);
        float gap = GameplayHudLayout.RankingGap;
        float baseFont = Mathf.Clamp(vmin * .017f, 10.88f, 14.4f);
        float basePadding = Mathf.Clamp(vmin * .007f, 2f, 6f);
        float fit = GameplayHudLayout.RankingFit(safe, baseSize.x);
        float left = safe.xMin;
        float panelTop = GameplayHudLayout.RankingTop(safe, workHud != null ? workHud.CampaignBottom : float.PositiveInfinity);
        for (int rank = 0; rank < cards.Length; rank++)
        {
            var card = cards[rank];
            bool visible = rank < ranked.Count && fit > 0f;
            card.panel.gameObject.SetActive(visible);
            if (!visible) continue;
            var crew = ranked[rank];
            bool local = crew.Local;
            card.border.color = local ? new Color32(165, 187, 141, 255) : new Color32(160, 140, 114, 255);
            card.background.color = local ? new Color32(59, 80, 58, 245) : new Color32(41, 40, 49, 245);
            card.face.sprite = portraits[crew.Slot.Value];
            card.rank.SetText("{0}", rank + 1);
            card.score.SetText("{0}", crew.Score.Value);
            card.nickname.SetText(crew.Nickname.Value.ToString());

            float scale = GameplayHudLayout.RankingCardScale(rank) * fit;
            float width = baseSize.x * scale * GameplayHudLayout.RankingWidthScale;
            float height = baseSize.y * scale;
            float font = baseFont * scale;
            float pad = basePadding * scale * GameplayHudLayout.RankingWidthScale;
            card.rank.fontSize = card.score.fontSize = font / canvas.scaleFactor;
            card.nickname.fontSize = card.rank.fontSize * GameplayHudLayout.RankingWidthScale;

            float x = left + width * .5f;
            float y = panelTop - height * .5f;
            Place(card.panel, new Vector2(x, y), new Vector2(width, height));
            float top = panelTop - 4f * scale - font * .5f;
            float innerWidth = Mathf.Max(1f, width - 2f * pad);
            float textHeight = font + 2f * scale;
            Place(card.rank.rectTransform, new Vector2(x, top), new Vector2(innerWidth, textHeight));
            float rankWidth = 13f * scale;
            Place(card.score.rectTransform, new Vector2(x + rankWidth * .5f, top), new Vector2(Mathf.Max(1f, innerWidth - rankWidth), textHeight));
            Place(card.face.rectTransform, new Vector2(x, y),
                new Vector2(Mathf.Min(42f * scale, innerWidth), Mathf.Max(1f, height - 12f * scale - 2f * font)));
            Place(card.nickname.rectTransform, new Vector2(x, panelTop - height + 4f * scale + font * .5f), new Vector2(innerWidth, textHeight));
            left += width + gap;
        }
    }

    private static int Compare(NetworkPlayer first, NetworkPlayer second)
    {
        int byScore = second.Score.Value.CompareTo(first.Score.Value);
        return byScore != 0 ? byScore : first.Slot.Value.CompareTo(second.Slot.Value);
    }

    private void Place(RectTransform rect, Vector2 center, Vector2 size) => GameplayHudLayout.PlacePixels(rect, center, size, canvas);
}

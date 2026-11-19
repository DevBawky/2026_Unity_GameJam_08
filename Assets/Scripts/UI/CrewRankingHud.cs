using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;

[DefaultExecutionOrder(65)]
public sealed class CrewRankingHud : MonoBehaviour
{
    private const float LeaderScale = 1.5f;
    private static readonly string[] Ordinals = { "1st", "2nd", "3rd", "4th" };

    [Serializable]
    private sealed class Row
    {
        public RectTransform panel;
        public UnityEngine.UI.Image background;
        public TMP_Text rankLabel;
        public TMP_Text nicknameLabel;
        public TMP_Text scoreLabel;
        public GameObject crown;

        [NonSerialized] public bool Visible;
        [NonSerialized] public bool Local;
        [NonSerialized] public int Slot;
        [NonSerialized] public int Rank;
        [NonSerialized] public int Score;
        [NonSerialized] public FixedString64Bytes Nickname;
        [NonSerialized] public Color BackgroundColor;
        [NonSerialized] public Vector2 MoveOrigin;
        [NonSerialized] public float MoveStarted;
        [NonSerialized] public bool MovingUp;
    }

    [SerializeField] private Row[] rows;
    [SerializeField] private Sprite rankCard, leaderCard;
    [SerializeField, Min(0f)] private float cardGap = 7f;
    [SerializeField, Min(0.01f)] private float moveDuration = 0.28f;
    [SerializeField, Min(0f)] private float passingOffset = 13f;
    [SerializeField] private Color localRowColor = new(.87f, 1f, .84f, 1f);

    private readonly List<Row> ranked = new(4);
    private bool[] occupied;
    private NetworkPlayer[] players;
    private RectTransform layout;
    private Canvas canvas;
    private PlayerWorkHud workHud;
    private Vector2 cardSize;

    private void Awake()
    {
        var gameScene = FindFirstObjectByType<MultiplayerGameScene>();
        if (gameScene != null) players = gameScene.Players;
        layout = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        workHud = GetComponentInParent<PlayerWorkHud>();
        cardSize = rows.Length > 0 ? rows[0].panel.sizeDelta : Vector2.zero;
        occupied = new bool[rows.Length];
        for (int slot = 0; slot < rows.Length; slot++)
        {
            var row = rows[slot];
            row.Slot = slot;
            row.BackgroundColor = row.background.color;
            row.panel.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (players == null) return;

        ranked.Clear();
        Array.Clear(occupied, 0, occupied.Length);
        foreach (var player in players)
        {
            if (player == null || !player.IsSpawned || !player.Assigned.Value) continue;
            int slot = player.Slot.Value;
            if (slot < 0 || slot >= rows.Length || occupied[slot]) continue;

            var row = rows[slot];
            occupied[slot] = true;
            int score = player.Score.Value;
            var nickname = player.Nickname.Value;
            bool local = player.Local;
            if (!row.Visible || row.Nickname != nickname) row.nicknameLabel.text = nickname.ToString();
            if (!row.Visible || row.Score != score) row.scoreLabel.text = score.ToString();
            if (!row.Visible || row.Local != local) row.background.color = local ? localRowColor : row.BackgroundColor;
            row.Nickname = nickname;
            row.Score = score;
            row.Local = local;
            ranked.Add(row);
        }

        for (int slot = 0; slot < rows.Length; slot++)
        {
            var row = rows[slot];
            if (occupied[slot] || !row.Visible) continue;
            row.panel.gameObject.SetActive(false);
            row.Visible = false;
        }

        ranked.Sort(CompareRows);
        FitLayout();
        float now = Time.unscaledTime;
        for (int rank = 0; rank < ranked.Count; rank++)
            PlaceRow(ranked[rank], rank, now);
    }

    private void FitLayout()
    {
        if (ranked.Count == 0 || canvas == null || Screen.width <= 0 || Screen.height <= 0) return;
        float campaignBottom = workHud != null ? workHud.CampaignBottom : float.PositiveInfinity;
        Rect area = GameplayHudLayout.RankingArea(GameplayHudLayout.WebSafeArea, campaignBottom);
        var topLeft = new Vector2(area.xMin, area.yMax);
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)layout.parent, topLeft,
                canvas.worldCamera, out var worldPosition))
            layout.position = worldPosition;

        float scale = canvas.scaleFactor;
        float width = (cardSize.x * (LeaderScale + ranked.Count - 1) + cardGap * (ranked.Count - 1)) * scale;
        float height = (cardSize.y * LeaderScale + passingOffset) * scale;
        float availableHeight = Screen.width <= 420 ? Mathf.Min(area.height, 85f) : area.height;
        float fit = Mathf.Clamp01(Mathf.Min(area.width / Mathf.Max(width, 1f), availableHeight / Mathf.Max(height, 1f)));
        layout.localScale = Vector3.one * fit;
    }

    private static int CompareRows(Row first, Row second)
    {
        int scoreOrder = second.Score.CompareTo(first.Score);
        return scoreOrder != 0 ? scoreOrder : first.Slot.CompareTo(second.Slot);
    }

    private void PlaceRow(Row row, int rank, float now)
    {
        float x = rank == 0 ? 0f : (LeaderScale + rank - 1) * cardSize.x + rank * cardGap;
        var target = new Vector2(x, 0f);
        if (!row.Visible)
        {
            row.MoveOrigin = target;
            row.MoveStarted = now - moveDuration;
            row.MovingUp = false;
            row.panel.anchoredPosition = target;
            ShowRank(row, rank);
            row.panel.gameObject.SetActive(true);
            row.Rank = rank;
            row.Visible = true;
            return;
        }

        if (row.Rank != rank)
        {
            row.MoveOrigin = row.panel.anchoredPosition;
            row.MoveStarted = now;
            row.MovingUp = rank < row.Rank;
            ShowRank(row, rank);
            row.Rank = rank;
        }

        float progress = Mathf.Clamp01((now - row.MoveStarted) / moveDuration);
        var position = Vector2.LerpUnclamped(row.MoveOrigin, target, Mathf.SmoothStep(0f, 1f, progress));
        if (row.MovingUp && progress < 1f) position.y -= Mathf.Sin(progress * Mathf.PI) * passingOffset;
        if (row.panel.anchoredPosition != position) row.panel.anchoredPosition = position;
    }

    private void ShowRank(Row row, int rank)
    {
        bool leading = rank == 0;
        row.rankLabel.text = rank < Ordinals.Length ? Ordinals[rank] : (rank + 1).ToString();
        row.panel.localScale = Vector3.one * (leading ? LeaderScale : 1f);
        row.background.sprite = leading ? leaderCard : rankCard;
        row.crown.SetActive(leading);
    }
}

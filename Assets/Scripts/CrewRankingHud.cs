using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;

public sealed class CrewRankingHud : MonoBehaviour
{
    [Serializable]
    private sealed class Row
    {
        public RectTransform panel;
        public UnityEngine.UI.Image background;
        public TMP_Text rankLabel;
        public TMP_Text nicknameLabel;
        public TMP_Text scoreLabel;

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
    [SerializeField, Min(1f)] private float rowSpacing = 58f;
    [SerializeField, Min(0.01f)] private float moveDuration = 0.28f;
    [SerializeField, Min(0f)] private float passingOffset = 24f;
    [SerializeField] private Color localRowColor = new(0.25f, 0.32f, 0.23f, 0.97f);

    private readonly List<Row> ranked = new(4);
    private bool[] occupied;
    private NetworkPlayer[] players;

    private void Awake()
    {
        var gameScene = FindFirstObjectByType<MultiplayerGameScene>();
        if (gameScene != null) players = gameScene.Players;
        occupied = new bool[rows.Length];
        for (int slot = 0; slot < rows.Length; slot++)
        {
            var row = rows[slot];
            row.Slot = slot;
            row.BackgroundColor = row.background.color;
            row.panel.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (players == null) return;

        ranked.Clear();
        Array.Clear(occupied, 0, occupied.Length);
        foreach (var player in players)
        {
            if (player == null || !player.IsSpawned || !player.Assigned.Value || player.Bot.Value) continue;
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
        float now = Time.unscaledTime;
        for (int rank = 0; rank < ranked.Count; rank++)
            PlaceRow(ranked[rank], rank, now);
    }

    private static int CompareRows(Row first, Row second)
    {
        int scoreOrder = second.Score.CompareTo(first.Score);
        return scoreOrder != 0 ? scoreOrder : first.Slot.CompareTo(second.Slot);
    }

    private void PlaceRow(Row row, int rank, float now)
    {
        var target = new Vector2(0f, -rank * rowSpacing);
        if (!row.Visible)
        {
            row.MoveOrigin = target;
            row.MoveStarted = now - moveDuration;
            row.MovingUp = false;
            row.panel.anchoredPosition = target;
            row.rankLabel.text = (rank + 1).ToString();
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
            row.rankLabel.text = (rank + 1).ToString();
            row.Rank = rank;
        }

        float progress = Mathf.Clamp01((now - row.MoveStarted) / moveDuration);
        var position = Vector2.LerpUnclamped(row.MoveOrigin, target, Mathf.SmoothStep(0f, 1f, progress));
        // Give the overtaking row room to pass while the displaced rows move straight down.
        if (row.MovingUp && progress < 1f) position.x += Mathf.Sin(progress * Mathf.PI) * passingOffset;
        if (row.panel.anchoredPosition != position) row.panel.anchoredPosition = position;
    }
}

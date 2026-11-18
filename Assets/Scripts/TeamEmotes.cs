using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 팀 이모티콘 표시. 보내기(숫자키·메뉴)와 네트워크는 CrewControls·NetworkPlayer.Emote가 맡고,
// 여기서는 받은 이모티콘을 보낸 캐릭터 옆 아이콘 말풍선과 상단 한 줄 알림(덮어쓰기)으로 보여준다.
public sealed class TeamEmotes : MonoBehaviour
{
    [SerializeField] private EmoteSet emotes;
    [SerializeField] private GameObject notice;
    [SerializeField] private Image noticeIcon;
    [SerializeField] private TMP_Text noticeLabel;
    [SerializeField] private RectTransform aimNotice, campaign;
    [SerializeField] private TMP_Text aimTitle, aimAccuracy, aimTime;
    [SerializeField] private Image aimFill;
    [SerializeField] private NetworkPlayer secondSeat;
    private Canvas canvas;
    [Tooltip("팀 알림 표시 시간(초)")]
    [SerializeField, Min(0f)] private float noticeSeconds = 4.8f;

    private float noticeHideTime;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        notice.SetActive(false);
        if (aimNotice != null) aimNotice.gameObject.SetActive(false);
    }
    private void OnEnable() => NetworkPlayer.Emoted += Show;
    private void OnDisable() => NetworkPlayer.Emoted -= Show;

    private void Update()
    {
        if (notice.activeSelf && Time.unscaledTime >= noticeHideTime) notice.SetActive(false);
    }

    private void LateUpdate()
    {
        if (aimNotice == null || canvas == null || Screen.height == 0) return;
        var player = NetworkPlayer.LocalPlayer;
        if (player == null || !player.Timing.IsCannonFocus)
            player = secondSeat != null && secondSeat.SecondLocal && secondSeat.Timing.IsCannonFocus ? secondSeat : null;
        bool aiming = CartStations.Running && player != null;
        aimNotice.gameObject.SetActive(aiming);
        bool showEmote = notice.activeSelf;
        if (!aiming && !showEmote) return;
        var safe = Screen.safeArea;
        float height = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * .11f, 66f, 88f);
        float width = Mathf.Min(760f, safe.width - 20f);
        float top = Mathf.Min(Screen.height * .78f + height, safe.yMax - 6f);
        if (campaign != null)
        {
            Vector2 lower = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,
                campaign.TransformPoint(new Vector3(0, campaign.rect.yMin, 0)));
            top = Mathf.Min(top, lower.y - 6f);
        }
        float gap = 10f;
        float emoteOffset = 0f;
        float room = safe.width * Mathf.Max(1f, top - safe.yMin);
        if (aiming && showEmote && Screen.width > Screen.height && width * (height * 2f + gap) > room * .4f)
        {
            gap = 4f;
            width = (Mathf.Min(760f, safe.width - 32f) - gap) * .5f;
            emoteOffset = (width + gap) * -.5f;
        }
        if (aiming)
        {
            Place(aimNotice, safe.center.x, top, width, height);
            var game = CartStations.Instance;
            float accuracy = Mathf.Clamp01(player.Cannon.Accuracy);
            aimTitle.SetText(game.IsFiring ? "착탄점을 조정하세요" : "왼쪽 스틱으로 착탄점을 맞추세요");
            aimAccuracy.SetText("{0:0}%", accuracy * 100f);
            aimFill.fillAmount = accuracy;
            var track = (RectTransform)aimFill.rectTransform.parent;
            track.anchorMin = new Vector2(Mathf.Max(.079f, 42f / width), track.anchorMin.y);
            if (game.IsFiring)
                aimTime.SetText("{0:0.0}초", Mathf.Max(0f, (float)(game.FuseDeadline.Value - player.NetworkManager.ServerTime.Time)));
            else aimTime.SetText("");
        }
        if (showEmote)
            Place((RectTransform)notice.transform, safe.center.x + emoteOffset, top - (aiming ? height + gap : 0f), width, height);
    }

    private void Place(RectTransform rect, float x, float top, float width, float height)
    {
        var parent = (RectTransform)rect.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(x, top - height * .5f),
            canvas.worldCamera, out var local);
        rect.anchoredPosition = local;
        rect.sizeDelta = new Vector2(width, height) / canvas.scaleFactor;
    }

    private void Show(NetworkPlayer player, int key)
    {
        if (key < 0 || key >= emotes.Count) return;
        player.ShowEmoteBubble(emotes[key].icon);
        noticeIcon.sprite = emotes[key].icon;
        noticeLabel.text = $"P{player.Slot.Value + 1} 플레이어가 {emotes[key].message}";
        noticeHideTime = Time.unscaledTime + noticeSeconds;
        notice.SetActive(true);
    }
}

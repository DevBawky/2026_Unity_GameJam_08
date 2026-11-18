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
    [Tooltip("팀 알림 표시 시간(초)")]
    [SerializeField, Min(0f)] private float noticeSeconds = 4.8f;

    private float noticeHideTime;

    private void Awake() => notice.SetActive(false);
    private void OnEnable() => NetworkPlayer.Emoted += Show;
    private void OnDisable() => NetworkPlayer.Emoted -= Show;

    private void Update()
    {
        if (notice.activeSelf && Time.unscaledTime >= noticeHideTime) notice.SetActive(false);
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

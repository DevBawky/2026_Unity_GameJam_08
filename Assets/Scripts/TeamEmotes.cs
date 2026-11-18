using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// 팀 이모티콘 입력과 상단 알림. 이모티콘 버튼 → 목록에서 고르거나, PC에서는 숫자키 1~7로 바로 보낸다.
// 팀 알림은 상단 한 줄이며, 새 이모티콘이 오면 덮어쓴다.
public sealed class TeamEmotes : MonoBehaviour
{
    private static readonly Key[] DigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7 };
    private static readonly Key[] NumpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7 };

    [SerializeField] private EmoteSet emotes;
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject notice;
    [SerializeField] private Image noticeIcon;
    [SerializeField] private TMP_Text noticeLabel;
    [Tooltip("팀 알림 표시 시간(초)")]
    [SerializeField, Min(0f)] private float noticeSeconds = 4.8f;

    private float noticeHideTime;

    private void Awake()
    {
        notice.SetActive(false);
        menu.SetActive(false);
    }

    private void OnEnable() => NetworkPlayer.Emoted += Show;
    private void OnDisable() => NetworkPlayer.Emoted -= Show;

    private void Update()
    {
        var local = NetworkPlayer.LocalPlayer;
        bool usable = local != null && CartStations.EmoteAllowed;
        if (toggleButton.gameObject.activeSelf != usable) toggleButton.gameObject.SetActive(usable);
        if (!usable && menu.activeSelf) menu.SetActive(false);
        if (usable) toggleButton.interactable = local.CanEmote;
        if (usable && Keyboard.current != null)
            for (int i = 0; i < DigitKeys.Length; i++)
                if (Keyboard.current[DigitKeys[i]].wasPressedThisFrame || Keyboard.current[NumpadKeys[i]].wasPressedThisFrame) Choose(i);
        if (notice.activeSelf && Time.unscaledTime >= noticeHideTime) notice.SetActive(false);
    }

    public void ToggleMenu() => menu.SetActive(!menu.activeSelf);

    public void Choose(int key)
    {
        menu.SetActive(false);
        if (NetworkPlayer.LocalPlayer != null) NetworkPlayer.LocalPlayer.SendEmote(key);
    }

    private void Show(NetworkPlayer player, int key)
    {
        if (key >= emotes.Count) return;
        player.ShowEmoteBubble(emotes[key].icon);
        noticeIcon.sprite = emotes[key].icon;
        noticeLabel.text = $"P{player.Slot.Value + 1} 플레이어가 {emotes[key].message}";
        noticeHideTime = Time.unscaledTime + noticeSeconds;
        notice.SetActive(true);
    }
}

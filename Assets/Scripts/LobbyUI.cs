using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyUI : MonoBehaviour
{
    [SerializeField] private LobbySession session;
    [SerializeField] private GameObject titlePanel, joinPanel, roomPanel, settingsPanel;
    [SerializeField] private TMP_InputField nickname, roomCode;
    [SerializeField] private TMP_Text codeLabel, statusLabel;
    [SerializeField] private TMP_Text[] playerLabels;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingLabel;
    [SerializeField] private Button createButton, joinButton, backButton, leaveButton, startButton;
    [SerializeField] private GameObject practicePanel;
    private int pendingSeats;
    private bool joining;
    private float copiedUntil;

    private void Start()
    {
        nickname.text = PlayerPrefs.GetString("Lobby.Nickname", "");
        ShowTitle();
    }
    private void Update()
    {
        bool connected = session.InRoom;
        titlePanel.SetActive(!joining && !connected && !practicePanel.activeSelf);
        joinPanel.SetActive(joining && !connected);
        roomPanel.SetActive(connected);
        bool busy = session.Busy;
        loadingPanel.SetActive(busy);
        if (busy)
        {
            loadingPanel.transform.SetAsLastSibling();
            loadingLabel.text = session.Status;
        }
        nickname.interactable = roomCode.interactable = !busy;
        createButton.interactable = joinButton.interactable = backButton.interactable = !busy;
        leaveButton.interactable = !busy && connected && !session.Roster.Starting.Value;
        startButton.gameObject.SetActive(session.IsHost);
        startButton.interactable = session.IsHost && !busy && !session.Roster.Starting.Value;
        statusLabel.text = Time.unscaledTime < copiedUntil ? "방 코드를 복사했습니다." : session.Status;
        if (!connected) return;
        codeLabel.text = "방 코드: " + session.Roster.Code.Value;
        for (int i = 0; i < playerLabels.Length; i++)
        {
            playerLabels[i].text = $"P{i + 1}\n대기 중";
            foreach (LobbyPlayerInfo p in session.Roster.Players)
                if (p.Slot == i) playerLabels[i].text = $"P{i + 1}\n{p.Nickname}" + (p.ClientId == 0 ? " (방장)" : "");
        }
    }
    public void ShowJoin() { joining = true; }
    public void ShowTitle() { if (!session.Busy) joining = false; }
    public void OpenSettings() { settingsPanel.SetActive(true); settingsPanel.transform.SetAsLastSibling(); }
    public void CloseSettings() { settingsPanel.SetActive(false); }
    public void Quit() { Application.Quit(); }
    private void SaveNickname() { PlayerPrefs.SetString("Lobby.Nickname", nickname.text.Trim()); PlayerPrefs.Save(); }
    public async void CreateRoom() { SaveNickname(); await session.CreateRoomAsync(nickname.text); }
    public async void JoinRoom() { SaveNickname(); await session.JoinRoomAsync(nickname.text, roomCode.text); }
    public async void LeaveRoom() { await session.LeaveRoomAsync(); }
    public void StartGame() { session.StartGame(); }
    public void CopyCode()
    {
        if (!session.InRoom) return;
        GUIUtility.systemCopyBuffer = session.Roster.Code.Value.ToString();
        copiedUntil = Time.unscaledTime + 2f;
    }
    public void PracticeSolo() => ChoosePractice(1);
    public void PracticePair() => ChoosePractice(2);
    private void ChoosePractice(int seats)
    {
        pendingSeats = seats;
        practicePanel.SetActive(true);
        practicePanel.transform.SetAsLastSibling();
    }
    public void CancelPractice() => practicePanel.SetActive(false);
    public void BeginPractice() => session.StartPractice(pendingSeats);
    public void BeginTest() => session.StartPractice(pendingSeats, true);
}

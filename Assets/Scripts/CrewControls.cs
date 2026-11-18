using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class CrewControls : MonoBehaviour
{
    public static readonly string[] Messages={"포탄 요청","장전 요청","바퀴 수리","마차 조작","아이 돌봄","좋아요","놀람"};
    [SerializeField] private NetworkPlayer[] crew;
    [SerializeField] private GameObject menu;
    [SerializeField] private Button[] options;
    [SerializeField] private TMP_Text team, task;
    private NetworkPlayer menuOwner;
    [SerializeField] private GameObject testSettingsButton, testSettingsPanel;
    public void ToggleTestSettings() => testSettingsPanel.SetActive(!testSettingsPanel.activeSelf);
    private float nextText;
    private static readonly Key[] Digits={Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7,Key.Digit8,Key.Digit9};

    private void Awake()
    {
        for(int i=0;i<options.Length;i++)
        {
            int choice=i+1;
            options[i].onClick.AddListener(()=>Select(choice));
        }
    }
    public async void LeaveGame()
    {
        var session=FindFirstObjectByType<LobbySession>();
        if(session!=null) await session.LeaveRoomAsync();
    }
    public void Toggle() => Open(NetworkPlayer.LocalPlayer);
    private void Open(NetworkPlayer owner)
    {
        if(owner==null) return;
        bool show=!menu.activeSelf || menuOwner!=owner;
        menuOwner=owner;
        menu.SetActive(show);
    }
    private void Select(int choice)
    {
        if(menuOwner!=null) menuOwner.SendEmote(choice);
        menu.SetActive(false);
    }
    private void Update()
    {
        if (testSettingsButton != null) testSettingsButton.SetActive(CartStations.Instance != null && CartStations.Instance.Endless.Value);
        var local=NetworkPlayer.LocalPlayer;
        if(local==null) return;
        var keys=Keyboard.current;
        var stations=CartStations.Instance;
        if(keys!=null)
        {
            if (keys.escapeKey.wasPressedThisFrame)
            {
                var owner = local;
                if (!local.Timing.HasStationAccess && LobbySession.LocalSeats == 2 && crew[1].Timing.HasStationAccess)
                    owner = crew[1];
                owner.Timing.ExitStation();
            }
            if(keys.tabKey.wasPressedThisFrame) Open(local);
            if(LobbySession.LocalSeats==2 && keys.rightCtrlKey.wasPressedThisFrame) Open(crew[1]);
            for(int i=0;i<Digits.Length;i++)
                if(keys[Digits[i]].wasPressedThisFrame)
                {
                    if(stations.ChoosingUpgrade) (LobbySession.LocalSeats==2 && keys.rightShiftKey.isPressed ? crew[1] : local).SubmitChoice(i);
                    else if(i<Messages.Length) { if(!menu.activeSelf) menuOwner=local; Select(i+1); }
                }
        }
        if(Time.unscaledTime<nextText) return;
        nextText=Time.unscaledTime+0.12f;
        string news="";
        double now=local.NetworkManager.ServerTime.Time;
        foreach(var p in crew)
            if(p.Assigned.Value && p.Emote.Value>0 && now-p.EmoteTime.Value<3) news+=$"{p.Nickname.Value} · {Messages[p.Emote.Value-1]}  ";
        team.text=news;
        task.text = stations.ChoosingUpgrade && LobbySession.LocalSeats == 2 ? "P1  1–9     P2  Right Shift + 1–9" : "";
    }
}

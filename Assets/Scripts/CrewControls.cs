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
    [SerializeField] private TMP_Text[] overheadActions;
    private NetworkPlayer menuOwner;
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
        for (int i = 0; i < overheadActions.Length; i++)
        {
            var actor = crew[i];
            overheadActions[i].text = actor.Local && CartStations.Running ? ActionLabel(actor, stations) : "";
        }
        string info=TaskText(local,stations);
        if(LobbySession.LocalSeats==2 && crew[1].Assigned.Value) info+="   |   P2 "+TaskText(crew[1],stations);
        task.text=info;
    }
    private static string ActionLabel(NetworkPlayer p, CartStations game)
    {
        if (p.Holding.Value) return "포탄 붙잡기";
        if (p.Timing.NearbyRoyal is StationAction royal)
            return game.AvailableCare(royal, p) switch
            {
                CareAction.Soothe => "달래기", CareAction.Reseat => "앉히기",
                CareAction.Feed => "간식 주기", CareAction.PickUp => "안기",
                CareAction.Attend => "돌보기", _ => ""
            };
        if (!p.Timing.HasStationAccess) return "";
        return p.Timing.CurrentAction switch
        {
            StationAction.Kitchen => "간식 만들기 · 길게",
            StationAction.Driver => "채찍질",
            StationAction.Cannon => game.IsFiring ? "조준" : "점화",
            StationAction.ShellPile => "포탄 꺼내기",
            StationAction.ShellDeposit => "포탄 적재",
            StationAction.Reload => "장전",
            StationAction.WheelRack => "예비 바퀴 들기",
            StationAction.WheelRepair => game.WheelRepairSteps == 0 ? "바퀴 끼우기" : "차축 핀 고정",
            _ => ""
        };
    }

    private static string TaskText(NetworkPlayer p,CartStations game)
    {
        if(game.ChoosingUpgrade) return LobbySession.LocalSeats==2 ? "P1  1–9     P2  Right Shift + 1–9" : "";
        if(!CartStations.Running) return "";
        string exit=p.SecondLocal?"RShift":"Q";
        if(p.Holding.Value) return "포탄 붙잡기!";
        if (p.Timing.NearbyRoyal is StationAction royal)
        {
            var child = royal == StationAction.Princess ? game.Princess.Value : game.Prince.Value;
            string action = ActionLabel(p, game);
            string status = $"관심 {child.Attention:0} · 배고픔 {child.Hunger:0}";
            return action.Length == 0 ? status : $"{(p.SecondLocal ? "Enter" : "Space")} {action} · {status}";
        }
        switch(p.Timing.HasStationAccess ? p.Timing.CurrentAction : null)
        {
            case StationAction.Kitchen: return "간식 만들기 · 행동 버튼 길게";
            case StationAction.ShellDeposit: return "포탄 적재 · 행동 버튼";
            case StationAction.Cannon: return $"{ActionLabel(p, game)} · {exit} 이탈";
            case StationAction.Driver:
                var right=p.DriverAim.Value.y>=0;
                int marks=right?game.RightWounds.Value:game.LeftWounds.Value;
                return $"{(right?"오른말 ↑":"왼말 ↓")} · {CartStations.HorseZone(p.DriverAim.Value.x)} · 말 {game.Cart.LeftStep}:{game.Cart.RightStep} · 목표 Y {game.Cart.Lane} · 상처 {marks} {(marks>=8?"화남":marks>=5?"피로":"")}   {exit} 이탈";
        }
        if(p.SpareWheel.Value) return "예비 바퀴 · 고장 난 모서리로";
        if(p.Snack.Value) return "간식 · 갑판 좌석으로";
        if(p.Carrying.Value) return "포탄 · 갑판 적재칸으로";
        return p.Timing.HasStationAccess ? ActionLabel(p, game) : "";
    }
}


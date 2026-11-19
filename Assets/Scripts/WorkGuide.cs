using UnityEngine;

internal readonly struct WorkGuide
{
    internal readonly string Action, Hint, ProgressName;
    internal readonly bool Timed;
    internal readonly float Progress, Seconds;
    internal readonly Color Color;

    private static readonly Color Pink = new Color32(245, 200, 224, 255);
    private static readonly Color Sand = new Color32(240, 194, 130, 255);
    private static readonly Color Blue = new Color32(155, 205, 225, 255);
    private static readonly Color Green = new Color32(167, 214, 170, 255);
    private static readonly Color Orange = new Color32(255, 207, 133, 255);
    private static readonly Color Purple = new Color32(187, 145, 217, 255);

    private WorkGuide(string action = null, string hint = null, Color color = default,
        bool timed = false, float progress = -1f, string progressName = null, float seconds = -1f)
    {
        Action = LocalizationTable.T(action);
        Hint = LocalizationTable.T(hint);
        Color = color;
        Timed = timed;
        Progress = progress;
        ProgressName = LocalizationTable.T(progressName);
        Seconds = seconds;
    }

    internal bool HasProgress => !string.IsNullOrEmpty(Hint) && Progress >= 0f;

    internal static WorkGuide For(NetworkPlayer player, CartStations game)
    {
        var timing = player.Timing;
        if (player.Holding.Value) return new WorkGuide("포탄 붙잡기", color: Orange, timed: true);
        if (game.CarriedChild(player, out var child)) return CarryingRoyal(player, game, child);
        if (timing.NearbyRoyal is StationAction royal)
        {
            string name = royal == StationAction.Princess ? "공주" : "왕자";
            var care = game.AvailableCare(royal, player);
            if (care == CareAction.Feed) return new WorkGuide(name + " 먹이기", name + "에게 간식을 주세요", Sand);
            if (care == CareAction.PickUp) return new WorkGuide("안아 올리기", name + "를 안아 올리세요", Pink);
        }
        if (player.CanUseStairShelf())
            return new WorkGuide(player.InCellar ? "선반에 올리기" : "선반에서 받기",
                player.InCellar ? "계단 선반에 포탄을 올려 두세요" : "계단 선반의 포탄을 받아 가세요", Blue);
        if (player.Carrying.Value)
            return new WorkGuide(hint: player.InCellar ? "계단으로 1층에 올라가세요" : "5시 방향 파란 적재칸으로 옮기세요", color: Blue);
        if (player.Snack.Value)
            return new WorkGuide(hint: player.InCellar ? "간식을 들고 1층으로 올라가세요" : "앉아 있는 아이에게 간식을 가져가세요", color: Sand);

        if (timing.IsCannonFocus)
        {
            float accuracy = Mathf.Clamp01(player.Cannon.Accuracy);
            if (game.IsFiring)
            {
                float left = Mathf.Max(0f, (float)(game.FuseDeadline.Value - player.NetworkManager.ServerTime.Time));
                return new WorkGuide("점화 중", "착탄점을 조정하세요", Orange, progress: accuracy, progressName: "조준률", seconds: left);
            }
            return new WorkGuide("점화", "왼쪽 스틱으로 착탄점을 맞추세요", Orange, progress: accuracy, progressName: "조준률");
        }
        if (timing.HasStationAccess)
        {
            switch (timing.CurrentAction)
            {
                case StationAction.Driver: return Driving(player, game);
                case StationAction.Kitchen:
                    return new WorkGuide("길게 눌러 만들기", "행동을 길게 눌러 간식을 만드세요", Sand,
                        progress: player.CookProgress.Value / CartRules.Active.cookSeconds);
                case StationAction.WheelRack: return new WorkGuide("예비 바퀴 들기", "예비 바퀴를 드세요", Sand);
                case StationAction.CannonCleaning:
                    return new WorkGuide("청소하기", LocalizationTable.F("포신 청소 {0}/{1}", game.CleaningTaps, CartStations.CleaningTapsRequired), Orange,
                        progress: (float)game.CleaningTaps / CartStations.CleaningTapsRequired);
                case StationAction.Reload:
                    return game.IsDeposited ? new WorkGuide("장전", color: Green, timed: true)
                        : new WorkGuide("장전 대기", "적재칸에 포탄을 가져오세요", Green);
                case StationAction.ShellPile: return new WorkGuide("포탄 들기", color: Blue, timed: true);
                case StationAction.WheelRepair:
                    string step = game.WheelRepairSteps == 0 ? "바퀴 끼우기" : "차축 핀 고정";
                    return new WorkGuide(step, color: Green, timed: true);
            }
        }
        if (player.SpareWheel.Value)
            return new WorkGuide(hint: player.InCellar ? "고장 난 바퀴의 수리 원으로 이동하세요" : "계단으로 지하층에 내려가세요", color: Sand);
        if (timing.NearbyStation == StationAction.Reload)
        {
            if (game.NeedsCleaning) return new WorkGuide("장전 대기", "포신을 먼저 청소하세요", Orange);
            if (!game.IsDeposited && !game.IsLoaded)
                return new WorkGuide("장전 대기", "적재칸에 포탄을 가져오세요", Green);
        }
        if (timing.NearbyStation == StationAction.WheelRepair && game.WheelDetached && game.WheelRepairSteps == 0)
            return new WorkGuide(hint: game.SpareKitReady.Value ? "비상 바퀴 꾸러미로 바로 수리하세요" : "12시 방향에서 예비 바퀴를 가져오세요", color: Sand);
        // 정비 안내등: 어느 층에서든 빠진 바퀴와 남은 수리 단계를 알려 준다.
        if (game.WheelDetached && game.Owns(ShopPerk.MaintenanceLamp))
            return new WorkGuide(hint: LocalizationTable.F("{0}번 바퀴 고장 · 수리 {1}/{2}단계", game.DetachedTireNumber, game.WheelRepairSteps, CartStations.WheelRepairStepsNeeded), color: Sand);
        return default;
    }

    private static WorkGuide CarryingRoyal(NetworkPlayer player, CartStations game, RiderState child)
    {
        bool prince = game.Prince.Value.Pose == RiderPose.Carried && game.Prince.Value.CarrierSlot == player.Slot.Value;
        string name = prince ? "왕자" : "공주";
        if (child.Upset)
        {
            float soothe = child.Soothing / game.SootheTarget;
            return child.Bored
                ? new WorkGuide(hint: "지루해요 · 마차 앞뒤나 지하층으로 가세요", color: new Color32(230, 186, 130, 255), progress: soothe)
                : new WorkGuide(hint: "안고 걸어서 달래 주세요", color: Pink, progress: soothe);
        }
        var seat = prince ? StationAction.RoyalFamily : StationAction.Princess;
        if (player.Timing.NearbyRoyal == seat && !player.InCellar)
            return new WorkGuide("앉히기", name + "를 좌석에 앉히세요", Pink);
        return new WorkGuide(hint: player.InCellar ? "계단으로 1층에 올라가세요" : name + "를 좌석으로 데려가세요", color: Pink);
    }

    private static WorkGuide Driving(NetworkPlayer player, CartStations game)
    {
        bool right = player.DriverAim.Value.y >= 0;
        var horse = right ? game.RightHorse.Value : game.LeftHorse.Value;
        string side = right ? "오른말" : "왼말";
        int runaway = game.RunawayDirection;
        if (runaway != 0)
        {
            var gallop = runaway > 0 ? game.RightHorse.Value : game.LeftHorse.Value;
            var hint = runaway > 0 ? "오른말 폭주 · 위로 급회전" : "왼말 폭주 · 아래로 급회전";
            return new WorkGuide(hint: hint, color: new Color32(255, 163, 135, 255),
                progress: 1f - (float)gallop.Remaining / CartStations.RunawayTicks);
        }
        if (horse.Mood == HorseMood.Tired)
            return new WorkGuide(hint: side + " 지침 · 체력 회복 중", color: new Color32(177, 229, 204, 255),
                progress: 1f - horse.Remaining / 120f);
        if (horse.Mood == HorseMood.Angry)
            return new WorkGuide(hint: side + " 놀람 · 잠시 쉬게 하세요", color: Orange);
        if (!game.AimsAtSteeringZone(player.DriverAim.Value))
            return new WorkGuide("채찍질", "왼쪽 스틱으로 말 엉덩이를 겨냥하세요", Purple);
        int hits = right ? game.RightWounds.Value : game.LeftWounds.Value;
        string whipFormat = right ? "오른말 채찍질 · {0}/{1}회" : "왼말 채찍질 · {0}/{1}회";
        return new WorkGuide("채찍질", LocalizationTable.F(whipFormat, hits, CartStations.RunawayHits), Purple,
            progress: (float)hits / CartStations.RunawayHits);
    }
}

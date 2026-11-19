using UnityEngine;

public sealed partial class CartStations
{
    private readonly int[] botJobs = { -1, -1, -1, -1 };
    private readonly int[] nextBotAction = new int[4];
    private readonly bool[] walkingRight = new bool[4];

    private bool BotCanTake(StationAction action, NetworkPlayer bot)
    {
        foreach (var player in gameScene.Players)
        {
            if (!player.Assigned.Value || player == bot) continue;
            if (!player.Bot.Value && player.Timing.IsInActionArea(action)) return false;
            if (player.Bot.Value && player.Slot.Value < bot.Slot.Value && botJobs[player.Slot.Value] == (int)action) return false;
        }
        return true;
    }
    private StationAction? BotJob(NetworkPlayer bot, bool driverNeededElsewhere)
    {
        if (bot.Carrying.Value) return StationAction.ShellDeposit;
        if (bot.SpareWheel.Value && wheelDetached) return StationAction.WheelRepair;
        if (bot.Snack.Value)
        {
            bool prince = Prince.Value.Pose == RiderPose.Seated;
            bool princess = Princess.Value.Pose == RiderPose.Seated;
            if (prince && (!princess || Prince.Value.Hunger >= Princess.Value.Hunger)) return StationAction.RoyalFamily;
            return princess ? StationAction.Princess : null;
        }
        foreach (var action in RoyalJobs)
        {
            var child = action == StationAction.Princess ? Princess.Value : Prince.Value;
            if (child.Pose != RiderPose.Seated && child.Pose != RiderPose.Carried && BotCanTake(action,bot)) return action;
        }
        if (wheelDetached && BotCanTake(StationAction.WheelRack,bot))
            return wheelRepairSteps > 0 ? StationAction.WheelRepair : StationAction.WheelRack;
        if ((Prince.Value.Hunger >= 70 || Princess.Value.Hunger >= 70) && BotCanTake(StationAction.Kitchen, bot))
        {
            bool fedSoon = false;
            foreach (var actor in gameScene.Players) fedSoon |= actor.Assigned.Value && actor.Snack.Value;
            if (!fedSoon) return StationAction.Kitchen;
        }
        if (HasWork(StationAction.Cannon,bot) && BotCanTake(StationAction.Cannon,bot)) return StationAction.Cannon;
        if (HasWork(StationAction.CannonCleaning, bot) && BotCanTake(StationAction.CannonCleaning, bot))
            return StationAction.CannonCleaning;
        if (HasWork(StationAction.Reload,bot) && BotCanTake(StationAction.Reload,bot)) return StationAction.Reload;
        if (depositedShells < 2 && shellsInPile > 0 && BotCanTake(StationAction.ShellPile,bot)) return StationAction.ShellPile;
        if (!driverNeededElsewhere && BotCanTake(StationAction.Driver,bot)) return StationAction.Driver;
        return null;
    }
    private static readonly StationAction[] RoyalJobs = {StationAction.RoyalFamily, StationAction.Princess};

    private void AdvanceBots()
    {
        var driver = EvasionDriver(out int direction);
        foreach (var bot in gameScene.Players)
        {
            if (!bot.Bot.Value || !bot.Assigned.Value) continue;
            int slot = bot.Slot.Value;
            if (fuseLit && gunner == bot) continue;
            var walk = bot.GetComponent<PlayerMovement>();
            if (bot.Holding.Value)
            {
                if (bot.Timing.TimingSuccess(StationAction.ShellPile,true,NetworkManager.ServerTime.Time)) ResolveShellHold(bot,true);
                continue;
            }
            if (bot != driver && CarriedChild(bot,out var child) && child.Upset)
            {
                botJobs[slot] = -1;
                Vector2 pos = bot.transform.position;
                Bounds deck = cartBody.bounds;
                if (child.Bored) walk.WalkBot(walk.StairPoint(true) + Vector2.right, true, Step);
                else
                {
                    float x = walkingRight[slot] ? deck.max.x - .15f : deck.min.x + .15f;
                    if (Mathf.Abs(pos.x - x) < .15f) walkingRight[slot] = !walkingRight[slot];
                    walk.WalkBot(new Vector2(x, pos.y), bot.InCellar, Step);
                }
                continue;
            }
            StationAction? choice;
            if (HoldsStation(StationAction.Cannon, bot) && HasWork(CannonAction, bot)) choice = CannonAction;
            else if (bot == driver) choice = StationAction.Driver;
            else if (CarriedChild(bot, out _)) choice = IsCarrier(Prince.Value,bot) ? StationAction.RoyalFamily : StationAction.Princess;
            else choice = BotJob(bot, driver != null);
            botJobs[slot] = choice.HasValue ? (int)choice.Value : -1;
            if (!choice.HasValue)
            {
                ReleaseStations(bot.NetworkObjectId);
                continue;
            }
            var action = choice.Value;
            bool cellar = action is StationAction.Kitchen or StationAction.WheelRack or StationAction.WheelRepair or StationAction.ShellPile;
            var area = bot.Timing.AreaFor(action);
            if (area == null) continue;
            Vector2 dest = action == StationAction.WheelRepair && detachedTire != null ?
                (Vector2)WheelWorkPoint + Vector2.up * NetworkPlayer.CellarOffset : (Vector2)area.position;
            if (bot.CompletedStation.Value == (int)action)
            {
                float sign = dest.x < cartBody.bounds.center.x ? 1 : -1;
                dest.x += sign * (Mathf.Abs(area.rect.width * area.lossyScale.x) + .5f);
            }
            walk.WalkBot(dest,cellar,Step);
            bool reached = bot.InCellar == cellar && bot.Timing.IsInActionArea(action);
            bot.CookingHeld.Value = action == StationAction.Kitchen && reached;
            if (!reached || bot.CompletedStation.Value == (int)action) continue;
            bool royal = TimingArea.IsRoyal(action);
            if (!royal) ReserveStation(bot,(int)action);
            if (!CanUse(action,bot)) continue;
            if (action == StationAction.Driver)
            {
                if (bot != driver || careTicks < nextDodgeWhip) continue;
                bool right = direction > 0;
                var uv = horseWhipZones[right ? 1 : 0].SteeringArea.center;
                bot.DriverAim.Value = new Vector2(2f * uv.x - 1f, right ? uv.y : uv.y - 1f);
                StrikeHorse(bot, bot.DriverAim.Value);
                nextDodgeWhip = careTicks + DodgeWhipTicks;
                continue;
            }
            if (careTicks < nextBotAction[slot]) continue;
            if (action == StationAction.Cannon)
            {
                if (Enemy.Active.Count == 0) continue;
                Vector2 offset = ((Vector2)Enemy.Active[0].transform.position - (Vector2)cannonMuzzle.position) / WorldPerWebUnit;
                bot.Aim.Value = CannonAim.CursorForOffset(offset);
                Ignite(bot);
            }
            else if (action == StationAction.CannonCleaning)
            {
                Succeed(action, bot);
                nextBotAction[slot] = careTicks + 5;
            }
            else if (action == StationAction.ShellDeposit) Enter(action,bot);
            else if (action != StationAction.Kitchen)
            {
                if (action is StationAction.ShellPile or StationAction.Reload or StationAction.WheelRepair)
                    if (!bot.Timing.TimingSuccess(action,false,NetworkManager.ServerTime.Time)) continue;
                Succeed(action,bot);
                nextBotAction[slot] = careTicks + 8;
            }
        }
    }
}

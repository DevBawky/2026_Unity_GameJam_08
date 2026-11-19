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
            if (!player.Bot.Value && TimingArea.IsRoyal(action) && player.Timing.IsInActionArea(action)) return false;
            if (player.Bot.Value && player.Slot.Value < bot.Slot.Value && botJobs[player.Slot.Value] == (int)action) return false;
        }
        return TimingArea.IsRoyal(action) || !PlayerHasPriority(action);
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
        if (wheelDetached)
        {
            var wheelJob = wheelRepairSteps > 0 ? StationAction.WheelRepair : StationAction.WheelRack;
            if (BotCanTake(wheelJob, bot)) return wheelJob;
        }
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
        botSteering = 0f;
        var driver = EvasionDriver(out int direction);
        foreach (var bot in gameScene.Players)
        {
            if (!bot.Bot.Value || !bot.Assigned.Value) continue;
            int slot = bot.Slot.Value;
            var walk = bot.GetComponent<PlayerMovement>();
            if (bot.Holding.Value)
            {
                if (bot.Timing.TimingSuccess(StationAction.ShellPile,true,NetworkManager.ServerTime.Time)) ResolveShellHold(bot,true);
                continue;
            }
            bool carryingChild = CarriedChild(bot, out var child);
            if (carryingChild)
            {
                ReleaseStations(bot.NetworkObjectId);
                bot.CookingHeld.Value = false;
                if (child.Upset)
                {
                    botJobs[slot] = -1;
                    WalkToSoothe(bot, walk, child, slot);
                    continue;
                }
            }
            if (!carryingChild && fuseLit && gunner == bot)
            {
                if (PlayerHasPriority(StationAction.Cannon)) YieldBotStation(bot, walk, StationAction.Cannon);
                continue;
            }
            if (!carryingChild && botJobs[slot] >= 0)
            {
                var previous = (StationAction)botJobs[slot];
                if (!TimingArea.IsRoyal(previous) && bot.Timing.IsInActionArea(previous) && PlayerHasPriority(previous))
                {
                    YieldBotStation(bot, walk, previous);
                    continue;
                }
            }
            StationAction? choice;
            if (carryingChild) choice = IsCarrier(Prince.Value, bot) ? StationAction.RoyalFamily : StationAction.Princess;
            else if (HoldsStation(StationAction.Cannon, bot) && HasWork(CannonAction, bot)) choice = CannonAction;
            else if (bot == driver) choice = StationAction.Driver;
            else choice = BotJob(bot, driver != null);
            botJobs[slot] = choice.HasValue ? (int)choice.Value : -1;
            if (!choice.HasValue)
            {
                ReleaseStations(bot.NetworkObjectId);
                continue;
            }
            var action = choice.Value;
            bool royal = TimingArea.IsRoyal(action);
            if (!royal && PlayerHasPriority(action))
            {
                YieldBotStation(bot, walk, action);
                continue;
            }
            bool cellar = action is StationAction.Kitchen or StationAction.WheelRack or StationAction.WheelRepair or StationAction.ShellPile;
            Vector2 dest;
            if (royal)
            {
                bool princess = action == StationAction.Princess;
                var target = princess ? Princess.Value : Prince.Value;
                cellar = !carryingChild && target.InCellar;
                dest = RoyalInteractionPosition(target, princess ? 1 : 0);
            }
            else
            {
                var area = bot.Timing.AreaFor(action);
                if (area == null) continue;
                dest = action == StationAction.WheelRepair && detachedTire != null
                    ? WheelWorkPoint + Vector3.up * NetworkPlayer.CellarOffset : area.position;
                if (bot.CompletedStation.Value == (int)action)
                {
                    float sign = dest.x < cartBody.bounds.center.x ? 1 : -1;
                    dest.x += sign * (Mathf.Abs(area.rect.width * area.lossyScale.x) + .5f);
                }
            }
            walk.WalkBot(dest, cellar, Step);
            bool reached = bot.InCellar == cellar && (royal ? WithinRoyalInteraction(action, bot) : bot.Timing.IsInActionArea(action));
            bot.CookingHeld.Value = action == StationAction.Kitchen && reached;
            if (!reached || bot.CompletedStation.Value == (int)action) continue;
            if (!royal) ReserveStation(bot,(int)action);
            if (!CanUse(action,bot)) continue;
            if (action == StationAction.Driver)
            {
                botSteering = bot == driver ? direction : 0f;
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

    private void WalkToSoothe(NetworkPlayer bot, PlayerMovement walk, RiderState child, int slot)
    {
        Vector2 position = bot.transform.position;
        Bounds bounds = bot.InCellar ? cartBody.bounds : DeckMovementBounds;
        const float turnDistance = .15f;
        float left = bounds.min.x + turnDistance;
        float right = bounds.max.x - turnDistance;
        bool goRight = walkingRight[slot];
        if (child.Bored && !bot.InCellar)
            goRight = cart.transform.TransformPoint(child.BoredFrom).x < bounds.center.x;
        else if (Mathf.Abs(position.x - (goRight ? right : left)) < turnDistance)
            goRight = !goRight;
        walkingRight[slot] = goRight;
        walk.WalkBot(new Vector2(goRight ? right : left, position.y), bot.InCellar, Step);
    }

    private void YieldBotStation(NetworkPlayer bot, PlayerMovement walk, StationAction action)
    {
        ReleaseStations(bot.NetworkObjectId);
        bot.CookingHeld.Value = false;
        if (!bot.Timing.IsInActionArea(action)) return;
        Vector2 center = action == StationAction.WheelRepair
            ? WheelWorkPoint + Vector3.up * NetworkPlayer.CellarOffset
            : bot.Timing.AreaFor(action).position;
        float inward = center.x < cartBody.bounds.center.x ? 1f : -1f;
        Vector2 position = bot.transform.position;
        walk.WalkBot(new Vector2(center.x + inward * 48f * WorldPerWebUnit, position.y), bot.InCellar, Step);
    }
}

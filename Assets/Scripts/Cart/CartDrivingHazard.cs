using System;
using System.Collections.Generic;
using UnityEngine;

// A source reports the unsafe cart-pivot Y interval and the seconds until contact begins/ends.
// Falling objects use the same time for entry and exit. Sources only report known hazards.
public readonly struct CartDrivingHazard
{
    public const float Lookahead = 6f;
    public readonly float CenterY, RadiusY, Enter, Exit;

    public CartDrivingHazard(float centerY, float radiusY, float enter, float exit)
    {
        CenterY = centerY;
        RadiusY = radiusY;
        Enter = enter;
        Exit = exit;
    }
}

public sealed partial class CartStations
{
    public event Action<List<CartDrivingHazard>> CollectDrivingHazards;
    private readonly List<CartDrivingHazard> drivingHazards = new();
    private float DrivingRisk(int direction, float delay)
    {
        float risk = 0;
        foreach (var hazard in drivingHazards)
        {
            if (hazard.Exit < 0 || hazard.Enter > CartDrivingHazard.Lookahead) continue;
            float start = Mathf.Max(0, hazard.Enter);
            float end = Mathf.Clamp(hazard.Exit, start, CartDrivingHazard.Lookahead);
            float y0 = DrivingYAt(start, direction, delay);
            float y1 = DrivingYAt(end, direction, delay);
            float nearest = Mathf.Clamp(hazard.CenterY, Mathf.Min(y0,y1), Mathf.Max(y0,y1));
            float clearance = hazard.RadiusY + .08f;
            float worst = Mathf.Max(0, clearance - Mathf.Abs(nearest - hazard.CenterY));
            float atExit = Mathf.Max(0, clearance - Mathf.Abs(y1 - hazard.CenterY));
            risk += (worst + .5f * atExit) / (.25f + start);
        }
        return risk;
    }

    private float DrivingYAt(float seconds, int direction, float delay) =>
        Mathf.Clamp(cart.transform.position.y + direction * SteeringSpeed * WorldPerWebUnit * Mathf.Max(0, seconds - delay),
            -cart.LateralLimit, cart.LateralLimit);

    private NetworkPlayer EvasionDriver(out int direction)
    {
        direction = 0;
        drivingHazards.Clear();
        CollectDrivingHazards?.Invoke(drivingHazards);
        if (drivingHazards.Count == 0) return null;
        float bestRisk = DrivingRisk(0, 0);
        if (bestRisk <= 0) return null;

        NetworkPlayer driver = null;
        float arrival = float.PositiveInfinity;
        foreach (var actor in gameScene.Players)
        {
            if (!actor.Assigned.Value) continue;
            bool seated = HoldsStation(StationAction.Driver, actor);
            if (!actor.Bot.Value)
            {
                if (seated || actor.Timing.IsInActionArea(StationAction.Driver)) return null;
                continue;
            }
            if (actor.Holding.Value || IsCarryingChild(actor) || (fuseLit && gunner == actor)) continue;
            var seat = actor.Timing.AreaFor(StationAction.Driver);
            if (seat == null) continue;
            float travel = seated ? 0 : actor.GetComponent<PlayerMovement>().BotTimeToDeckPoint(seat.position);
            if (travel > arrival || (travel == arrival && !seated)) continue;
            arrival = travel;
            driver = actor;
        }
        if (driver == null) return null;

        for (int side = -1; side <= 1; side += 2)
        {
            float risk = DrivingRisk(side, arrival);
            if (risk >= bestRisk - .0001f) continue;
            direction = side;
            bestRisk = risk;
        }
        return direction == 0 ? null : driver;
    }
}

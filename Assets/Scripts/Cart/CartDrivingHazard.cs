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
    private const int DodgeWhipTicks = 6;
    private int nextDodgeWhip;

    private float DrivingRisk(int hits, int direction, int wounds, float delay)
    {
        float risk = 0;
        foreach (var hazard in drivingHazards)
        {
            if (hazard.Exit < 0 || hazard.Enter > CartDrivingHazard.Lookahead) continue;
            float start = Mathf.Max(0, hazard.Enter);
            float end = Mathf.Clamp(hazard.Exit, start, CartDrivingHazard.Lookahead);
            float y0 = DrivingYAt(start, hits, direction, wounds, delay);
            float y1 = DrivingYAt(end, hits, direction, wounds, delay);
            float nearest = Mathf.Clamp(hazard.CenterY, Mathf.Min(y0,y1), Mathf.Max(y0,y1));
            float clearance = hazard.RadiusY + .08f;
            float worst = Mathf.Max(0, clearance - Mathf.Abs(nearest - hazard.CenterY));
            float atExit = Mathf.Max(0, clearance - Mathf.Abs(y1 - hazard.CenterY));
            risk += (worst + .5f * atExit) / (.25f + start);
        }
        return risk;
    }

    private float DrivingYAt(float seconds, int hits, int direction, int wounds, float delay)
    {
        float target = cart.Lane;
        float y = cart.PredictSteeringY(cart.transform.position.y, target, Mathf.Min(seconds,delay));
        float remaining = seconds - delay;
        for (int i = 0; i < hits && remaining > 0; i++)
        {
            if (wounds + i + 1 == RunawayHits)
            {
                float edge = direction * cart.LateralLimit;
                float runaway = Mathf.Min(remaining, RunawayTicks * Step);
                y = Mathf.MoveTowards(y, edge, RunawaySteeringSpeed * WorldPerWebUnit * runaway);
                return cart.PredictSteeringY(y, edge, remaining - runaway);
            }
            target = Mathf.Clamp(target + direction * CartMotion.WhipSteeringStep, -cart.LateralLimit, cart.LateralLimit);
            float interval = Mathf.Min(remaining, DodgeWhipTicks * Step);
            y = cart.PredictSteeringY(y, target, interval);
            remaining -= interval;
        }
        return cart.PredictSteeringY(y, target, Mathf.Max(0, remaining));
    }

    private NetworkPlayer EvasionDriver(out int direction)
    {
        direction = 0;
        drivingHazards.Clear();
        CollectDrivingHazards?.Invoke(drivingHazards);
        if (drivingHazards.Count == 0 || RunawayDirection != 0) return null;
        float bestRisk = DrivingRisk(0, 0, 0, 0);
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
            if (actor.Holding.Value || (fuseLit && gunner == actor)) continue;
            var seat = actor.Timing.AreaFor(StationAction.Driver);
            if (seat == null) continue;
            float travel = seated ? 0 : actor.GetComponent<PlayerMovement>().BotTimeToDeckPoint(seat.position);
            if (travel > arrival || (travel == arrival && !seated)) continue;
            arrival = travel;
            driver = actor;
        }
        if (driver == null) return null;

        float delay = Mathf.Max(arrival, (nextDodgeWhip - careTicks) * Step);
        float bestEffort = 0;
        for (int side = -1; side <= 1; side += 2)
        {
            bool right = side > 0;
            if ((right ? RightHorse : LeftHorse).Value.Mood != HorseMood.Normal) continue;
            int wounds = (right ? RightWounds : LeftWounds).Value;
            int toEdge = Mathf.CeilToInt((cart.LateralLimit - side * cart.Lane) / CartMotion.WhipSteeringStep);
            int maxHits = Mathf.Min(toEdge, RunawayHits - wounds);
            for (int count = 1; count <= maxHits; count++)
            {
                float risk = DrivingRisk(count, side, wounds, delay);
                // Equal safety: prefer fewer hits, a rested horse, and avoiding a runaway.
                float effort = count + wounds * .05f + (wounds + count == RunawayHits ? 20 : 0);
                if (risk < bestRisk - .0001f || (direction != 0 && Mathf.Abs(risk-bestRisk) < .0001f && effort < bestEffort))
                {
                    direction = side;
                    bestRisk = risk;
                    bestEffort = effort;
                }
            }
        }
        return direction == 0 ? null : driver;
    }
}

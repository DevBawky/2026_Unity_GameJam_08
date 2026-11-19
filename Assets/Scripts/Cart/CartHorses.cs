using System;
using Unity.Netcode;
using UnityEngine;

public enum HorseMood : byte { Normal, Angry, Runaway, Tired }

[Serializable]
public struct HorseRageState : INetworkSerializeByMemcpy, IEquatable<HorseRageState>
{
    public float Value;
    public HorseMood Mood;
    public int Remaining, RestTicks;

    public readonly bool Equals(HorseRageState other) =>
        Value == other.Value && Mood == other.Mood && Remaining == other.Remaining && RestTicks == other.RestTicks;
}

public sealed partial class CartStations
{
    internal const float DriverSteeringSpeed = 140f;
    private const int RageRestTicks = 60, BoostTicks = 150, TiredTicks = 360, WhipCooldownTicks = 3;
    [SerializeField] private HorseWhipZone[] horseWhipZones;
    public readonly NetworkVariable<HorseRageState> HorseRage = new();
    private float botSteering;

    internal HorseMood CurrentHorseMood => HorseRage.Value.Mood;
    internal float HorsePace => CurrentHorseMood switch
    {
        HorseMood.Runaway => 1.60f,
        HorseMood.Tired => .55f,
        _ => 1f + .35f * HorseRage.Value.Value
    };

    internal float DriverSteering
    {
        get
        {
            foreach (var driver in gameScene.Players)
            {
                if (!HoldsStation(StationAction.Driver, driver) || driver.Holding.Value) continue;
                float input = driver.Bot.Value ? botSteering : driver.DriverSteer.Value;
                return float.IsFinite(input) ? Mathf.Clamp(input, -1f, 1f) : 0f;
            }
            return 0f;
        }
    }

    private void AdvanceHorseRage()
    {
        var rage = HorseRage.Value;
        if (rage.Mood != HorseMood.Normal)
        {
            rage.Remaining--;
            if (rage.Remaining == 0)
                rage = rage.Mood == HorseMood.Runaway
                    ? new HorseRageState { Mood = HorseMood.Tired, Remaining = TiredTicks }
                    : default;
        }
        else if (rage.RestTicks > 0) rage.RestTicks--;
        else if (rage.Value > 0f) rage.Value = Mathf.Max(0f, rage.Value - .10f * Step);
        HorseRage.Value = rage;
    }

    internal void WhipHorse(NetworkPlayer actor = null)
    {
        if (!IsServer) return;
        var rage = HorseRage.Value;
        if (rage.Mood != HorseMood.Normal || rage.RestTicks > RageRestTicks - WhipCooldownTicks) return;
        rage.Value = Mathf.Min(1f, rage.Value + 1f / 8f);
        rage.RestTicks = RageRestTicks;
        if (rage.Value >= 1f)
        {
            rage.Mood = HorseMood.Runaway;
            rage.Remaining = BoostTicks;
        }
        HorseRage.Value = rage;
        if (actor != null) actor.AwardScore(CartRules.HorseBoostScore);
    }
}

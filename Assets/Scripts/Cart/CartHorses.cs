using System;
using Unity.Netcode;
using UnityEngine;

public enum HorseMood : byte { Normal, Angry, Runaway, Tired }
[Serializable]
public struct HorseCondition : INetworkSerializeByMemcpy
{
    public HorseMood Mood;
    public int Remaining;
}

// 말 두 마리: 채찍질(가속·조향), 놀람·폭주·지침 상태, 마부 조준점 표시.
public sealed partial class CartStations
{
    internal const int RunawayHits = 8, RunawayTicks = 36;
    internal const float RunawaySteeringSpeed = 176f;
    public NetworkVariable<int> RightWounds = new(), LeftWounds = new();
    [SerializeField] private SpriteRenderer driverCursor;
    [SerializeField] private HorseWhipZone[] horseWhipZones;
    public NetworkVariable<HorseCondition> LeftHorse = new(), RightHorse = new();
    private readonly int[] nextHorseRest = new int[2];
    private HorseWhipZone WhipZone(Vector2 aim) => horseWhipZones[aim.y >= 0f ? 1 : 0];
    public bool AimsAtSteeringZone(Vector2 aim)
    {
        var zone = WhipZone(aim);
        return zone.SteersAt(HorseWhipZone.CursorUV(aim));
    }

    private void ShowDriverAim()
    {
        if (driverCursor == null) return;
        if (IsSpawned && Running && horseWhipZones != null && horseWhipZones.Length == 2)
        {
            foreach (var player in gameScene.Players)
            {
                if (!HoldsStation(StationAction.Driver, player)) continue;
                Vector2 aim = player.DriverAim.Value;
                var zone = WhipZone(aim);
                Vector2 uv = HorseWhipZone.CursorUV(aim);
                driverCursor.transform.position = zone.transform.TransformPoint(zone.LocalPoint(uv));
                driverCursor.color = zone.SteersAt(uv) ? new Color(.62f, 1f, .66f) : new Color(1f, .46f, .42f);
                if (!driverCursor.gameObject.activeSelf) driverCursor.gameObject.SetActive(true);
                return;
            }
        }
        if (driverCursor.gameObject.activeSelf) driverCursor.gameObject.SetActive(false);
    }
    public int RunawayDirection => RightHorse.Value.Mood == HorseMood.Runaway ? 1 :
        LeftHorse.Value.Mood == HorseMood.Runaway ? -1 : 0;
    public float FatigueRate => (LeftHorse.Value.Mood == HorseMood.Tired ? .78f : 1f) *
        (RightHorse.Value.Mood == HorseMood.Tired ? .78f : 1f);

    private void AdvanceHorse(bool right)
    {
        int side = right ? 1 : 0;
        var status = right ? RightHorse : LeftHorse;
        var wounds = right ? RightWounds : LeftWounds;
        var state = status.Value;
        if (state.Mood == HorseMood.Normal)
        {
            if (wounds.Value > 0 && careTicks >= nextHorseRest[side])
            {
                wounds.Value--;
                nextHorseRest[side] = careTicks + 24;
            }
            return;
        }
        state.Remaining--;
        if (state.Mood == HorseMood.Tired) wounds.Value = Mathf.CeilToInt(RunawayHits * state.Remaining / 120f);
        if (state.Remaining <= 0)
        {
            if (state.Mood == HorseMood.Runaway)
            {
                cart.ResetHorsePace(right);
                state = new HorseCondition { Mood = HorseMood.Tired, Remaining = 120 };
            }
            else state = default;
        }
        status.Value = state;
    }

    internal void StrikeHorse(Vector2 aim)
    {
        if (!IsServer || !float.IsFinite(aim.x) || !float.IsFinite(aim.y) ||
            Mathf.Abs(aim.x) > 1f || Mathf.Abs(aim.y) > 1f) return;
        bool right = aim.y >= 0f;
        var state = right ? RightHorse : LeftHorse;
        if (state.Value.Mood != HorseMood.Normal) return;
        if (AimsAtSteeringZone(aim)) WhipRump(right);
        else
        {
            cart.ResetHorsePace(right);
            state.Value = new HorseCondition { Mood = HorseMood.Angry, Remaining = Owns(ShopPerk.Blinders) ? BlindersAngryTicks : 90 };
        }
    }

    private void WhipRump(bool right)
    {
        var wounds = right ? RightWounds : LeftWounds;
        var state = right ? RightHorse : LeftHorse;
        if (state.Value.Mood != HorseMood.Normal || (wounds.Value == RunawayHits - 1 && RunawayDirection != 0)) return;
        nextHorseRest[right ? 1 : 0] = careTicks + (Owns(ShopPerk.OatFeed) ? OatFeedRestTicks : 90);
        wounds.Value++;
        cart.BoostHorse(right, Owns(ShopPerk.ImprovedWhip) ? ImprovedWhipStrength : 1f);
        if (wounds.Value == RunawayHits)
            state.Value = new HorseCondition { Mood = HorseMood.Runaway, Remaining = RunawayTicks };
    }
}

using Unity.Netcode;
using UnityEngine;

// 장애물·몬스터·투석기 피해와 충격 연출(흔들림·먼지·소리).
public sealed partial class CartStations
{
    // 장애물·몬스터 공격이 마차에 닿았을 때.
    public void ReceiveCollision(float damage) => ReceiveImpact(damage, ObstacleShock, .34f);
    public void ReceiveEnemyHit(float damage) => ReceiveImpact(damage, PursuerShock, 0f);
    public void ReceiveCatapultHit(float damage) => ReceiveImpact(damage, CatapultShock, .52f);

    private void ReceiveImpact(float damage, float strength, float fallChance)
    {
        if (!IsServer || !Running) return;
        if (!DevInvincible) durability = Mathf.Max(Endless.Value ? 1f : 0f, durability - Mathf.Max(0f, damage));
        AddImpactEffects(collisionAnxietyIncrease, impactShakeStrength, strength, fallChance);
        if (StageDistance >= wheelLossMinDistance && !wheelDetached && Sample() < wheelLossChance) DetachWheel();
        if (durability <= 0f) FailStage("CART DESTROYED");
    }

    private void AddImpactEffects(float anxiety, float cameraStrength, float workStrength, float fallChance, bool cannon = false)
    {
        float multiplier = Mathf.Pow(CushionImpactFactor, cushionAxleLevel);
        princeAnxiety = Mathf.Clamp01(princeAnxiety + anxiety * multiplier);
        princessAnxiety = Mathf.Clamp01(princessAnxiety + anxiety * multiplier);
        if (fallChance > 0f && randomState != 0)
        {
            KnockDown(Prince, fallChance * multiplier);
            KnockDown(Princess, fallChance * multiplier);
        }

        float strength = Mathf.Min(CannonShock, workStrength * multiplier);
        // A hit and its wheel detachment can happen in the same simulation step.
        double time = NetworkManager.ServerTime.Time;
        if (strength >= .9f)
            royalSeatBounce.Value = new CartWorkShock { Time = time, Strength = strength };
        pendingCookingShock = Mathf.Max(pendingCookingShock, strength);
        uint sequence = ++impactSequence;
        workShock.Value = new CartWorkShock
        {
            Time = time,
            Strength = strength,
            Phase = Mathf.Repeat(sequence * 2.3999632f, 2f * Mathf.PI)
        };
        if (strength > (Owns(ShopPerk.CarrySling) ? CarrySlingThreshold : ShellSlipThreshold)) StartShellCatch(time);
        ImpactRpc(sequence, cannon, cameraStrength * multiplier, shakeDuration);
    }

    internal float WorkShakeAt(double time, out float phase)
    {
        var shock = workShock.Value;
        phase = shock.Phase;
        double elapsed = System.Math.Max(0d, time - shock.Time);
        float steppedElapsed = (float)(System.Math.Floor(elapsed * 30d + 1e-6d) / 30d);
        return Mathf.Max(0f, shock.Strength - steppedElapsed * ShakeDecayPerSecond);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ImpactRpc(uint sequence, bool cannon, float shake, float duration)
    {
        if (sequence == lastPlayedImpact) return;
        lastPlayedImpact = sequence;
        if (!cannon)
        {
            cameraRig.Shake(shake, duration);
            if (secondCamera != null) secondCamera.Shake(shake, duration);
            if (impactDust != null) impactDust.Emit(12);
        }
        var clip = cannon ? cannonSound : hitSound;
        if (impactAudio != null && clip != null) impactAudio.PlayOneShot(clip);
    }

    internal void PlayCannonLanding() => CannonLandingRpc();

    [Rpc(SendTo.ClientsAndHost)]
    private void CannonLandingRpc()
    {
        if (impactAudio != null && cannonLandingSound != null)
            impactAudio.PlayOneShot(cannonLandingSound);
    }
}

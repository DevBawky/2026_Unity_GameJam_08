using Unity.Netcode;
using UnityEngine;

// 장애물·몬스터·투석기 피해와 충격 연출(흔들림·먼지·소리).
public sealed partial class CartStations
{
    // 장애물·몬스터 공격이 마차에 닿았을 때.
    public void ReceiveCollision(float damage) => ReceiveImpact(damage, .34f);
    public void ReceiveEnemyHit(float damage) => ReceiveImpact(damage, 0f);
    public void ReceiveCatapultHit(float damage) => ReceiveImpact(damage, .52f);

    private void ReceiveImpact(float damage, float fallChance)
    {
        if (!IsServer || !Running) return;
        if (!DevInvincible) durability = Mathf.Max(Endless.Value ? 1f : 0f, durability - Mathf.Max(0f, damage));
        if (damage >= shellHoldDamageThreshold) ApplyImpact();
        AddImpactEffects(collisionAnxietyIncrease, impactShakeStrength, false, fallChance);
        if (StageDistance >= wheelLossMinDistance && !wheelDetached && Sample() < wheelLossChance) DetachWheel();
        if (durability <= 0f) FailStage("CART DESTROYED");
    }

    private void AddImpactEffects(float anxiety, float shake, bool cannon = false, float fallChance = .25f)
    {
        float multiplier = Mathf.Pow(CushionImpactFactor, cushionAxleLevel);
        princeAnxiety = Mathf.Clamp01(princeAnxiety + anxiety * multiplier);
        princessAnxiety = Mathf.Clamp01(princessAnxiety + anxiety * multiplier);
        ShockCare(shake * multiplier, fallChance * multiplier);
        LastShockTime.Value = NetworkManager.ServerTime.Time;
        ImpactRpc(++impactSequence, cannon, shake * multiplier, aimJitterDegrees * multiplier, shakeDuration);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ImpactRpc(uint sequence, bool cannon, float shake, float aim, float duration)
    {
        if (sequence == lastPlayedImpact) return;
        lastPlayedImpact = sequence;
        cameraRig.Shake(shake, duration);
        if (secondCamera != null) secondCamera.Shake(shake, duration);
        if (impactDust != null) impactDust.Emit(cannon ? 18 : 12);
        if (cannon && muzzleVisual != null) muzzleFlashStart = Time.time;
        var clip = cannon ? cannonSound : hitSound;
        if (impactAudio != null && clip != null) impactAudio.PlayOneShot(clip);
    }
}

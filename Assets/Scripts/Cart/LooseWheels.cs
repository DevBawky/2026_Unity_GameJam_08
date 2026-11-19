using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct LooseWheelState : INetworkSerializeByMemcpy, IEquatable<LooseWheelState>
{
    public Vector2 Position, Velocity;
    public float Angle;
    public int Mount;
    public bool Equals(LooseWheelState other) => Position.Equals(other.Position) && Velocity.Equals(other.Velocity)
        && Angle.Equals(other.Angle) && Mount == other.Mount;
}

// 바퀴 빠짐·수리와, 빠져서 굴러가는 바퀴(적을 튕겨냄).
[Serializable]
public struct WheelMountVisual : INetworkSerializeByMemcpy, IEquatable<WheelMountVisual>
{
    public int SlotKey;
    public double Time;
    public readonly bool Equals(WheelMountVisual other) => SlotKey == other.SlotKey && Time == other.Time;
}

public sealed partial class CartStations
{
    public readonly NetworkVariable<WheelMountVisual> WheelMount = new();
    public SpriteRenderer TireAt(int index) => index >= 0 && index < tires.Count ? tires[index] : null;
    public Vector3 WheelWorkPoint => detachedTire == null ? cart.transform.position
        : detachedTire.transform.position + Vector3.up * (IsUpperWheel(detachedTire) ? -29f : 31f) * WorldPerWebUnit;
    public bool IsUpperWheel(SpriteRenderer tire) => tire.transform.position.y > cart.transform.position.y;

    public bool IsWheelMounted(int index) =>
        (damagedTires & (1 << index)) == 0 || tires[index] == detachedTire && wheelRepairSteps > 0;

    private void RefreshTireArtwork(int index)
    {
        var tire = tires[index];
        bool mounted = IsWheelMounted(index);
        tire.sprite = mounted || missingWheelSprite == null ? tireSprites[index] : missingWheelSprite;
        Color color = tire.color;
        color.a = mounted || missingWheelSprite != null ? tireAlphas[index] : 0f;
        tire.color = color;
    }

    private void DetachWheel(Vector2? contact = null)
    {
        int index = -1;
        if (contact is Vector2 point)
        {
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < tires.Count; i++)
            {
                if (!IsWheelMounted(i)) continue;
                float distance = ((Vector2)tires[i].transform.position - point).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                index = i;
            }
        }
        else if (!wheelDetached && tires.Count > 0) index = (int)(Sample() * tires.Count);
        if (index < 0) return;

        var tire = tires[index];
        float previousPace = cart.SpeedMultiplier;
        LaunchWheel(tire);
        damagedTires |= 1 << index;
        wheelDetached = true;
        wheelEverDetached = true;
        if (detachedTire == null || detachedTire == tire)
        {
            detachedTire = tire;
            wheelRepairSteps = 0;
            WheelMount.Value = default;
        }
        RefreshTireArtwork(index);
        cart.SpeedMultiplier = WheelLossSpeed;
        if (IsAxleBroken && previousPace > WheelLossSpeed) cart.ApplySpeedHit(WheelLossSpeed / previousPace);
        AddImpactEffects(0f, impactShakeStrength, WheelShock, 0f, wheel: true);
        Publish();
    }

    private bool IsAxleBroken
    {
        get
        {
            int front = 0, rear = 0;
            float center = cartBody.bounds.center.x;
            for (int i = 0; i < tires.Count; i++)
            {
                if ((damagedTires & (1 << i)) == 0) continue;
                if (tires[i].transform.position.x < center) front++;
                else rear++;
            }
            return front >= 2 || rear >= 2;
        }
    }

    // 보조 지지대가 있으면 바퀴가 빠져도 속도가 덜 떨어진다.
    private float WheelLossSpeed => IsAxleBroken ? .18f
        : Owns(ShopPerk.SupportStrut) ? Mathf.Max(wheelLossSpeedMultiplier, SupportStrutSpeed) : wheelLossSpeedMultiplier;

    private void RepairWheel()
    {
        int index = tires.IndexOf(detachedTire);
        if (index < 0) return;
        damagedTires &= ~(1 << index);
        RefreshTireArtwork(index);
        wheelRepairSteps = 0;
        detachedTire = null;
        for (int i = 0; i < tires.Count; i++)
        {
            if ((damagedTires & (1 << i)) == 0) continue;
            detachedTire = tires[i];
            break;
        }
        wheelDetached = detachedTire != null;
        cart.SpeedMultiplier = wheelDetached ? WheelLossSpeed : 1f;
        Publish();
    }

    private void RepairAllWheels()
    {
        while (wheelDetached && detachedTire != null) RepairWheel();
    }

    public NetworkList<LooseWheelState> LooseWheels = new();
    private readonly List<SpriteRenderer> wheelVisuals = new();

    private void LaunchWheel(SpriteRenderer tire)
    {
        Vector2 pos = tire.transform.position + Vector3.up * (IsUpperWheel(tire) ? 2f : 0f) * WorldPerWebUnit;
        bool rear = pos.x >= cartBody.bounds.center.x;
        float side = pos.y >= cartBody.bounds.center.y ? 1 : -1;
        LooseWheels.Add(new LooseWheelState {
            Position=pos, Mount=tires.IndexOf(tire),
            Velocity=new Vector2((rear ? 126 : 134)+cart.Speed*2,side*(rear ? -46 : 84))*WorldPerWebUnit
        });
    }

    private void ShowWheels()
    {
        while (wheelVisuals.Count < LooseWheels.Count)
        {
            var view=Instantiate(looseWheelPrefab);
            view.transform.localScale=Vector3.one*(WorldPerWebUnit*150f);
            wheelVisuals.Add(view);
        }
        for (int i=wheelVisuals.Count-1;i>=0;i--)
        {
            if (i>=LooseWheels.Count) {Destroy(wheelVisuals[i].gameObject);wheelVisuals.RemoveAt(i);continue;}
            var w=LooseWheels[i];
            wheelVisuals[i].transform.SetPositionAndRotation(w.Position,Quaternion.Euler(0,0,w.Angle));
        }
    }
    private void ClearWheelVisuals()
    {
        foreach(var wheel in wheelVisuals) if(wheel!=null)Destroy(wheel.gameObject);
        wheelVisuals.Clear();
    }

    // Solve in ellipse space so a fast wheel or a moving target cannot skip a contact.
    internal static float EllipseEntry(Vector2 start,Vector2 step,Vector2 radii)
    {
        var p=start/radii;var v=step/radii;
        float c=p.sqrMagnitude-1;
        if(c<=0)return 0;
        float a=v.sqrMagnitude,b=Vector2.Dot(p,v),disc=b*b-a*c;
        if(a==0 || b>=0 || disc<0)return float.PositiveInfinity;
        return (-b-Mathf.Sqrt(disc))/a;
    }
    private void Roll(ref LooseWheelState w,float from,float to)
    {
        var step=w.Velocity*(Step*(to-from));
        for(int i=Enemy.Active.Count-1;i>=0;i--)
        {
            var enemy=Enemy.Active[i];
            if(enemy.Knocked)continue;
            Vector2 pos=enemy.transform.position;
            var delta=pos-enemy.PreviousPosition;
            var start=enemy.PreviousPosition+delta*from;
            var body=enemy.GetComponentInChildren<SpriteRenderer>();
            if(body==null)continue;
            var radii=(Vector2)body.bounds.extents+Vector2.one*(20*WorldPerWebUnit);
            start+=(Vector2)body.bounds.center-pos;
            float at=EllipseEntry(w.Position-start,step-delta*(to-from),radii);
            if(at>1)continue;
            float side=start.y+delta.y*(to-from)*at>=w.Position.y+step.y*at ? 1:-1;
            enemy.KnockAway(new Vector2(Mathf.Max(102,w.Velocity.x/WorldPerWebUnit*.82f+48),
                Mathf.Clamp(w.Velocity.y/WorldPerWebUnit*.46f+side*110,-170,170))*WorldPerWebUnit);
        }
        w.Position+=step;
    }
    private void AdvanceWheels()
    {
        if(LooseWheels.Count==0)return;
        float scale=WorldPerWebUnit;
        Vector2 ground=cart.ScrollVelocity;
        for(int i=LooseWheels.Count-1;i>=0;i--)
        {
            var w=LooseWheels[i];
            Vector2 relative=(w.Velocity-ground)*Step;
            float fraction=1; Vector2 normal=Vector2.zero;
            void Surface(Vector2 center,Vector2 half)
            {
                var radius=half+Vector2.one*(20*scale);
                var start=w.Position-center+ground*Step;
                float at=EllipseEntry(start,relative,radius);
                if(at>=fraction)return;
                var n=((start+relative*at)/(radius*radius)).normalized;
                if(Vector2.Dot(relative,n)>=0)return;
                fraction=at;normal=n;
            }
            foreach(var obj in Obstacle.WheelSurfaces)
                Surface(obj.transform.position,obj.Size*.5f);
            Roll(ref w,0,fraction);
            if(fraction<1)
            {
                w.Velocity-=normal*(1.72f*Vector2.Dot(w.Velocity-ground,normal));
                w.Position+=normal*(.65f*scale);
                Roll(ref w,fraction,1);
            }
            w.Angle-=Mathf.Sign(w.Velocity.x-ground.x)*(w.Velocity-ground).magnitude*Step/(20*scale)*Mathf.Rad2Deg;
            bool offRoad=Mathf.Abs(w.Position.y)>cart.LateralLimit+134*scale;
            w.Velocity.x=Mathf.Lerp(w.Velocity.x,ground.x+60*scale,offRoad ? .044f:.025f);
            w.Velocity.y*=offRoad ? .88f:.994f;
            if(w.Position.x>cartBody.bounds.center.x+852*scale)LooseWheels.RemoveAt(i);
            else LooseWheels[i]=w;
        }
    }
}

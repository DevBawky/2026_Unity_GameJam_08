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

    private void SetTireAlpha(float alpha)
    {
        if (detachedTire == null) return;
        Color color = detachedTire.color;
        if (missingWheelSprite != null)
        {
            bool missing = alpha <= 0f;
            detachedTire.sprite = missing ? missingWheelSprite : tireSprites[tires.IndexOf(detachedTire)];
            alpha = missing ? tireAlphas[tires.IndexOf(detachedTire)] : alpha;
        }
        color.a = alpha;
        detachedTire.color = color;
    }

    private void DetachWheel()
    {
        if (wheelDetached || tires.Count == 0) return;
        detachedTire = tires[(int)(Sample()*tires.Count)];
        detachedTireAlpha = detachedTire.color.a;
        LaunchWheel(detachedTire);
        wheelDetached = true;
        wheelEverDetached = true;
        wheelRepairSteps = 0;
        SetTireAlpha(0f);
        cart.SpeedMultiplier = WheelLossSpeed;
        AddImpactEffects(0f, impactShakeStrength, WheelShock, 0f);
        Publish();
    }

    // 보조 지지대가 있으면 바퀴가 빠져도 속도가 덜 떨어진다.
    private float WheelLossSpeed => Owns(ShopPerk.SupportStrut) ? Mathf.Max(wheelLossSpeedMultiplier, SupportStrutSpeed) : wheelLossSpeedMultiplier;

    private void RepairWheel()
    {
        SetTireAlpha(detachedTireAlpha);
        detachedTire = null;
        wheelDetached = false;
        cart.SpeedMultiplier = 1f;
        Publish();
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
            view.transform.localScale=Vector3.one*(WorldPerWebUnit*100f);
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

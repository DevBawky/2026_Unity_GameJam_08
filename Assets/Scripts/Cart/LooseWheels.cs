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

public sealed partial class CartStations
{
    public NetworkList<LooseWheelState> LooseWheels = new();
    private readonly List<SpriteRenderer> wheelVisuals = new();

    private void LaunchWheel(SpriteRenderer tire)
    {
        Vector2 pos = tire.transform.position;
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
            var src=tires[LooseWheels[wheelVisuals.Count].Mount];
            var copy=Instantiate(src,src.transform.position,Quaternion.identity);
            copy.transform.localScale=src.transform.lossyScale;
            copy.color=Color.white;
            wheelVisuals.Add(copy);
        }
        for (int i=wheelVisuals.Count-1;i>=0;i--)
        {
            if (i>=LooseWheels.Count) {Destroy(wheelVisuals[i].gameObject);wheelVisuals.RemoveAt(i);continue;}
            var w=LooseWheels[i];var view=wheelVisuals[i];
            view.sprite=tires[w.Mount].sprite;
            view.transform.SetPositionAndRotation(w.Position,Quaternion.Euler(0,0,w.Angle));
        }
    }
    private void ClearWheelVisuals()
    {
        foreach(var wheel in wheelVisuals) if(wheel!=null)Destroy(wheel.gameObject);
        wheelVisuals.Clear();
    }

    // Solve in ellipse space so a fast wheel or a moving target cannot skip a contact.
    private static float Entry(Vector2 start,Vector2 step,Vector2 radii)
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
            float at=Entry(w.Position-start,step-delta*(to-from),radii);
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
                float at=Entry(start,relative,radius);
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

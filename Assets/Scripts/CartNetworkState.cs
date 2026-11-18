using System;
using Unity.Netcode;

[Serializable]
public struct CartNetworkState : INetworkSerializeByMemcpy, IEquatable<CartNetworkState>
{
    public float Durability, Prince, Princess, StageStart;
    public int Stage, Stored, Pile, Lost, RepairSteps, Tire, Cushion, Horse, Precision;
    public StageFlow Flow;
    public bool Loaded, Firing, Wheel, EverWheel;
    public bool Equals(CartNetworkState v) => Durability==v.Durability && Prince==v.Prince && Princess==v.Princess && StageStart==v.StageStart && Stage==v.Stage && Stored==v.Stored && Pile==v.Pile && Lost==v.Lost && RepairSteps==v.RepairSteps && Tire==v.Tire && Cushion==v.Cushion && Horse==v.Horse && Precision==v.Precision && Flow==v.Flow && Loaded==v.Loaded && Firing==v.Firing && Wheel==v.Wheel && EverWheel==v.EverWheel;
}
[Serializable]
public struct CartMotionState : INetworkSerializeByMemcpy, IEquatable<CartMotionState>
{
    public float Speed, Target, Distance, Left, Right, Multiplier, Scroll, ObstacleSlow, SlowRemaining;
    public int LeftStep, RightStep;
    public float Lane;
    public bool Equals(CartMotionState v) => Speed==v.Speed && Target==v.Target && Distance==v.Distance && LeftStep==v.LeftStep && RightStep==v.RightStep && Lane==v.Lane && Left==v.Left && Right==v.Right && Multiplier==v.Multiplier && Scroll==v.Scroll && ObstacleSlow==v.ObstacleSlow && SlowRemaining==v.SlowRemaining;
}

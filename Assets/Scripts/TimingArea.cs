using UnityEngine;

public enum StationAction { Driver = 0, RoyalFamily = 2, ShellPile, ShellDeposit, Reload, Cannon, WheelRepair, WheelRack, Kitchen, Princess, Stairs, CannonCleaning }

// 상호작용 영역 설정. 포탄 보관·대포는 타이밍 없이 바로 동작하고, 나머지는 원형 타이밍 미니게임을 쓴다.
public sealed class TimingArea : MonoBehaviour
{
    [SerializeField] private StationAction action;
    [SerializeField, Range(5f, 180f)] private float circularTargetAngle = 60f;
    [SerializeField, Min(1f)] private float circularSpeed = 180f;

    [SerializeField] private StationTimingProfile profile;

    public static bool IsRoyal(StationAction action) => action is StationAction.RoyalFamily or StationAction.Princess;
    public static bool IsCannon(StationAction action) => action is StationAction.Cannon or StationAction.CannonCleaning or StationAction.Reload;
    public static bool UsesTimingFor(StationAction action) => action is StationAction.ShellPile or StationAction.Reload or StationAction.WheelRepair;
    public StationAction Action => action;
    public float CircularTargetAngle => profile != null ? profile.targetAngle : circularTargetAngle;
    public float CircularSpeed => profile != null ? profile.degreesPerSecond : circularSpeed;
}

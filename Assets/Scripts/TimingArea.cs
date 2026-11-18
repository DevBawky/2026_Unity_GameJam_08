using UnityEngine;

public enum TimingPattern { Bar, Circular }
public enum StationAction { RightHorse, LeftHorse, RoyalFamily, ShellPile, ShellDeposit, Reload, Cannon }

public sealed class TimingArea : MonoBehaviour
{
    [SerializeField] private TimingPattern pattern = TimingPattern.Bar;
    [SerializeField] private StationAction action;
    [SerializeField, Range(5f, 180f)] private float circularTargetAngle = 60f;
    [SerializeField, Min(1f)] private float circularSpeed = 180f;
    public TimingPattern Pattern => pattern;
    public StationAction Action => action;
    public bool UsesTiming => action != StationAction.ShellDeposit && action != StationAction.Cannon;
    public float CircularTargetAngle => circularTargetAngle;
    public float CircularSpeed => circularSpeed;
}

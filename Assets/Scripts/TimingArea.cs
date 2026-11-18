using UnityEngine;

public enum StationAction { RightHorse, LeftHorse, RoyalFamily, ShellPile, ShellDeposit, Reload, Cannon, WheelRepair }

// 상호작용 영역 설정. 포탄 보관·대포는 타이밍 없이 바로 동작하고, 나머지는 원형 타이밍 미니게임을 쓴다.
public sealed class TimingArea : MonoBehaviour
{
    [SerializeField] private StationAction action;
    [SerializeField, Range(5f, 180f)] private float circularTargetAngle = 60f;
    [SerializeField, Min(1f)] private float circularSpeed = 180f;

    public StationAction Action => action;
    public bool UsesTiming => action != StationAction.ShellDeposit && action != StationAction.Cannon;
    public float CircularTargetAngle => circularTargetAngle;
    public float CircularSpeed => circularSpeed;
}

using System;
using UnityEngine;

public enum StationAction { Driver = 0, RoyalFamily = 2, ShellPile, ShellDeposit, Reload, Cannon, WheelRepair, WheelRack, Kitchen, Princess, Stairs, CannonCleaning }

// 상호작용 영역 설정. 포탄 보관·대포는 타이밍 없이 바로 동작하고, 나머지는 원형 타이밍 미니게임을 쓴다.
public sealed class TimingArea : MonoBehaviour
{
    [SerializeField] private StationAction action;
    [SerializeField] private RectTransform pad;
    [SerializeField, Range(5f, 180f)] private float circularTargetAngle = 60f;
    [SerializeField, Min(1f)] private float circularSpeed = 180f;

    [SerializeField] private StationTimingProfile profile;

    public static bool IsRoyal(StationAction action) => action is StationAction.RoyalFamily or StationAction.Princess;
    public static bool IsCannon(StationAction action) => action is StationAction.Cannon or StationAction.CannonCleaning or StationAction.Reload;
    public static bool UsesTimingFor(StationAction action) => action is StationAction.ShellPile or StationAction.Reload or StationAction.WheelRepair;
    public StationAction Action => action;
    public float CircularTargetAngle => profile != null ? profile.targetAngle : circularTargetAngle;
    public float CircularSpeed => profile != null ? profile.degreesPerSecond : circularSpeed;

    internal bool OverlapsPlayer(Bounds body, float reach = 1f) =>
        pad != null && OverlapsPlayer(body, pad.rect, pad.worldToLocalMatrix, reach);

    internal static bool OverlapsPlayer(Bounds body, Rect rect, Matrix4x4 worldToPad, float reach = 1f)
    {
        Vector2 radius = rect.size * (.5f * reach);
        if (radius.x <= 0f || radius.y <= 0f || body.size.x <= 0f || body.size.y <= 0f) return false;

        Vector2 center = (Vector2)worldToPad.MultiplyPoint3x4(body.center) - rect.center;
        Vector3 horizontal = worldToPad.MultiplyVector(Vector3.right * body.extents.x);
        Vector3 vertical = worldToPad.MultiplyVector(Vector3.up * body.extents.y);
        double halfWidth = (Math.Abs(horizontal.x) + Math.Abs(vertical.x)) / radius.x;
        double halfHeight = (Math.Abs(horizontal.y) + Math.Abs(vertical.y)) / radius.y;

        // Scale both shapes so the pad is a unit circle. Their overlap ratio stays the same.
        double x = center.x / (double)radius.x;
        double y = center.y / (double)radius.y;
        double left = x - halfWidth, right = x + halfWidth;
        double bottom = y - halfHeight, top = y + halfHeight;
        double required = 4d * halfWidth * halfHeight * (CartRules.Active.requiredPlayerOverlapPercent * .01d);
        double width = Math.Min(right, 1d) - Math.Max(left, -1d);
        double height = Math.Min(top, 1d) - Math.Max(bottom, -1d);
        if (width <= 0d || height <= 0d || width * height < required) return false;

        double overlap = CircleAreaTo(right, top) - CircleAreaTo(left, top)
            - CircleAreaTo(right, bottom) + CircleAreaTo(left, bottom);
        return overlap >= required;
    }

    // Signed area inside the circle between the origin and this corner.
    private static double CircleAreaTo(double x, double y)
    {
        double end = Math.Min(Math.Abs(x), 1d);
        double height = Math.Min(Math.Abs(y), 1d);
        double flatEnd = Math.Min(end, Math.Sqrt(1d - height * height));
        double area = flatEnd * height;
        if (end > flatEnd) area += CircleIntegral(end) - CircleIntegral(flatEnd);
        return (x < 0d) != (y < 0d) ? -area : area;
    }

    private static double CircleIntegral(double x) =>
        .5d * (x * Math.Sqrt(1d - x * x) + Math.Asin(x));
}

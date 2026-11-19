using UnityEngine;

[CreateAssetMenu(menuName = "Game/Station Timing")]
public sealed class StationTimingProfile : ScriptableObject
{
    [Range(5f, 180f)] public float targetAngle = 60f;
    [Min(1f)] public float degreesPerSecond = 180f;
}

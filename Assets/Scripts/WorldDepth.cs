using UnityEngine;

internal static class WorldDepth
{
    internal static int Order(Vector3 ground, bool rider)
    {
        float y = ground.y > NetworkPlayer.CellarOffset * .5f ? ground.y - NetworkPlayer.CellarOffset : ground.y;
        int row = Mathf.Clamp(Mathf.RoundToInt(-y * 24f), -350, 350);
        int tie = rider ? 63 : 62 - Mathf.Clamp(Mathf.RoundToInt(ground.x * 2f) + 16, 0, 62);
        return 4096 + row * 64 + tie;
    }
}

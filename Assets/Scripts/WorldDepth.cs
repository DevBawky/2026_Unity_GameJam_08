using UnityEngine;

// 화면 y(아래일수록 앞)로 스프라이트 정렬 순서를 정한다. 지하층은 1층과 같은 기준으로 맞춘다.
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

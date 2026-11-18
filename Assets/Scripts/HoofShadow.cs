using UnityEngine;

// 말 스프라이트 프레임의 발굽 위치에 맞춰 그림자 위치·크기를 잡는다.
internal static class HoofShadow
{
    internal static void Place(SpriteRenderer shadow, SpriteRenderer horse, Vector3 hoof,
        float frameWidth, float frameHeight, float webScale, float depth)
    {
        Bounds art = horse.bounds;
        float x = art.min.x + (hoof.x + hoof.y) * .5f / frameWidth * art.size.x;
        float y = art.max.y - (hoof.z - 1) / frameHeight * art.size.y;
        float radius = (hoof.y - hoof.x) * .5f / frameWidth * art.size.x + 4f * webScale;
        shadow.transform.position = new Vector3(x, y, horse.transform.position.z);
        var parent = shadow.transform.parent.lossyScale;
        var sprite = shadow.sprite.bounds.size;
        shadow.transform.localScale = new Vector3(2f * radius / (sprite.x * Mathf.Abs(parent.x)),
            2f * depth * webScale / (sprite.y * Mathf.Abs(parent.y)), 1f);
    }
}

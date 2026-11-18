using UnityEngine;
using UnityEngine.UI;

// 원형 타이밍 UI. Container가 SetAngles를 받아 목표 호(Target)와 핀(Pin) 위치를 갱신한다.
// 목표 호 메시는 각도·폭이 바뀔 때만 다시 만든다(핀만 움직이는 프레임엔 재생성 안 함).
public sealed class CircularTimingGraphic : MaskableGraphic
{
    private const float TrackRadiusRatio = 0.38f;
    private const float PinRadiusRatio = 0.915f;
    private const float ArcInnerRatio = 0.83f;
    private const float DegreesPerSegment = 4f;

    private enum Element { Container, Track, Target, Pin }

    [SerializeField] private Element element;
    [SerializeField] private CircularTimingGraphic targetGraphic;
    [SerializeField] private RectTransform pinTransform;

    private float targetCenter = float.NaN;
    private float targetAngle = float.NaN;

    private float Radius => Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * TrackRadiusRatio;

    public void SetAngles(float center, float width, float pin)
    {
        if (targetGraphic != null) targetGraphic.SetAngles(center, width, pin);
        if (pinTransform != null)
        {
            float radians = pin * Mathf.Deg2Rad;
            pinTransform.anchoredPosition = rectTransform.rect.center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (Radius * PinRadiusRatio);
        }
        if (center == targetCenter && width == targetAngle) return;
        targetCenter = center;
        targetAngle = width;
        if (element == Element.Target) SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (element != Element.Target || float.IsNaN(targetAngle)) return;
        float radius = Radius;
        Vector2 center = rectTransform.rect.center;
        int segments = Mathf.Max(1, Mathf.CeilToInt(targetAngle / DegreesPerSegment));
        for (int i = 0; i <= segments; i++)
        {
            float angle = (targetCenter - targetAngle * 0.5f + targetAngle * i / segments) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(center + direction * radius, color, Vector2.zero);
            vh.AddVert(center + direction * radius * ArcInnerRatio, color, Vector2.zero);
            if (i == 0) continue;
            int index = i * 2;
            vh.AddTriangle(index - 2, index, index - 1);
            vh.AddTriangle(index, index + 1, index - 1);
        }
    }
}

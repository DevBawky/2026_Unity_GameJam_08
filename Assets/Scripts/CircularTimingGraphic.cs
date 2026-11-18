using UnityEngine;
using UnityEngine.UI;

public sealed class CircularTimingGraphic : MaskableGraphic
{
    private enum Element { Container, Track, Target, Pin }
    [SerializeField] private Element element;
    [SerializeField] private CircularTimingGraphic targetGraphic;
    [SerializeField] private RectTransform pinTransform;
    private float targetCenter;
    private float targetAngle;

    public void SetAngles(float center, float width, float pin)
    {
        if (targetGraphic != null) targetGraphic.SetAngles(center, width, pin);
        if (pinTransform != null)
        {
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.38f * 0.915f;
            float radians = pin * Mathf.Deg2Rad;
            pinTransform.anchoredPosition = rectTransform.rect.center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }
        targetCenter = center;
        targetAngle = width;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (element != Element.Target) return;
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.38f;
        Vector2 center = rectTransform.rect.center;
        int segments = Mathf.Max(1, Mathf.CeilToInt(targetAngle / 4f));
        for (int i = 0; i <= segments; i++)
        {
            float angle = (targetCenter - targetAngle * 0.5f + targetAngle * i / segments) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(center + direction * radius, color, Vector2.zero);
            vh.AddVert(center + direction * radius * 0.83f, color, Vector2.zero);
            if (i == 0) continue;
            int index = i * 2;
            vh.AddTriangle(index - 2, index, index - 1);
            vh.AddTriangle(index, index + 1, index - 1);
        }
    }
}

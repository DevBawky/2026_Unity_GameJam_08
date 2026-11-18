using UnityEngine;
public sealed class ProgressTriangleGraphic : UnityEngine.UI.MaskableGraphic
{
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vertices)
    {
        vertices.Clear();
        Rect rect = rectTransform.rect;
        vertices.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
        vertices.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
        vertices.AddVert(new Vector3(rect.center.x, rect.yMin), color, Vector2.zero);
        vertices.AddTriangle(0, 1, 2);
    }
}

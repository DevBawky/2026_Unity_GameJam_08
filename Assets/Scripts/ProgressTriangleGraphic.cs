using UnityEngine;
using UnityEngine.UI;

// 진행 거리 표시용 아래를 가리키는 삼각형.
public sealed class ProgressTriangleGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        Rect rect = rectTransform.rect;
        vertices.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
        vertices.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
        vertices.AddVert(new Vector3(rect.center.x, rect.yMin), color, Vector2.zero);
        vertices.AddTriangle(0, 1, 2);
    }
}

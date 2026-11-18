using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(HorseWhipZone))]
public sealed class HorseWhipZoneEditor : Editor
{
    private readonly BoxBoundsHandle box = new BoxBoundsHandle
    {
        axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y
    };
    private static readonly Color ZoneColor = new Color(.3f, 1f, .4f);

    private void OnSceneGUI()
    {
        var zone = (HorseWhipZone)target;
        Rect area = zone.SteeringArea;
        var bounds = zone.SpriteBounds;
        if (bounds.size.x <= 0f || bounds.size.y <= 0f) return;
        using (new Handles.DrawingScope(ZoneColor, zone.transform.localToWorldMatrix))
        {
            Vector3 min = zone.LocalPoint(area.min);
            Vector3 max = zone.LocalPoint(area.max);
            Vector3 center = (min + max) * .5f;
            Handles.DrawSolidRectangleWithOutline(new[] { min, new Vector3(min.x,max.y,min.z), max,
                new Vector3(max.x,min.y,min.z) }, new Color(.1f,.9f,.25f,.15f), ZoneColor);
            box.center = center;
            box.size = max - min;
            EditorGUI.BeginChangeCheck();
            box.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 lo = box.center - box.size * .5f;
                Vector3 hi = box.center + box.size * .5f;
                Save(Rect.MinMaxRect(Mathf.InverseLerp(bounds.min.x,bounds.max.x,lo.x),
                    Mathf.InverseLerp(bounds.min.y,bounds.max.y,lo.y), Mathf.InverseLerp(bounds.min.x,bounds.max.x,hi.x),
                    Mathf.InverseLerp(bounds.min.y,bounds.max.y,hi.y)));
            }

            EditorGUI.BeginChangeCheck();
            var next = Handles.Slider2D(center, Vector3.forward, Vector3.right, Vector3.up,
                Mathf.Min(max.x-min.x,max.y-min.y)*.15f, Handles.RectangleHandleCap, Vector2.zero);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = next - center;
                area.position += new Vector2(delta.x / bounds.size.x, delta.y / bounds.size.y);
                Save(area);
            }
        }
    }

    private void Save(Rect area)
    {
        serializedObject.Update();
        serializedObject.FindProperty("steeringArea").rectValue = area;
        serializedObject.ApplyModifiedProperties();
    }
}

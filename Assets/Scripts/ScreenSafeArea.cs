using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class ScreenSafeArea : MonoBehaviour
{
    [SerializeField] private RectTransform[] content;
    private Vector2[] originalMin, originalMax;
    private Rect previous;
    private void Awake()
    {
        originalMin = new Vector2[content.Length];
        originalMax = new Vector2[content.Length];
        for (int i = 0; i < content.Length; i++)
        {
            originalMin[i] = content[i].anchorMin;
            originalMax[i] = content[i].anchorMax;
        }
    }

    private void LateUpdate()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Rect area = Screen.safeArea;
        Vector2 size = new(Screen.width, Screen.height);
        Vector2 min = area.min / size;
        Vector2 max = area.max / size;
        Fit(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
    }

    private void Fit(Rect normalized)
    {
        Vector2 min = normalized.min;
        if (normalized == previous) return;
        previous = normalized;
        for (int i = 0; i < content.Length; i++)
        {
            content[i].anchorMin = min + originalMin[i] * normalized.size;
            content[i].anchorMax = min + originalMax[i] * normalized.size;
        }
    }
}

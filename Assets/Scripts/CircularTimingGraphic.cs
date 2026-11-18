using UnityEngine;

// Controls preconfigured Image components; no graphics or assets are created at runtime.
public sealed class CircularTimingGraphic : MonoBehaviour
{
    [SerializeField] private CircularTimingGraphic targetGraphic;
    [SerializeField] private RectTransform pinTransform;
    [SerializeField] private UnityEngine.UI.Image targetImage;
    public RectTransform rectTransform => (RectTransform)transform;

    public void SetAngles(float center, float width, float pin)
    {
        if (targetGraphic != null) targetGraphic.SetAngles(center, width, pin);
        if (targetImage != null)
        {
            targetImage.fillAmount = Mathf.Clamp01(width / 360f);
            targetImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, center - width * 0.5f);
        }
        if (pinTransform != null)
        {
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.38f * 0.915f;
            float radians = pin * Mathf.Deg2Rad;
            pinTransform.anchoredPosition = rectTransform.rect.center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }
    }
}

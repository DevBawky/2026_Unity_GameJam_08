using UnityEngine;
using UnityEngine.UI;

// 원형 타이밍 UI. 에디터에 배치한 Image의 채움 양·회전과 핀 위치만 바꾼다(런타임 생성 없음).
// 컨테이너는 targetGraphic(목표 호)과 pinTransform(핀)을 함께 갱신한다.
public sealed class CircularTimingGraphic : MonoBehaviour
{
    private const float TrackRadiusRatio = 0.38f;
    private const float PinRadiusRatio = 0.915f;

    [SerializeField] private CircularTimingGraphic targetGraphic;
    [SerializeField] private RectTransform pinTransform;
    [SerializeField] private Image targetImage;

    // PlayerTiming이 Graphic 시절 이름 그대로 부른다.
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
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * TrackRadiusRatio * PinRadiusRatio;
            float radians = pin * Mathf.Deg2Rad;
            pinTransform.anchoredPosition = rectTransform.rect.center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }
    }
}

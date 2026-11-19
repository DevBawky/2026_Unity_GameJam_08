using UnityEngine;

// 반복 배경 타일용. WorldScroller 자식으로 두면, 월드 x가 wrapX를 넘을 때 loopLength만큼 왼쪽으로 되돌린다.
public class ScrollWrap : MonoBehaviour
{
    [SerializeField] private float loopLength = 24f; // 타일 폭 × 타일 개수
    [SerializeField] private float wrapX = 12f;

    private void LateUpdate()
    {
        float excess = transform.position.x - wrapX;
        if (excess > 0f && loopLength > 0f)
            transform.position -= Vector3.right * (Mathf.Ceil(excess / loopLength) * loopLength);
    }
}

using UnityEngine;

// 반복 배경 타일용. WorldScroller 자식으로 두면, 화면 오른쪽(wrapX)을 넘을 때 loopLength만큼 왼쪽으로 되돌린다.
public class ScrollWrap : MonoBehaviour
{
    [SerializeField] float loopLength = 24f; // 타일 폭 × 타일 개수
    [SerializeField] float wrapX = 12f;      // 월드 x 기준

    void LateUpdate()
    {
        if (transform.position.x > wrapX)
            transform.position -= Vector3.right * loopLength;
    }
}

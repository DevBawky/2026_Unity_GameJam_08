using UnityEngine;

// 씬이 시작되면 지정한 배경음을 SoundManager로 튼다. 곡이 비어 있으면 아무것도 하지 않는다.
public sealed class SceneBgm : MonoBehaviour
{
    [Tooltip("이 씬에서 반복 재생할 배경음 (비워두면 재생하지 않음)")]
    [SerializeField] private AudioClip bgm;

    private void Start()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayBgm(bgm);
    }
}

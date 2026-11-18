using UnityEngine;

// 씬이 시작되면 지정한 배경음을 SoundManager로 튼다. 곡이 비어 있으면 앞 씬의 배경음을 끈다.
// (mainGame처럼 씬이 자체 음악을 관리하면 비워둔다.)
public sealed class SceneBgm : MonoBehaviour
{
    [Tooltip("이 씬에서 반복 재생할 배경음 (비워두면 이전 씬 배경음을 끔)")]
    [SerializeField] private AudioClip bgm;

    private void Start()
    {
        SoundManager sound = SoundManager.Instance;
        if (sound == null) return;
        if (bgm == null) sound.StopBgm();
        else sound.PlayBgm(bgm);
    }
}

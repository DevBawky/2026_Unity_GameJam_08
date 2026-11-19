using UnityEngine;
using UnityEngine.Audio;

internal enum GameSfx
{
    UiTick, UiConfirm, UiReject, TeamBell, DangerPulse, PursuitHorn, StoneFlight,
    ImpactDirt, ImpactCart, AxleSnap, WheelFit, ShellMetal, CannonLoad, FuseBurn,
    CannonFire, BrushScrub, WhipSnap, RoyalWhimper, CookSizzle, StageResult, DefeatSting
}

// 배경음·효과음을 재생하고 믹서 볼륨을 저장·적용한다. 씬이 바뀌어도 하나만 남는다.
public sealed class SoundManager : MonoBehaviour
{
    public const string MasterVolume = "MasterVolume";
    public const string BgmVolume = "BgmVolume";
    public const string SfxVolume = "SfxVolume";
    private const float DefaultVolume = 0.8f;
    private const float MuteDecibels = -80f;
    private const float DecibelsPerDecade = 20f;

    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource cookingSource;
    [SerializeField] private AudioClip[] gameSfx;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 믹서 값은 Awake에서 넣으면 무시되므로 Start에서 적용한다.
    private void Start()
    {
        ApplyVolume(MasterVolume, GetVolume(MasterVolume));
        ApplyVolume(BgmVolume, GetVolume(BgmVolume));
        ApplyVolume(SfxVolume, GetVolume(SfxVolume));
    }

    public float GetVolume(string parameter) => PlayerPrefs.GetFloat(parameter, DefaultVolume);

    public void SetVolume(string parameter, float volume)
    {
        PlayerPrefs.SetFloat(parameter, volume);
        ApplyVolume(parameter, volume);
    }

    public void PlayBgm(AudioClip clip)
    {
        if (clip == null || (bgmSource.clip == clip && bgmSource.isPlaying)) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBgm() => bgmSource.Stop();

    public void PlaySfx(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
    }

    internal static bool PlayCue(GameSfx cue)
    {
        var manager = Instance;
        int index = (int)cue;
        if (manager == null || manager.gameSfx == null || index >= manager.gameSfx.Length) return false;
        var clip = manager.gameSfx[index];
        if (clip == null) return false;
        manager.PlaySfx(clip);
        return true;
    }

    internal void SetCooking(bool active)
    {
        if (cookingSource == null || active == cookingSource.isPlaying) return;
        if (active) cookingSource.Play();
        else cookingSource.Stop();
    }

    private void ApplyVolume(string parameter, float volume)
    {
        mixer.SetFloat(parameter, volume > 0.0001f ? Mathf.Log10(volume) * DecibelsPerDecade : MuteDecibels);
    }
}

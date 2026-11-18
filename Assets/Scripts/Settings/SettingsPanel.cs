using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 설정 창: 볼륨 3종(SoundManager), 화면 모드(Screen), 언어(LocalizationTable)를 바꾼다.
public sealed class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider masterSlider, bgmSlider, sfxSlider;
    [SerializeField] private TMP_Text screenModeLabel, languageLabel;

    private void Awake()
    {
        masterSlider.onValueChanged.AddListener(v => SetVolume(SoundManager.MasterVolume, v));
        bgmSlider.onValueChanged.AddListener(v => SetVolume(SoundManager.BgmVolume, v));
        sfxSlider.onValueChanged.AddListener(v => SetVolume(SoundManager.SfxVolume, v));
    }

    private void OnEnable()
    {
        SoundManager sound = SoundManager.Instance;
        if (sound != null)
        {
            masterSlider.SetValueWithoutNotify(sound.GetVolume(SoundManager.MasterVolume));
            bgmSlider.SetValueWithoutNotify(sound.GetVolume(SoundManager.BgmVolume));
            sfxSlider.SetValueWithoutNotify(sound.GetVolume(SoundManager.SfxVolume));
        }
        LocalizationTable.LanguageChanged += RefreshLabels;
        RefreshLabels();
    }

    private void OnDisable()
    {
        LocalizationTable.LanguageChanged -= RefreshLabels;
        PlayerPrefs.Save();
    }

    public void ToggleScreenMode()
    {
        bool windowed = Screen.fullScreenMode == FullScreenMode.Windowed;
        Screen.fullScreenMode = windowed ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        RefreshLabels();
    }

    public void ToggleLanguage()
    {
        bool korean = LocalizationTable.Current == LocalizationTable.Language.Korean;
        LocalizationTable.Current = korean ? LocalizationTable.Language.English : LocalizationTable.Language.Korean;
    }

    private void SetVolume(string parameter, float volume)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetVolume(parameter, volume);
    }

    private void RefreshLabels()
    {
        bool windowed = Screen.fullScreenMode == FullScreenMode.Windowed;
        screenModeLabel.text = LocalizationTable.T(windowed ? "창 모드" : "전체 화면");
        languageLabel.text = LocalizationTable.Current == LocalizationTable.Language.Korean ? "한국어" : "English";
    }
}

using System;
using UnityEngine;
using System.Collections.Generic;

// 한국어 원문을 키로 영어 번역을 찾는 표. 표에 없는 문구는 한국어 그대로 보여준다.
[CreateAssetMenu(menuName = "GameJam/Localization Table")]
public sealed class LocalizationTable : ScriptableObject
{
    public enum Language { Korean, English }

    [Serializable]
    private struct Entry
    {
        [TextArea] public string korean;
        [TextArea] public string english;
    }

    private const string LanguageKey = "Language";

    public static event Action LanguageChanged;

    public static Language Current
    {
        get => (Language)PlayerPrefs.GetInt(LanguageKey, (int)Language.Korean);
        set
        {
            if (value == Current) return;
            PlayerPrefs.SetInt(LanguageKey, (int)value);
            LanguageChanged?.Invoke();
        }
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<string, string> english;

    public string Translate(string korean)
    {
        if (Current == Language.Korean || string.IsNullOrEmpty(korean)) return korean;
        if (english == null)
        {
            english = new Dictionary<string, string>();
            foreach (Entry entry in entries) english[entry.korean] = entry.english;
        }
        return english.TryGetValue(korean, out string text) ? text : korean;
    }
}

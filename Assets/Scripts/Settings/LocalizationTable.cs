using System;
using UnityEngine;
using System.Collections.Generic;

// 한국어 원문을 키로 영어 번역을 찾는 표. 표에 없는 문구는 한국어 그대로 보여준다.
// "연결 실패: "처럼 ": "로 끝나는 키는 앞부분만 일치해도 번역하고, 뒷부분도 표에서 다시 찾는다.
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
    private const string PrefixSuffix = ": ";

    public static event Action LanguageChanged;

    private static Language? current;
    private static LocalizationTable loaded;
    private static readonly Dictionary<string, (int a, int b, int c, Language language, string text)> formatted = new();

    // 매 프레임 불려서 PlayerPrefs를 매번 읽지 않고 한 번만 읽어 둔다.
    public static Language Current
    {
        get => current ??= (Language)PlayerPrefs.GetInt(LanguageKey, (int)Language.Korean);
        set
        {
            if (value == Current) return;
            current = value;
            PlayerPrefs.SetInt(LanguageKey, (int)value);
            LanguageChanged?.Invoke();
        }
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<string, string> english;
    private readonly List<KeyValuePair<string, string>> prefixes = new();
    private string lastMissKey, lastMissText;

    // 도메인 리로드를 꺼 둬서 플레이마다 static 값을 비운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current = null;
        formatted.Clear();
    }

    // 씬의 LocalizedText가 이 표를 참조하면 로드되면서 등록된다. 코드에서 만든 문구는 T/F로 번역한다.
    private void OnEnable() => loaded = this;

    public static string T(string korean) => loaded != null ? loaded.Translate(korean) : korean;

    // 숫자가 들어가는 문구: 표의 키는 "{0}" 자리표시를 포함한 한국어 원문이다.
    public static string F(string koreanFormat, params object[] args) => string.Format(T(koreanFormat), args);

    // 매 프레임 쓰는 정수 문구용. 값이 그대로면 이전 문자열을 돌려줘 할당하지 않는다.
    public static string F(string koreanFormat, int a, int b = 0, int c = 0)
    {
        Language language = Current;
        if (formatted.TryGetValue(koreanFormat, out var hit) && hit.a == a && hit.b == b && hit.c == c && hit.language == language)
            return hit.text;
        string text = string.Format(T(koreanFormat), a, b, c);
        formatted[koreanFormat] = (a, b, c, language, text);
        return text;
    }

    public string Translate(string korean)
    {
        if (Current == Language.Korean || string.IsNullOrEmpty(korean)) return korean;
        if (english == null)
        {
            english = new Dictionary<string, string>();
            foreach (Entry entry in entries)
            {
                english[entry.korean] = entry.english;
                if (entry.korean.EndsWith(PrefixSuffix)) prefixes.Add(new KeyValuePair<string, string>(entry.korean, entry.english));
            }
        }
        if (english.TryGetValue(korean, out string text)) return text;
        if (ReferenceEquals(korean, lastMissKey)) return lastMissText;
        lastMissKey = korean;
        lastMissText = korean;
        foreach (var prefix in prefixes)
        {
            if (!korean.StartsWith(prefix.Key, StringComparison.Ordinal)) continue;
            string rest = korean.Substring(prefix.Key.Length);
            lastMissText = prefix.Value + (english.TryGetValue(rest, out string restText) ? restText : rest);
            break;
        }
        return lastMissText;
    }
}

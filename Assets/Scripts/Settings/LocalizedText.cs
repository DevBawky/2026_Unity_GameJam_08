using TMPro;
using UnityEngine;

// 에디터에 적힌 한국어 문구를 키로 삼아, 언어가 바뀌면 번역표의 문구로 바꿔 보여준다.
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedText : MonoBehaviour
{
    [SerializeField] private LocalizationTable table;

    private TMP_Text label;
    private string korean;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        korean = label.text;
    }

    private void OnEnable()
    {
        LocalizationTable.LanguageChanged += Apply;
        Apply();
    }

    private void OnDisable() => LocalizationTable.LanguageChanged -= Apply;

    private void Apply() => label.text = table.Translate(korean);
}

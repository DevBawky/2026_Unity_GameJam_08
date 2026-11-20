using System.Collections;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-40)]
public sealed class GameplayStory : MonoBehaviour
{
    [SerializeField] private CartStations stations;
    [SerializeField] private GameObject introPanel;
    [SerializeField] private TMP_Text dialogue, waiting, nextLabel;
    [SerializeField] private UnityEngine.UI.Button next;
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private RectTransform wipe, wipeFill, coverEdge, revealEdge;
    [SerializeField] private RectTransform introPortrait, introDialogueBox;
    [SerializeField] private float fadeSeconds = .4f;
    [SerializeField] private float blackoutSeconds = .4f;
    private const float EdgeWidth = 60f;

    private int presentedLine = -1;
    private bool portraitLayout;
    private UnityEngine.UI.Image fadeImage;
    private StageFlow shownPhase = StageFlow.Waiting;
    private StageFlow observedPhase = StageFlow.Waiting;
    private Coroutine transition;

    internal bool ResultsVisible => shownPhase == observedPhase;

    private void Awake()
    {
        next.onClick.AddListener(AdvanceIntro);
        fadeImage = fade.GetComponent<UnityEngine.UI.Image>();
        fade.alpha = 1f;
        fade.gameObject.SetActive(true);
    }

    private void Update()
    {
        var phase = stations.IsSpawned ? stations.State.Value.Flow : StageFlow.Waiting;
        if (phase != observedPhase)
        {
            var previous = observedPhase;
            observedPhase = phase;
            if (previous != StageFlow.OperationIntro || phase != StageFlow.Running)
            {
                if (transition != null) StopCoroutine(transition);
                transition = StartCoroutine(TransitionTo(phase));
            }
            else shownPhase = phase;
        }

        bool introducing = shownPhase == StageFlow.OperationIntro && stations.InOperationIntro;
        if (introPanel.activeSelf != introducing) introPanel.SetActive(introducing);
        if (introducing)
        {
            UpdateIntroLayout();
            int line = stations.IntroLine;
            if (line != presentedLine)
            {
                presentedLine = line;
                dialogue.text = CartStations.IntroDialogue[line];
                nextLabel.text = line == CartStations.IntroDialogue.Length - 1 ? "작전 시작" : "다음 대사";
            }
            dialogue.maxVisibleCharacters = stations.IntroVisibleCharacters;
            next.gameObject.SetActive(stations.IsHost && stations.IntroLineReady);
            waiting.gameObject.SetActive(!stations.IsHost && stations.IntroLineReady);
        }
        else presentedLine = -1;
    }

    private IEnumerator TransitionTo(StageFlow phase)
    {
        fade.gameObject.SetActive(true);
        if (wipe.gameObject.activeSelf)
        {
            HideWipe();
            fade.alpha = 1f;
        }
        bool tiled = phase != StageFlow.Complete && !SettingsPanel.ReducedMotion && fadeSeconds > 0f;
        if (tiled && fade.alpha < 1f) yield return WipeScreen(false);
        else yield return FadeTo(1f);
        if (!SettingsPanel.ReducedMotion) yield return new WaitForSecondsRealtime(blackoutSeconds);
        shownPhase = phase;
        clearPanel.SetActive(phase == StageFlow.Complete);
        if (!clearPanel.activeSelf)
        {
            if (tiled) yield return WipeScreen(true);
            else yield return FadeTo(0f);
        }
        HideWipe();
        fade.gameObject.SetActive(false);
        transition = null;
    }

    private IEnumerator WipeScreen(bool reveal)
    {
        double start = Time.realtimeSinceStartupAsDouble;
        fade.alpha = 1f;
        fadeImage.color = Color.clear;
        wipe.gameObject.SetActive(true);
        coverEdge.gameObject.SetActive(!reveal);
        revealEdge.gameObject.SetActive(reveal);
        var edge = reveal ? revealEdge : coverEdge;
        float progress;
        do
        {
            progress = Mathf.Clamp01((float)((Time.realtimeSinceStartupAsDouble - start) / fadeSeconds));
            float width = wipe.rect.width;
            float front = Mathf.Lerp(-EdgeWidth, width, progress);
            edge.offsetMin = new Vector2(front, 0f);
            edge.offsetMax = new Vector2(front + EdgeWidth, 0f);
            wipeFill.offsetMin = new Vector2(reveal ? Mathf.Clamp(front + EdgeWidth, 0f, width) : 0f, 0f);
            wipeFill.offsetMax = new Vector2(reveal ? width : Mathf.Clamp(front, 0f, width), 0f);
            yield return null;
        } while (progress < 1f);
        if (reveal) fade.alpha = 0f;
    }

    private void HideWipe()
    {
        wipe.gameObject.SetActive(false);
        fadeImage.color = Color.black;
    }

    private IEnumerator FadeTo(float alpha)
    {
        if (SettingsPanel.ReducedMotion || fadeSeconds <= 0f)
        {
            fade.alpha = alpha;
            yield break;
        }
        while (fade.alpha != alpha)
        {
            fade.alpha = Mathf.MoveTowards(fade.alpha, alpha, Time.unscaledDeltaTime / fadeSeconds);
            yield return null;
        }
    }

    private void AdvanceIntro() => stations.AdvanceIntro(presentedLine);

    private void UpdateIntroLayout()
    {
        bool tall = Screen.height > Screen.width;
        if (tall == portraitLayout) return;
        portraitLayout = tall;
        introPortrait.anchorMin = tall ? new Vector2(.15f, .33f) : new Vector2(.035f, .07f);
        introPortrait.anchorMax = tall ? new Vector2(.85f, .67f) : new Vector2(.425f, .60f);
        introDialogueBox.anchorMin = tall ? new Vector2(.035f, .055f) : new Vector2(.44f, .07f);
        introDialogueBox.anchorMax = tall ? new Vector2(.965f, .33f) : new Vector2(.965f, .54f);
    }
}

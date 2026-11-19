using Unity.Netcode;
using UnityEngine;

public sealed partial class CartStations
{
    internal static readonly string[] IntroDialogue =
    {
        "이 자식들! 실적이 안나온다고 이런 무모한 짓을 벌이면 어떻게 하냔 말이야!!",
        "하... 이렇게 된 이상 확실하게 왕자와 공주를 데리고 안전지대까지 빠져나와.",
        "너희들이 붙잡혀도 나는 모르는 일이다. 알겠어?!",
        "그리고 애들을 울리기라도 하면 니네 부서는 진짜 끝장날 줄 알아!!"
    };

    private const float IntroCharactersPerSecond = 27f;
    private const float MusicFadeOut = .12f;
    private const float MusicFadeIn = .11f;
    [SerializeField] private bool operationIntroEnabled;
    [SerializeField] private GameplayStory story;
    private readonly NetworkVariable<int> introLine = new();
    private readonly NetworkVariable<double> introLineStartedAt = new();
    private StageFlow? playingMusicFor;

    internal bool InOperationIntro => flow == StageFlow.OperationIntro;
    internal bool ResultsVisible => story == null || story.ResultsVisible;
    internal int IntroLine => introLine.Value;
    internal double IntroElapsed => System.Math.Max(0, NetworkManager.ServerTime.Time - introLineStartedAt.Value);
    internal int IntroVisibleCharacters => Mathf.Min(IntroDialogue[IntroLine].Length,
        1 + Mathf.FloorToInt((float)(IntroElapsed + 1e-6) * IntroCharactersPerSecond));
    internal bool IntroLineReady => IntroVisibleCharacters >= IntroDialogue[IntroLine].Length;

    private void SetIntroLine(int line, double elapsed = 0d)
    {
        introLine.Value = Mathf.Clamp(line, 0, IntroDialogue.Length - 1);
        introLineStartedAt.Value = NetworkManager.ServerTime.Time - elapsed;
        Publish();
    }

    internal void AdvanceIntro(int expectedLine)
    {
        if (!IsHost || !InOperationIntro || expectedLine != IntroLine || !IntroLineReady) return;
        if (IntroLine + 1 < IntroDialogue.Length) SetIntroLine(IntroLine + 1);
        else StartStage();
    }

    private void UpdateCampaignMusic()
    {
        StageFlow phase = IsSpawned ? flow : StageFlow.Waiting;
        var cue = phase switch
        {
            StageFlow.OperationIntro or StageFlow.Running => (chaseMusic, true, .72f),
            StageFlow.ChoosingUpgrade or StageFlow.Complete => (clearMusic, false, .65f),
            StageFlow.Failed => (defeatMusic, false, .61f),
            _ => (waitingMusic, true, .52f)
        };
        (AudioClip clip, bool loop, float gain) = cue;

        if (playingMusicFor != phase)
        {
            if (music.clip != clip || playingMusicFor == null)
            {
                if (music.isPlaying && music.volume > 0f)
                {
                    music.volume = Mathf.MoveTowards(music.volume, 0f, Time.unscaledDeltaTime / MusicFadeOut);
                    if (music.volume > 0f) return;
                }
                music.Stop();
                music.clip = clip;
                music.loop = loop;
                music.volume = 0f;
                if (clip != null) music.Play();
            }
            playingMusicFor = phase;
        }
        music.volume = Mathf.MoveTowards(music.volume, gain, gain * Time.unscaledDeltaTime / MusicFadeIn);
    }
}

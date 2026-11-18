using UnityEngine;

public sealed class SpriteSequence : ScriptableObject
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float[] frameStarts;
    [SerializeField] private float duration;

    public bool PlayingAt(float seconds) => seconds >= 0f && seconds < duration;

    public Sprite FrameAt(float seconds, bool loop = false)
    {
        if (loop) seconds = Mathf.Repeat(seconds, duration);
        for (int i = frames.Length - 1; i > 0; i--)
            if (seconds >= frameStarts[i]) return frames[i];
        return frames[0];
    }
}

using UnityEngine;

public sealed class CrewSpriteSet : ScriptableObject
{
    public enum Pose { Idle, Shell, Snack, SpareWheel, Prince, Princess }

    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] equipment;

    public Sprite Get(int slot, Pose pose) => sprites[Mathf.Clamp(slot, 0, 3) * 6 + (int)pose];
    public bool HasWalkFrames => walkFrames != null && walkFrames.Length == 16;
    public Sprite Walk(int slot, int tick, bool moving) =>
        walkFrames[slot * 4 + ((tick + slot * 3) / (moving ? 4 : 13)) % 4];
    public Sprite Equipment(Pose pose) => pose != Pose.Idle ? equipment[(int)pose - 1] : null;
}

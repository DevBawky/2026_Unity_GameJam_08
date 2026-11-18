using UnityEngine;

public sealed class CrewSpriteSet : ScriptableObject
{
    public enum Pose { Idle, Shell, Snack, SpareWheel, Prince, Princess }

    [SerializeField] private Sprite[] sprites;

    public Sprite Get(int slot, Pose pose) => sprites[Mathf.Clamp(slot, 0, 3) * 6 + (int)pose];
}

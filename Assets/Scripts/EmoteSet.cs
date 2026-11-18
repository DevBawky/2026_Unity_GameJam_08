using System;
using UnityEngine;

// 이모티콘 목록. 웹판 emotes.mjs·CrewControls.Messages와 같은 순서(NetworkPlayer.Emote 1~7 → 0~6)다.
[CreateAssetMenu(menuName = "Game/Emote Set")]
public sealed class EmoteSet : ScriptableObject
{
    [Serializable]
    public struct Emote
    {
        public Sprite icon;
        [Tooltip("모바일 목록에 보이는 이름")]
        public string label;
        [Tooltip("팀 알림 문구. 앞에 \"P1 플레이어가 \"가 붙는다")]
        public string message;
    }

    [SerializeField] private Emote[] emotes;

    public int Count => emotes.Length;
    public Emote this[int index] => emotes[index];
}

using System;
using UnityEngine;

// 이모티콘 목록. 웹판 emotes.mjs와 같은 순서·문구이며 숫자키 1~7이 이 순서를 따른다.
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

    // 숫자키 1~7. 네트워크로 받은 번호도 이 범위로 검사한다.
    public const int MaxCount = 7;

    [SerializeField] private Emote[] emotes = new Emote[MaxCount];

    public int Count => emotes.Length;
    public Emote this[int index] => emotes[index];
}

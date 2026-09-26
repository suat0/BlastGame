using System;
using UnityEngine;

namespace BlastGame.Game
{
    // Every sound the game makes, by what happened rather than by file. Code asks for SfxId.BoxBreak;
    // which clips that is, how loud, how varied and how often it may play is data here.
    public enum SfxId
    {
        Blast, BlastBig, Land, Reject, BoxHit, BoxBreak, Collect, Combo, LowMoves, Shuffle,
        LevelStart, Win, Lose, Click, PopupOpen, Locked, CoinLand,
    }

    [CreateAssetMenu(fileName = "SoundBank", menuName = "Blast/Sound Bank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public SfxId id;

            [Tooltip("One is picked at random each time, so a sound heard often does not repeat exactly.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();

            [Range(0f, 1f)] public float volume = 1f;

            [Tooltip("Random pitch spread either side of the requested pitch.")]
            [Range(0f, 0.3f)] public float pitchVariance = 0.05f;

            [Tooltip("At most this many at once; a request beyond it is dropped. Forty blocks landing " +
                     "together are one thud, not forty.")]
            [Min(1)] public int maxVoices = 2;

            [Tooltip("Seconds before the same sound may start again.")]
            [Min(0f)] public float cooldown = 0.03f;

            [Tooltip("Music dips while this plays, so a jingle is heard over it.")]
            public bool ducksMusic;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [SerializeField] private AudioClip homeMusic;
        [SerializeField] private AudioClip levelMusic;

        [NonSerialized] private Entry[] byId;

        public AudioClip HomeMusic => homeMusic;
        public AudioClip LevelMusic => levelMusic;

        // Indexed on first use, so a lookup during play is an array read.
        public Entry Get(SfxId id)
        {
            if (byId == null)
            {
                byId = new Entry[Enum.GetValues(typeof(SfxId)).Length];
                foreach (Entry entry in entries) byId[(int)entry.id] = entry;
            }

            return byId[(int)id];
        }
    }
}

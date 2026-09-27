using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlastGame.Game
{
    // Plays the game's sounds and its music. Lives on the App object, so the music carries across a
    // scene change instead of restarting.
    //
    // A fixed set of AudioSources, reused: nothing is created or allocated when a sound plays. One
    // event is one sound - a blast of twelve blocks plays one blast - and the bank's voice limits and
    // cooldowns keep a cascade of landings from turning into noise.
    //
    // Two volumes stand in for a mixer: sound on or off, and music on or off with a dip while a jingle
    // plays. A mixer adds nothing to those two numbers and cannot be created from code.
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] private SoundBank bank;
        [SerializeField] private int voices = 10;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField] private float musicFade = 0.8f;

        [Tooltip("Music level, as a fraction, while a jingle plays over it.")]
        [SerializeField, Range(0f, 1f)] private float duckLevel = 0.25f;

        private AudioSource[] sources;
        private SfxId[] sourceSound;
        private float[] sourceStarted;
        private float[] lastPlayed;

        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource currentMusic;
        private float duckUntil;

        private UnityAction click;

        private void Awake()
        {
            sources = new AudioSource[voices];
            sourceSound = new SfxId[voices];
            sourceStarted = new float[voices];
            for (int i = 0; i < voices; i++) sources[i] = CreateSource(false);

            lastPlayed = new float[SoundBank.IdCount];
            for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = float.NegativeInfinity;

            musicA = CreateSource(true);
            musicB = CreateSource(true);
            currentMusic = musicA;

            click = PlayClick;
        }

        private AudioSource CreateSource(bool music)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = music;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;

        private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

        // Every button in a freshly loaded scene clicks, inactive popups' buttons included. Hooked here
        // once per scene load, so no screen has to remember to wire its buttons to a sound.
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                button.onClick.AddListener(click);

            if (bank == null) return;
            PlayMusic(scene.name == Scenes.Level ? bank.LevelMusic : bank.HomeMusic);
        }

        private void PlayClick() => Play(SfxId.Click);

        public void Play(SfxId id, float pitch = 1f)
        {
            if (bank == null || !PlayerProgress.SfxOn) return;

            SoundBank.Entry entry = bank.Get(id);
            if (entry == null || entry.clips.Length == 0) return;

            float now = Time.unscaledTime;
            if (now - lastPlayed[(int)id] < entry.cooldown) return;

            int active = 0, free = -1, oldest = 0;
            for (int i = 0; i < sources.Length; i++)
            {
                if (!sources[i].isPlaying)
                {
                    if (free < 0) free = i;
                    continue;
                }

                if (sourceSound[i] == id) active++;
                if (sourceStarted[i] < sourceStarted[oldest]) oldest = i;
            }

            if (active >= entry.maxVoices) return;

            // Every source busy: the oldest sound is the one least missed.
            int slot = free >= 0 ? free : oldest;
            AudioSource source = sources[slot];

            source.clip = entry.clips[Random.Range(0, entry.clips.Length)];
            source.volume = entry.volume;
            source.pitch = pitch * (1f + Random.Range(-entry.pitchVariance, entry.pitchVariance));
            source.Play();

            sourceSound[slot] = id;
            sourceStarted[slot] = now;
            lastPlayed[(int)id] = now;

            if (entry.ducksMusic) duckUntil = now + source.clip.length / Mathf.Abs(source.pitch);
        }

        // Crossfades to a clip, or keeps playing it if it is already the one on.
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || currentMusic.clip == clip) return;

            currentMusic = currentMusic == musicA ? musicB : musicA;
            currentMusic.clip = clip;
            currentMusic.volume = 0f;
            currentMusic.Play();
        }

        // Unscaled: the pause menu freezes time, not the music.
        private void Update()
        {
            float now = Time.unscaledTime;
            float step = musicVolume / musicFade * Time.unscaledDeltaTime;

            float target = PlayerProgress.MusicOn ? musicVolume * (now < duckUntil ? duckLevel : 1f) : 0f;
            currentMusic.volume = Mathf.MoveTowards(currentMusic.volume, target, step);

            AudioSource other = currentMusic == musicA ? musicB : musicA;
            if (!other.isPlaying) return;

            other.volume = Mathf.MoveTowards(other.volume, 0f, step);
            if (other.volume <= 0f) other.Stop();
        }
    }

    // The one line every caller writes. Quiet when there is no App - an edit-mode tool, a test.
    public static class Sfx
    {
        public static void Play(SfxId id, float pitch = 1f)
        {
            App app = App.Instance;
            if (app != null && app.Audio != null) app.Audio.Play(id, pitch);
        }
    }
}

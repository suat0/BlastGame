using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlastGame.Game
{
    // The one object that lives across scenes: the scene transition and the audio.
    //
    // Created before the first scene loads rather than placed in one, so every scene can be opened and
    // played on its own in the editor and still find it. A copy placed in a scene would need a
    // destroy-the-duplicate check and would exist only if play started from that scene.
    public sealed class App : MonoBehaviour
    {
        private const string PrefabPath = "App";

        [SerializeField] private SceneTransition transition;

        [Tooltip("Mobile runs at 30 by default. Everything on the board is motion, and half of it " +
                 "would be lost at 30.")]
        [SerializeField] private int targetFrameRate = 60;

        [Tooltip("Tweens allocated up front. PrimeTween grows its pool when it runs out, and growing " +
                 "allocates, so this is set above the most that are ever alive at once.")]
        [SerializeField] private int tweenCapacity = 256;

        public static App Instance { get; private set; }

        public SceneTransition Transition => transition;

        public AudioService Audio { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            var prefab = Resources.Load<App>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"No App prefab at Resources/{PrefabPath}; scenes will load without a fade or sound.");
                return;
            }

            Instance = Instantiate(prefab);
            Instance.name = prefab.name;
            DontDestroyOnLoad(Instance.gameObject);
        }

        // The one line every caller writes, like Sfx.Play. Without the App the scene still loads, just
        // without the fade - Create has already logged why.
        public static void LoadScene(string sceneName)
        {
            if (Instance != null && Instance.transition != null) Instance.transition.LoadScene(sceneName);
            else SceneManager.LoadScene(sceneName);
        }

        private void Awake()
        {
            Audio = GetComponent<AudioService>();

            Application.targetFrameRate = targetFrameRate;
            PrimeTweenConfig.SetTweensCapacity(tweenCapacity);
        }

        // The save is already written on every change that matters; this catches the app being
        // backgrounded mid-way through anything that is not, before the OS decides to kill it.
        private void OnApplicationPause(bool paused)
        {
            if (paused) PlayerProgress.Save();
        }
    }
}

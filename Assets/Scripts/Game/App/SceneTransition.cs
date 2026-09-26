using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlastGame.Game
{
    public static class Scenes
    {
        public const string Home = "Home";
        public const string Level = "Level";
    }

    // Covers the screen, swaps the scene underneath, uncovers it. The cover hides the load hitch and
    // the frame in which the new scene has not drawn yet.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SceneTransition : MonoBehaviour
    {
        [SerializeField] private CanvasGroup cover;
        [SerializeField] private float fadeDuration = 0.25f;

        private string pendingScene;

        public bool IsBusy => pendingScene != null;

        private void Awake()
        {
            cover.alpha = 0f;
            cover.blocksRaycasts = false;
        }

        public void LoadScene(string sceneName)
        {
            // A second request while one is running is a double tap, not a change of mind.
            if (IsBusy) return;

            pendingScene = sceneName;

            // Taps are swallowed from the first frame of the fade, or a tap on the way out would land
            // on a button of a scene that is about to be gone.
            cover.blocksRaycasts = true;

            // The previous transition's fade-in may still be running on the same alpha.
            Tween.StopAll(cover);

            // Unscaled: a level left from the pause menu is running at timeScale 0.
            Tween.Alpha(cover, 1f, fadeDuration, Ease.OutQuad, useUnscaledTime: true)
                 .OnComplete(this, self => self.SwapScene());
        }

        private void SwapScene()
        {
            // Pause is a property of the scene being left. Carried into the next one, it would freeze
            // everything there that does not run on unscaled time.
            Time.timeScale = 1f;

            AsyncOperation load = SceneManager.LoadSceneAsync(pendingScene, LoadSceneMode.Single);
            load.completed += HandleSceneLoaded;
        }

        private void HandleSceneLoaded(AsyncOperation _)
        {
            pendingScene = null;

            Tween.Alpha(cover, 0f, fadeDuration, Ease.InQuad, useUnscaledTime: true)
                 .OnComplete(this, self => self.cover.blocksRaycasts = false);
        }

#if UNITY_EDITOR
        private void Reset() => cover = GetComponent<CanvasGroup>();
#endif
    }
}

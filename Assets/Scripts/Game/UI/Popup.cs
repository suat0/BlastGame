using PrimeTween;
using UnityEngine;

namespace BlastGame.Game.UI
{
    // A dimmed backdrop and a card that pops in over it. Every popup in the game is one of these with
    // its own labels and buttons on the card; which one is open is decided by the level's states, not
    // by the popups themselves.
    //
    // Unscaled time throughout: the pause popup opens with the game at timeScale 0.
    public class Popup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup dim;
        [SerializeField] private RectTransform card;

        [SerializeField] private float showDuration = 0.34f;
        [SerializeField] private float hideDuration = 0.18f;

        public bool IsOpen { get; private set; }

        // Leaving the scene while a popup animates would otherwise leave the fade's callback aimed at a
        // destroyed object.
        protected virtual void OnDestroy()
        {
            Tween.StopAll(dim);
            Tween.StopAll(card);
        }

        public void Show()
        {
            Tween.StopAll(dim);
            Tween.StopAll(card);

            IsOpen = true;
            gameObject.SetActive(true);

            // Taps behind an open popup are swallowed by the dim; buttons on a popup still animating in
            // already work, because waiting for the entrance before accepting a tap feels unresponsive.
            dim.blocksRaycasts = true;

            dim.alpha = 0f;
            card.localScale = Vector3.zero;

            Tween.Alpha(dim, 1f, showDuration * 0.6f, Ease.OutQuad, useUnscaledTime: true);
            Tween.Scale(card, 1f, showDuration, Ease.OutBack, useUnscaledTime: true);
        }

        public void Hide()
        {
            if (!IsOpen) return;

            // Closed on the frame it opened - a quick double tap. The card has not grown yet, so there
            // is nothing to shrink, and a tween from zero to zero is just noise.
            if (card.localScale.x <= 0.001f)
            {
                HideImmediately();
                return;
            }

            Tween.StopAll(dim);
            Tween.StopAll(card);

            IsOpen = false;

            // Released at once, not when the fade ends: the state that closed this one may already
            // be opening the next popup or handing input back to the board.
            dim.blocksRaycasts = false;

            Tween.Scale(card, 0f, hideDuration, Ease.InBack, useUnscaledTime: true);
            Tween.Alpha(dim, 0f, hideDuration, Ease.InQuad, useUnscaledTime: true)
                 .OnComplete(this, self => self.gameObject.SetActive(false));
        }

        // For a scene that starts with this closed. Hide animates, and there is nothing to animate
        // away from on the first frame.
        public void HideImmediately()
        {
            Tween.StopAll(dim);
            Tween.StopAll(card);

            IsOpen = false;
            dim.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}

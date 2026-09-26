using PrimeTween;
using TMPro;
using UnityEngine;

namespace BlastGame.Game.UI
{
    // A strip across the middle of the screen at the start of a level, restating the goal the start
    // popup showed. It asks nothing of the player and takes no taps.
    public sealed class IntroBanner : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform strip;
        [SerializeField] private TMP_Text label;

        [SerializeField] private float slideDuration = 0.3f;

        private Sequence sweep;

        private void Awake() => gameObject.SetActive(false);

        // A scene unloaded mid-sweep would otherwise leave the sequence's last callback aimed at a
        // destroyed object.
        private void OnDestroy() => sweep.Stop();

        // Slides in from the right, holds, slides out to the left.
        public void Play(int boxes, float holdDuration)
        {
            if (boxes > 0) label.SetText("Break {0:0} boxes!", boxes);
            else label.SetText("Blast away!");

            sweep.Stop();

            gameObject.SetActive(true);

            float width = ((RectTransform)transform).rect.width;

            sweep = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.UIAnchoredPositionX(strip, width, 0f, slideDuration, Ease.OutCubic))
                .Group(Tween.Alpha(group, 0f, 1f, slideDuration))
                .ChainDelay(holdDuration)
                .Chain(Tween.UIAnchoredPositionX(strip, -width, slideDuration, Ease.InCubic))
                .Group(Tween.Alpha(group, 0f, slideDuration))
                .ChainCallback(this, self => self.gameObject.SetActive(false));
        }
    }
}

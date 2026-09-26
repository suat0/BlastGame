using PrimeTween;
using TMPro;
using UnityEngine;

namespace BlastGame.Game.UI
{
    // One short line that rises, holds and fades: "Team unlocks at Level 20". A single instance,
    // reused; a new message replaces the one on screen rather than stacking under it.
    public sealed class Toast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform body;
        [SerializeField] private TMP_Text label;

        [SerializeField] private float holdDuration = 1.4f;
        [SerializeField] private float rise = 60f;

        private Sequence showing;
        private Vector2 restPosition;

        private void Awake()
        {
            restPosition = body.anchoredPosition;
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        private void OnDestroy() => showing.Stop();

        public void Show(string message)
        {
            label.text = message;

            showing.Stop();

            showing = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.UIAnchoredPosition(body, restPosition - new Vector2(0f, rise), restPosition, 0.25f, Ease.OutBack))
                .Group(Tween.Alpha(group, 0f, 1f, 0.2f))
                .ChainDelay(holdDuration)
                .Chain(Tween.Alpha(group, 0f, 0.3f));
        }
    }
}

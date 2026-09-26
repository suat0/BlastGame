using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.UI
{
    // "Level Completed" with the score and the coins earned. The coin count climbs from zero; the
    // coins themselves are already saved by the time this opens.
    public sealed class WinPopup : Popup
    {
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text coinsLabel;
        [SerializeField] private Button continueButton;

        [SerializeField] private float coinCountDuration = 0.8f;

        public Button ContinueButton => continueButton;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Tween.StopAll(coinsLabel);
        }

        public void Show(int score, int coins)
        {
            scoreLabel.SetText("Score {0:0}", score);
            coinsLabel.SetText("+{0:0}", 0f);

            Show();

            Tween.StopAll(coinsLabel);

            // The target overload: the callback captures nothing, so the tween allocates nothing.
            Tween.Custom(coinsLabel, 0f, coins, coinCountDuration,
                         (label, value) => label.SetText("+{0:0}", Mathf.Ceil(value)),
                         Ease.OutQuad, startDelay: 0.3f, useUnscaledTime: true);
        }
    }
}

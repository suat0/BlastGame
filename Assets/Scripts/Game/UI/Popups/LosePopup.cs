using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.UI
{
    // "Out of Moves": try again, or go home.
    public sealed class LosePopup : Popup
    {
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;

        public Button RetryButton => retryButton;
        public Button HomeButton => homeButton;
    }
}

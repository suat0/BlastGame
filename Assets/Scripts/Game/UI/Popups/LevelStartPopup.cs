using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.UI
{
    // "Level 5" with its goal and move limit, and a Play button. Opens on the home screen before a
    // level loads, and inside the level again when the player retries.
    public sealed class LevelStartPopup : Popup
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private GameObject hardRibbon;

        [Tooltip("Hidden whole when the level has no Boxes, so the icon goes with the count.")]
        [SerializeField] private GameObject goalGroup;
        [SerializeField] private TMP_Text goalLabel;
        [SerializeField] private TMP_Text movesLabel;

        [SerializeField] private Button playButton;
        [SerializeField] private Button closeButton;

        public Button PlayButton => playButton;
        public Button CloseButton => closeButton;

        // levelNumber is one-based, as the player counts; zero means a debug level with no place in
        // the campaign.
        public void Show(int levelNumber, bool hard, int boxes, int moves)
        {
            if (levelNumber > 0) titleLabel.SetText("Level {0:0}", levelNumber);
            else titleLabel.SetText("Test level");

            hardRibbon.SetActive(hard);

            goalGroup.SetActive(boxes > 0);
            goalLabel.SetText("{0:0}", boxes);

            if (moves > 0) movesLabel.SetText("{0:0} moves", moves);
            else movesLabel.SetText("No move limit");

            Show();
        }
    }
}

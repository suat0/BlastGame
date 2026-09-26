using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.UI
{
    // Settings inside a level: sound switches, back to the board, or out to the home screen.
    public sealed class PausePopup : Popup
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button sfxButton;
        [SerializeField] private TMP_Text musicLabel;
        [SerializeField] private TMP_Text sfxLabel;

        public Button ResumeButton => resumeButton;
        public Button LeaveButton => leaveButton;

        private void OnEnable()
        {
            musicButton.onClick.AddListener(HandleMusicClicked);
            sfxButton.onClick.AddListener(HandleSfxClicked);
            RefreshLabels();
        }

        private void OnDisable()
        {
            musicButton.onClick.RemoveListener(HandleMusicClicked);
            sfxButton.onClick.RemoveListener(HandleSfxClicked);
        }

        // The switches write the save directly: they are settings, not part of the level, and there is
        // nothing for the level's states to decide about them.
        private void HandleMusicClicked()
        {
            PlayerProgress.MusicOn = !PlayerProgress.MusicOn;
            RefreshLabels();
        }

        private void HandleSfxClicked()
        {
            PlayerProgress.SfxOn = !PlayerProgress.SfxOn;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            musicLabel.text = PlayerProgress.MusicOn ? "Music: On" : "Music: Off";
            sfxLabel.text = PlayerProgress.SfxOn ? "Sound: On" : "Sound: Off";
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.UI
{
    // Sound switches plus one way out, which differs by screen: in a level it is "Leave level", on the
    // home screen "Reset progress". A popup built without one simply has no such button.
    public sealed class SettingsPopup : Popup
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button sfxButton;
        [SerializeField] private TMP_Text musicLabel;
        [SerializeField] private TMP_Text sfxLabel;

        [Tooltip("The screen-specific action. Optional.")]
        [SerializeField] private Button actionButton;

        public Button CloseButton => closeButton;
        public Button ActionButton => actionButton;

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

        // The switches write the save directly: they are settings, not part of a level, and there is
        // nothing for any state to decide about them.
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

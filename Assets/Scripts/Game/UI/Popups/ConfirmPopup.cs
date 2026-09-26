using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.UI
{
    // "Are you sure?" for the two taps that cannot be taken back: leaving a level and resetting
    // progress. The caller supplies the words.
    public sealed class ConfirmPopup : Popup
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private TMP_Text confirmLabel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        public Button ConfirmButton => confirmButton;
        public Button CancelButton => cancelButton;

        public void Show(string title, string body, string confirm)
        {
            titleLabel.text = title;
            bodyLabel.text = body;
            confirmLabel.text = confirm;

            Show();
        }
    }
}

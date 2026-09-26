using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.UI
{
    // "Leave the level?" A tap on Leave cannot be taken back, so it is asked once more.
    public sealed class ConfirmExitPopup : Popup
    {
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button stayButton;

        public Button LeaveButton => leaveButton;
        public Button StayButton => stayButton;
    }
}

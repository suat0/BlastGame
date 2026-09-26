using BlastGame.Core;
using TMPro;
using UnityEngine;

namespace BlastGame.Game.UI
{
    // Moves and the objective. Nothing else: the score moved to the end-of-level card, as in the games
    // this one is modelled on, so the only numbers in view during play are the two that decide it.
    //
    // Canvas UI here, deliberately not on the board: what the board avoids is the canvas rebuild, and
    // two labels changing once per move are the opposite of a hundred blocks moving every frame.
    // Reads the session rather than being told what to print, so a new label costs nothing elsewhere.
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private GameController controller;

        [Header("Labels")]
        [SerializeField] private TMP_Text movesLabel;
        [SerializeField] private TMP_Text objectiveLabel;

        [Tooltip("Hidden whole when the level has no objective, so the icon goes with the count.")]
        [SerializeField] private GameObject objectiveGroup;

        private void OnEnable() => controller.OnStatusChanged += HandleStatusChanged;

        private void OnDisable() => controller.OnStatusChanged -= HandleStatusChanged;

        private void HandleStatusChanged()
        {
            GameSession session = controller.Session;

            // SetText with an argument, not an interpolated string: TMP formats into a buffer it
            // already owns, so a label that changes every move allocates nothing.
            movesLabel.SetText("{0:0}", session.HasMoveLimit ? session.MovesLeft : session.Moves);

            // The objective is data that can be absent, not a second game mode.
            objectiveGroup.SetActive(session.HasObjective);
            if (session.HasObjective) objectiveLabel.SetText("{0:0}", session.RemainingBoxes);
        }
    }
}

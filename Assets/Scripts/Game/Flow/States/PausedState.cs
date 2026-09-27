using UnityEngine;

namespace BlastGame.Game
{
    // The settings popup over a frozen board. timeScale 0 stops every board animation at once, since
    // all of them run on scaled time; the popups run on unscaled time and keep moving.
    public sealed class PausedState : LevelState
    {
        public PausedState(LevelFlow flow) : base(flow) { }

        public override void Enter()
        {
            Time.timeScale = 0f;
            Flow.PausePopup.Show();
        }

        // Only a return to play unfreezes. Going on to the confirmation keeps the board frozen behind
        // it, and leaving the level resets the scale on the way out (SceneTransition).
        public override void Exit() => Flow.PausePopup.Hide();

        public override void OnBack() => Resume();

        public override void OnSettingsPressed() => Resume();

        public void Resume()
        {
            if (!IsCurrent) return;

            Time.timeScale = 1f;
            Flow.ChangeState(Flow.Playing);
        }

        public void AskToLeave()
        {
            if (!IsCurrent) return;
            Flow.ChangeState(Flow.ConfirmExit);
        }
    }
}

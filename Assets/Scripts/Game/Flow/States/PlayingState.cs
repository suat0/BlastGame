using BlastGame.Core;

namespace BlastGame.Game
{
    // The board takes taps. Leaves for the pause menu, or - when Core reports the level decided - for
    // the wait while the last move finishes falling.
    //
    // Also counts how long the player has done nothing, and after a while points at the biggest
    // group. Any tap at all, played or not, resets the count and puts the hint away.
    public sealed class PlayingState : LevelState
    {
        private float idle;
        private bool hinting;

        public PlayingState(LevelFlow flow) : base(flow) { }

        public override void Enter()
        {
            idle = 0f;
            hinting = false;
            Flow.SetInputEnabled(true);
        }

        public override void Exit()
        {
            Flow.SetInputEnabled(false);
            StopHint();
        }

        // Only while the board is still: a hint pointing at blocks that are about to move points at
        // the wrong place.
        public override void Tick(float unscaledDeltaTime)
        {
            if (hinting) return;

            idle += unscaledDeltaTime;
            if (idle < Flow.HintDelay || !Flow.BoardView.IsIdle) return;

            hinting = true;
            Flow.BoardView.ShowHint();
        }

        public override void OnBack() => Flow.ChangeState(Flow.Paused);

        public override void OnSettingsPressed() => Flow.ChangeState(Flow.Paused);

        public override void OnTapRejected() => StopHint();

        public override void OnStatusChanged(GameState state)
        {
            StopHint();

            if (state == GameState.Playing) return;

            Flow.Settling.Await(state == GameState.Won ? (LevelState)Flow.Won : Flow.Lost);
            Flow.ChangeState(Flow.Settling);
        }

        private void StopHint()
        {
            idle = 0f;
            if (!hinting) return;

            hinting = false;
            Flow.BoardView.HideHint();
        }
    }
}

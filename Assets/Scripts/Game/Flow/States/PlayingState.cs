using BlastGame.Core;

namespace BlastGame.Game
{
    // The board takes taps. Leaves for the pause menu, or - when Core reports the level decided - for
    // the wait while the last move finishes falling.
    public sealed class PlayingState : LevelState
    {
        public PlayingState(LevelFlow flow) : base(flow) { }

        public override void Enter() => Flow.SetInputEnabled(true);

        public override void Exit() => Flow.SetInputEnabled(false);

        public override void OnBack() => Flow.ChangeState(Flow.Paused);

        public override void OnSettingsPressed() => Flow.ChangeState(Flow.Paused);

        public override void OnStatusChanged(GameState state)
        {
            if (state == GameState.Playing) return;

            Flow.Settling.Await(state == GameState.Won ? (LevelState)Flow.Won : Flow.Lost);
            Flow.ChangeState(Flow.Settling);
        }
    }
}

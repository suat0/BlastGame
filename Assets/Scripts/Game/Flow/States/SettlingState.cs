namespace BlastGame.Game
{
    // Core has decided the level; the screen has not caught up. Waits for every block to land, then
    // a beat longer, then shows the outcome - an end card over blocks still in the air would hide the
    // move that decided it.
    public sealed class SettlingState : LevelState
    {
        private LevelState outcome;
        private float beat;

        public SettlingState(LevelFlow flow) : base(flow) { }

        public void Await(LevelState next) => outcome = next;

        public override void Enter() => beat = Flow.OutcomeBeat;

        public override void Tick(float unscaledDeltaTime)
        {
            if (!Flow.BoardView.IsIdle) return;

            beat -= unscaledDeltaTime;
            if (beat <= 0f) Flow.ChangeState(outcome);
        }
    }
}

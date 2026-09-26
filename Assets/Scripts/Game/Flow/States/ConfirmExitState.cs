namespace BlastGame.Game
{
    // "Leave the level?" over the frozen board. Staying goes back to the pause menu it came from.
    public sealed class ConfirmExitState : LevelState
    {
        public ConfirmExitState(LevelFlow flow) : base(flow) { }

        public override void Enter() =>
            Flow.ConfirmExitPopup.Show("Leave level?", "Your progress in this level will be lost.", "Leave");

        public override void Exit() => Flow.ConfirmExitPopup.Hide();

        public override void OnBack() => Stay();

        public void Leave()
        {
            if (Flow.Current != this) return;
            Flow.GoHome();
        }

        public void Stay()
        {
            if (Flow.Current != this) return;
            Flow.ChangeState(Flow.Paused);
        }
    }
}

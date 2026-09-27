namespace BlastGame.Game
{
    // Out of moves. Retry regenerates the board in place - no scene reload, no new pool - and briefs
    // the level again.
    public sealed class LostState : LevelState
    {
        public LostState(LevelFlow flow) : base(flow) { }

        public override void Enter()
        {
            Flow.LosePopup.Show();
            Sfx.Play(SfxId.Lose);
        }

        public override void Exit() => Flow.LosePopup.Hide();

        public override void OnBack() => Leave();

        public void Retry()
        {
            if (!IsCurrent) return;

            Flow.RestartLevel();
            Flow.ChangeState(Flow.Briefing);
        }

        public void Leave()
        {
            if (!IsCurrent) return;
            Flow.GoHome();
        }
    }
}

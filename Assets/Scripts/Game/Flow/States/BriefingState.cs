namespace BlastGame.Game
{
    // The start popup, opened inside the level after a retry. The first attempt is briefed on the
    // home screen before the level loads, so it starts at the intro instead.
    public sealed class BriefingState : LevelState
    {
        public BriefingState(LevelFlow flow) : base(flow) { }

        public override void Enter() => Flow.StartPopup.Show(Flow.LevelNumber, Flow.Controller.Level);

        public override void Exit() => Flow.StartPopup.Hide();

        public override void OnBack() => Leave();

        public void Play()
        {
            if (!IsCurrent) return;
            Flow.ChangeState(Flow.Intro);
        }

        // Closing the start popup of a level already failed once is walking away from it.
        public void Leave()
        {
            if (!IsCurrent) return;
            Flow.GoHome();
        }
    }
}

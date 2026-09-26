namespace BlastGame.Game
{
    // The goal banner sweeps across while the board settles in. No input until it has gone.
    public sealed class IntroState : LevelState
    {
        private float remaining;

        public IntroState(LevelFlow flow) : base(flow) { }

        public override void Enter()
        {
            remaining = Flow.IntroDuration;
            Flow.IntroBanner.Play(Flow.Controller.Session.RemainingBoxes, Flow.IntroDuration * 0.6f);
        }

        public override void Tick(float unscaledDeltaTime)
        {
            remaining -= unscaledDeltaTime;
            if (remaining <= 0f) Flow.ChangeState(Flow.Playing);
        }
    }
}

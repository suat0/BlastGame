namespace BlastGame.Game
{
    // The level is won and the board has settled. A flash and confetti over the board, then the card.
    //
    // Progress and coins are saved on entry, before any of that is visible - a player who kills the
    // app during the celebration keeps the win.
    public sealed class WonState : LevelState
    {
        private float untilCard;
        private int coins;

        public WonState(LevelFlow flow) : base(flow) { }

        public override void Enter()
        {
            coins = Flow.CompleteLevel();
            untilCard = Flow.CelebrationDuration;

            Flow.Feedback.Flash();
            Flow.BoardView.Celebrate();
        }

        public override void Tick(float unscaledDeltaTime)
        {
            if (untilCard <= 0f) return;

            untilCard -= unscaledDeltaTime;
            if (untilCard <= 0f) Flow.WinPopup.Show(Flow.Controller.Session.Score, coins);
        }

        public override void OnBack() => Continue();

        public void Continue()
        {
            // Not before the card is up: a back press during the confetti would skip the reward.
            if (Flow.Current != this || untilCard > 0f) return;
            Flow.GoHome();
        }
    }
}

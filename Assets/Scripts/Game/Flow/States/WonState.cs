namespace BlastGame.Game
{
    // The level is won and the board has settled. Progress and coins are saved on entry, before the
    // card is even visible - a player who kills the app on the win card keeps the win.
    public sealed class WonState : LevelState
    {
        public WonState(LevelFlow flow) : base(flow) { }

        public override void Enter()
        {
            int coins = Flow.CompleteLevel();
            Flow.WinPopup.Show(Flow.Controller.Session.Score, coins);
        }

        public override void OnBack() => Continue();

        public void Continue()
        {
            if (Flow.Current != this) return;
            Flow.GoHome();
        }
    }
}

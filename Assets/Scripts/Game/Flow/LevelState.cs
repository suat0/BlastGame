using BlastGame.Core;

namespace BlastGame.Game
{
    // One phase of a level as the player sees it: the goal banner, play, the pause menu, the end
    // card. Not Core's GameState - Core says "won" the instant the last move resolves, while on
    // screen blocks are still falling and the level is not over yet.
    //
    // An abstract class rather than an interface: every state ignores most of these events, and the
    // empty defaults are what let each one override only the two or three it answers.
    public abstract class LevelState
    {
        protected readonly LevelFlow Flow;

        protected LevelState(LevelFlow flow) => Flow = flow;

        public virtual void Enter() { }

        public virtual void Exit() { }

        // Unscaled, so a state that waits on a timer still counts while the game is paused.
        public virtual void Tick(float unscaledDeltaTime) { }

        // Escape on a keyboard, the back button on Android.
        public virtual void OnBack() { }

        public virtual void OnSettingsPressed() { }

        // Core resolved a move and the session's status changed. Called before any view has animated
        // the move.
        public virtual void OnStatusChanged(GameState state) { }

        // A tap that played nothing - a lone block, a Box.
        public virtual void OnTapRejected() { }
    }
}

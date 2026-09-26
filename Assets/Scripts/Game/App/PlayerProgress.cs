using UnityEngine;

namespace BlastGame.Game
{
    // The one door to saved state. Nothing else names SaveSystem or knows the data is a JSON file, so
    // moving to a server-backed save touches this class and SaveSystem and nothing that calls them.
    public static class PlayerProgress
    {
        private static SaveData data;

        private static SaveData Data => data ??= SaveSystem.Load();

        // Zero-based, so it indexes LevelCatalog directly; the player is shown LevelIndex + 1.
        public static int LevelIndex => Data.levelIndex;

        public static int Coins => Data.coins;

        public static bool MusicOn
        {
            get => Data.musicOn;
            set { Data.musicOn = value; Save(); }
        }

        public static bool SfxOn
        {
            get => Data.sfxOn;
            set { Data.sfxOn = value; Save(); }
        }

        // Set by a win and read once by the home screen, which counts the coins in. Deliberately not
        // saved: an app killed between the win and the home screen loses an animation, not the coins.
        public static int PendingCoinReward { get; private set; }

        // Saved at once rather than on the way out. Mobile apps are killed without notice, and a
        // level won and then lost to a kill is the one loss a player will certainly notice.
        public static void CompleteLevel(int coinsEarned)
        {
            Data.levelIndex++;
            Data.coins += coinsEarned;

            PendingCoinReward += coinsEarned;

            Save();
        }

        public static int TakePendingCoinReward()
        {
            int reward = PendingCoinReward;
            PendingCoinReward = 0;
            return reward;
        }

        public static void Save() => SaveSystem.Save(Data);

        public static void ResetAll()
        {
            SaveSystem.Delete();

            data = new SaveData();
            PendingCoinReward = 0;
        }

        // With domain reload turned off in the editor, statics survive from one play session to the
        // next. Dropping the cache here makes every session read the file afresh, as a build does.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            data = null;
            PendingCoinReward = 0;
        }
    }
}

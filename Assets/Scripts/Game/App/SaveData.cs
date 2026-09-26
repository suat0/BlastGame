using System;

namespace BlastGame.Game
{
    // Everything that outlives the app, in one object written as one file. Plain fields because
    // JsonUtility serialises fields, not properties.
    [Serializable]
    public sealed class SaveData
    {
        // Bumped when a field changes meaning. SaveSystem compares it on load, so a future format
        // change has one place to migrate from instead of reading old values as new ones.
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        public int levelIndex;
        public int coins;

        public bool musicOn = true;
        public bool sfxOn = true;
    }
}

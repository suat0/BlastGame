using System;
using UnityEngine;

namespace BlastGame.Game
{
    // The campaign, in order. A level is still just a LevelConfig asset; this only says which comes
    // after which.
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Blast/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private LevelConfig[] levels = Array.Empty<LevelConfig>();

        [Tooltip("Past the last level the campaign replays this many of its final levels in a loop, " +
                 "so a demo never runs out. A shipped game would show 'more levels soon' instead.")]
        [SerializeField, Min(1)] private int loopTail = 4;

        public int Count => levels.Length;

        // Any index at all resolves to a level: the saved progress only ever grows, and a player who
        // has finished the campaign still has a next level to press.
        public LevelConfig At(int index)
        {
            if (levels.Length == 0) throw new InvalidOperationException("The level catalog is empty.");
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "Level index cannot be negative.");

            if (index < levels.Length) return levels[index];

            int tail = Mathf.Min(loopTail, levels.Length);
            return levels[levels.Length - tail + (index - levels.Length) % tail];
        }
    }
}

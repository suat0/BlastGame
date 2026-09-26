using System;
using UnityEngine;

namespace BlastGame.Game
{
    // How the home screen tells the level scene what to play. Both scenes reference this one asset;
    // home writes it before loading, the level reads it on start.
    //
    // Two traps come with a ScriptableObject used as runtime state, and both are closed here:
    // - Values written in play mode persist into the asset in the editor. The fields are
    //   NonSerialized, so nothing written here ever reaches the file.
    // - With domain reload off, a value can survive from one play session into the next. The request
    //   is consumed on read, so a level scene opened on its own finds nothing and plays its own
    //   debug level instead of whatever the last session asked for.
    [CreateAssetMenu(fileName = "LevelRequest", menuName = "Blast/Level Request")]
    public sealed class LevelRequest : ScriptableObject
    {
        [NonSerialized] private LevelConfig level;
        [NonSerialized] private int campaignIndex;

        public void Set(LevelConfig requested, int index)
        {
            level = requested != null ? requested : throw new ArgumentNullException(nameof(requested));
            campaignIndex = index;
        }

        public bool TryTake(out LevelConfig requested, out int index)
        {
            requested = level;
            index = campaignIndex;

            level = null;
            campaignIndex = 0;

            return requested != null;
        }
    }
}

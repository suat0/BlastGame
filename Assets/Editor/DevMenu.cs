#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BlastGame.Game.EditorTools
{
    public static class DevMenu
    {
        // Back to Level 1 with no coins, without opening the settings popup in play mode.
        [MenuItem("Tools/Blast/Reset Progress")]
        private static void ResetProgress()
        {
            PlayerProgress.ResetAll();
            Debug.Log("Progress reset.");
        }
    }
}
#endif

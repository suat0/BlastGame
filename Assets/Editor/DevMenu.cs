#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
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

        // The popups first, since both scenes instantiate them. The builders open and replace scenes,
        // so unsaved changes are offered for saving before anything is touched.
        [MenuItem("Tools/Blast/Rebuild UI")]
        private static void RebuildUi()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            PopupBuilder.Build();
            LevelBuilder.Build();
            VfxBuilder.Build();
            HomeBuilder.Build();
        }
    }
}
#endif

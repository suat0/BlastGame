#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlastGame.Game.EditorTools
{
    // Joins the two scenes: the build order that makes home the first screen, and the request asset
    // the level scene reads its level from.
    public static class SceneSetup
    {
        public const string LevelScenePath = "Assets/Scenes/Level.unity";

        private const string RequestPath = "Assets/Levels/LevelRequest.asset";

        public static void Apply()
        {
            // Home first: index 0 is the scene a build opens with.
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(HomeBuilder.ScenePath, true),
                new EditorBuildSettingsScene(LevelScenePath, true),
            };

            Scene level = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);

            var controller = Object.FindFirstObjectByType<GameController>();
            if (controller == null) throw new MissingComponentException("No GameController in the level scene.");

            var so = new SerializedObject(controller);
            so.FindProperty("request").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelRequest>(RequestPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(level);
            EditorSceneManager.SaveScene(level);

            Debug.Log("Build scenes set and level scene wired.");
        }
    }
}
#endif

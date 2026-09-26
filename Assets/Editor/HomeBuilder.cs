#if UNITY_EDITOR
using BlastGame.Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlastGame.Game.EditorTools
{
    // Builds the home scene from nothing and saves it. A scaffold, like HudBuilder: once the generated
    // art is in and the layout settles, the scene becomes the source of truth and this is retired.
    public static class HomeBuilder
    {
        public const string ScenePath = "Assets/Scenes/Home.unity";

        private const string CatalogPath = "Assets/Levels/LevelCatalog.asset";
        private const string RequestPath = "Assets/Levels/LevelRequest.asset";

        private static readonly Color Sky = UiBuild.Hex("#2A1B5C");
        private static readonly Color TitleInk = UiBuild.Hex("#FFD84D");
        private static readonly Color PlayFace = UiBuild.Hex("#5BC236");
        private static readonly Color PlayShade = UiBuild.Hex("#2E7A1B");
        private static readonly Color White = Color.white;

        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Sky;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            UiBuild.EnsureEventSystem();

            Canvas canvas = UiBuild.CreateCanvas("HomeUI", null, 0);
            Transform root = canvas.transform;

            UiBuild.Fill("Backdrop", root, Sky);

            TMP_Text title = UiBuild.Text("Title", root, "BLAST VILLAINS", 120f, TitleInk, TextAlignmentOptions.Center);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.7f);
            titleRect.sizeDelta = new Vector2(1000f, 200f);

            var levelButton = UiBuild.CreateButton("LevelButton", root, new Vector2(620f, 200f), Vector2.zero,
                                                   PlayFace, PlayShade, "Level 1", 96f, White);
            var levelRect = (RectTransform)levelButton.transform;
            levelRect.anchorMin = levelRect.anchorMax = new Vector2(0.5f, 0.22f);

            var view = canvas.gameObject.AddComponent<HomeView>();
            var so = new SerializedObject(view);
            so.FindProperty("catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            so.FindProperty("request").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelRequest>(RequestPath);
            so.FindProperty("levelButton").objectReferenceValue = levelButton;
            so.FindProperty("levelLabel").objectReferenceValue = levelButton.GetComponentInChildren<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Home scene built.");
        }
    }
}
#endif

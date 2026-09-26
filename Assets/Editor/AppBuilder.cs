#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.EditorTools
{
    // Builds the App prefab that App.Create loads before the first scene, and applies the player
    // settings a portrait game needs. A generator like HudBuilder: the prefab is what ships, and
    // running this again replaces it.
    public static class AppBuilder
    {
        private const string PrefabPath = "Assets/Resources/App.prefab";

        // The same colour the level's backdrop starts from, so a fade reads as the screen dimming
        // rather than a black flash.
        private static readonly Color Cover = new Color32(0x1E, 0x16, 0x38, 0xFF);

        public static void Build()
        {
            ApplyPlayerSettings();
            BuildPrefab();

            AssetDatabase.SaveAssets();
            Debug.Log("App prefab and player settings written.");
        }

        private static void ApplyPlayerSettings()
        {
            // Auto-rotation with landscape allowed turned a phone on its side into a broken layout.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // A desktop build is how the reviewers are most likely to play it. Full screen on a
            // landscape monitor would stretch the UI sideways, so it opens as a phone-shaped window
            // short enough for a 13-inch laptop.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 486;
            PlayerSettings.defaultScreenHeight = 864;
            PlayerSettings.resizableWindow = false;
        }

        private static void BuildPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var root = new GameObject("App");
            var app = root.AddComponent<App>();

            var transitionObject = new GameObject("Transition",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
            transitionObject.transform.SetParent(root.transform, false);

            var canvas = transitionObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;          // above every scene's UI

            var coverImage = new GameObject("Cover", typeof(RectTransform), typeof(Image));
            var coverRect = (RectTransform)coverImage.transform;
            coverRect.SetParent(transitionObject.transform, false);
            coverRect.anchorMin = Vector2.zero;
            coverRect.anchorMax = Vector2.one;
            coverRect.offsetMin = Vector2.zero;
            coverRect.offsetMax = Vector2.zero;

            var image = coverImage.GetComponent<Image>();
            image.color = Cover;
            image.raycastTarget = true;          // the cover is what swallows taps mid-transition
            image.maskable = false;

            var transition = transitionObject.AddComponent<SceneTransition>();

            var transitionSo = new SerializedObject(transition);
            transitionSo.FindProperty("cover").objectReferenceValue = transitionObject.GetComponent<CanvasGroup>();
            transitionSo.ApplyModifiedPropertiesWithoutUndo();

            var appSo = new SerializedObject(app);
            appSo.FindProperty("transition").objectReferenceValue = transition;
            appSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }
    }
}
#endif

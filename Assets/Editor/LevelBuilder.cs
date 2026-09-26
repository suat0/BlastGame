#if UNITY_EDITOR
using BlastGame.Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlastGame.Game.EditorTools
{
    // Rebuilds the level scene's UI from scratch and saves the scene: the HUD, the popups, the intro
    // banner and the LevelFlow that drives them. The layout lives here rather than in the scene file
    // alone so it can be read, reviewed and re-run; a canvas assembled by dragging is a diff nobody
    // can review.
    //
    // A generator, not a binding. The scene is what ships; running this replaces the UI wholesale,
    // so an edit made in the inspector is lost the next time it runs. It is a scaffold - once the
    // generated art is in and the layout settles, the scene becomes the source of truth and this file
    // is retired rather than left to drift.
    public static class LevelBuilder
    {
        private const string BoxSpritePath = "Assets/Art/Board/Box0.png";

        // One place for every colour in the HUD, picked against Match Villains' in-level HUD: a purple
        // bar, white plates, dark purple figures.
        private static readonly Color Figure = UiBuild.Hex("#2E2153");
        private static readonly Color Caption = UiBuild.Hex("#FFFFFF");
        private static readonly Color Gold = UiBuild.Hex("#FFD84D");
        private static readonly Color BannerFill = new Color(0.18f, 0.10f, 0.40f, 0.92f);
        private static readonly Color Sky = UiBuild.Hex("#1E1638");
        private static readonly Color Well = UiBuild.Hex("#17102F");

        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(SceneSetup.LevelScenePath, OpenSceneMode.Single);

            var hud = Object.FindFirstObjectByType<HudView>();
            if (hud == null) throw new MissingComponentException("No HudView in the scene.");

            Transform hudCanvas = hud.transform;
            for (int i = hudCanvas.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(hudCanvas.GetChild(i).gameObject);

            // Everything tappable or readable sits inside the safe area; nothing in the HUD runs to the
            // screen edge, so the whole HUD goes under it.
            RectTransform safe = UiBuild.Rect("Safe", hudCanvas);
            UiBuild.Stretch(safe, 0f, 0f);
            safe.gameObject.AddComponent<SafeArea>();

            BuildTopBar(safe, out TMP_Text moves, out TMP_Text objective, out GameObject objectiveGroup,
                        out RectTransform objectiveIcon);
            Button settings = BuildSettingsButton(safe);
            RectTransform boardArea = BuildBoardArea(safe);

            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("movesLabel").objectReferenceValue = moves;
            hudSo.FindProperty("objectiveLabel").objectReferenceValue = objective;
            hudSo.FindProperty("objectiveGroup").objectReferenceValue = objectiveGroup;
            hudSo.FindProperty("objectiveIcon").objectReferenceValue = objectiveIcon;
            hudSo.FindProperty("boardView").objectReferenceValue = Object.FindFirstObjectByType<BoardView>();

            // Boxes flying from the board to the goal. On the HUD canvas root, outside the safe area,
            // since they start wherever the board is.
            SerializedProperty flyerList = hudSo.FindProperty("flyers");
            flyerList.arraySize = 10;
            for (int i = 0; i < 10; i++)
                flyerList.GetArrayElementAtIndex(i).objectReferenceValue = Flyer(hudCanvas);
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            FeedbackView feedback = BuildFeedback();
            BuildFlow(settings, hud, feedback);
            BuildBackdrop(boardArea);

            Camera camera = Camera.main;
            if (camera != null) camera.backgroundColor = Sky;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("Level UI rebuilt.");
        }

        // The popups get a canvas of their own above the HUD: a popup animating in rebuilds its own
        // canvas and leaves the HUD's alone.
        private static void BuildFlow(Button settings, HudView hud, FeedbackView feedback)
        {
            Replace(null, "Popups");
            Replace(null, "Flow");

            Canvas canvas = UiBuild.CreateCanvas("Popups", null, 10);
            Transform root = canvas.transform;

            IntroBanner banner = BuildIntroBanner(root);

            var start = Instantiate<LevelStartPopup>(PopupBuilder.LevelStartPath, root);
            var pause = Instantiate<SettingsPopup>(PopupBuilder.PausePath, root);
            var confirm = Instantiate<ConfirmPopup>(PopupBuilder.ConfirmPath, root);
            var win = Instantiate<WinPopup>(PopupBuilder.WinPath, root);
            var lose = Instantiate<LosePopup>(PopupBuilder.LosePath, root);

            var flowObject = new GameObject("Flow");
            var flow = flowObject.AddComponent<LevelFlow>();

            var so = new SerializedObject(flow);
            so.FindProperty("controller").objectReferenceValue = Object.FindFirstObjectByType<GameController>();
            so.FindProperty("boardView").objectReferenceValue = Object.FindFirstObjectByType<BoardView>();
            so.FindProperty("input").objectReferenceValue = Object.FindFirstObjectByType<InputHandler>();
            so.FindProperty("hud").objectReferenceValue = hud;
            so.FindProperty("feedback").objectReferenceValue = feedback;
            so.FindProperty("settingsButton").objectReferenceValue = settings;
            so.FindProperty("introBanner").objectReferenceValue = banner;
            so.FindProperty("startPopup").objectReferenceValue = start;
            so.FindProperty("pausePopup").objectReferenceValue = pause;
            so.FindProperty("confirmExitPopup").objectReferenceValue = confirm;
            so.FindProperty("winPopup").objectReferenceValue = win;
            so.FindProperty("losePopup").objectReferenceValue = lose;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T Instantiate<T>(string prefabPath, Transform parent) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new System.IO.FileNotFoundException(prefabPath);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return instance.GetComponent<T>();
        }

        private static IntroBanner BuildIntroBanner(Transform canvas)
        {
            RectTransform root = UiBuild.Rect("IntroBanner", canvas);
            UiBuild.Stretch(root, 0f, 0f);

            RectTransform strip = UiBuild.Rect("Strip", root);
            strip.anchorMin = new Vector2(0f, 0.5f);
            strip.anchorMax = new Vector2(1f, 0.5f);
            strip.sizeDelta = new Vector2(0f, 220f);
            strip.anchoredPosition = Vector2.zero;

            UiBuild.Fill("Fill", strip, BannerFill);

            TMP_Text label = UiBuild.Text("Label", strip, "Break 8 boxes!", 96f, Caption, TextAlignmentOptions.Center);
            UiBuild.Stretch(label.rectTransform, 0f, 0f);

            var group = strip.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var banner = root.gameObject.AddComponent<IntroBanner>();
            var so = new SerializedObject(banner);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("strip").objectReferenceValue = strip;
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            return banner;
        }

        // Match Villains' layout: a purple bar across the top holding a white Moves plate and a white
        // Goals plate, with the Count's portrait hanging off the right end.
        private static void BuildTopBar(Transform canvas, out TMP_Text moves, out TMP_Text objective,
                                        out GameObject objectiveGroup, out RectTransform objectiveIcon)
        {
            RectTransform bar = UiBuild.Rect("TopBar", canvas);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(-60f, 250f);      // 30px of margin on each side
            bar.anchoredPosition = new Vector2(0f, -40f);

            // Drawn first so the fill covers it except where it is offset: a solid shadow, not a
            // blurred one, which is what the block art does too.
            Image barFace = UiBuild.Picture("Fill", bar, UiBuild.Art("panel_purple"), Vector2.zero);
            UiBuild.Stretch(barFace.rectTransform, 0f, 0f);

            RectTransform movesPlate = Plate(bar, "Moves", new Vector2(40f, 0f), 230f);
            moves = UiBuild.Text("Value", movesPlate, "20", 96f, Figure, TextAlignmentOptions.Center);
            UiBuild.Stretch(moves.rectTransform, 0f, -4f);

            RectTransform goalsPlate = Plate(bar, "Goals", new Vector2(300f, 0f), 380f);
            objectiveGroup = goalsPlate.parent.parent.gameObject;   // the column, caption and all

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)icon.transform;
            iconRect.SetParent(goalsPlate, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(100f, 100f);
            iconRect.anchoredPosition = new Vector2(-70f, 0f);

            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BoxSpritePath);
            UiBuild.MakeNonInteractive(iconImage);
            objectiveIcon = iconRect;

            objective = UiBuild.Text("Count", goalsPlate, "8", 80f, Figure, TextAlignmentOptions.Left);
            RectTransform countRect = objective.rectTransform;
            countRect.anchorMin = countRect.anchorMax = new Vector2(0.5f, 0.5f);
            countRect.sizeDelta = new Vector2(150f, 100f);
            countRect.anchoredPosition = new Vector2(70f, -4f);

            // The Count watches from the corner, framed in a gold shield, hanging below the bar the
            // way Match Villains hangs its portrait.
            RectTransform portrait = UiBuild.Rect("Portrait", bar);
            portrait.anchorMin = portrait.anchorMax = new Vector2(1f, 0.5f);
            portrait.sizeDelta = new Vector2(250f, 290f);
            portrait.anchoredPosition = new Vector2(-140f, -30f);

            Image face = UiBuild.Picture("Face", portrait, UiBuild.Art("portrait_count"), new Vector2(200f, 200f));
            UiBuild.Place(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f));
            Image shield = UiBuild.Picture("Frame", portrait, UiBuild.Art("frame_portrait"), new Vector2(250f, 290f));
            UiBuild.Place(shield.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero);
        }

        private static RectTransform Flyer(Transform canvas)
        {
            var go = new GameObject("BoxFlyer", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(100f, 100f);

            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BoxSpritePath);
            UiBuild.MakeNonInteractive(image);
            return rect;
        }

        // Between the HUD and the popups: score popups, the combo word, the flash.
        private static FeedbackView BuildFeedback()
        {
            Replace(null, "Feedback");

            Canvas canvas = UiBuild.CreateCanvas("Feedback", null, 5);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;   // shows things, takes no taps
            Transform root = canvas.transform;

            Image flash = UiBuild.Fill("Flash", root, Color.white);

            var popups = new TMP_Text[6];
            for (int i = 0; i < popups.Length; i++)
            {
                TMP_Text popup = UiBuild.Text("Score", root, "+0", 72f, Color.white, TextAlignmentOptions.Center);
                popup.fontSharedMaterial = UiBuild.Outline;
                popup.rectTransform.anchorMin = popup.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                popup.rectTransform.sizeDelta = new Vector2(300f, 100f);
                popups[i] = popup;
            }

            TMP_Text combo = UiBuild.Text("Combo", root, "Amazing!", 150f, Gold, TextAlignmentOptions.Center);
            combo.fontSharedMaterial = UiBuild.Outline;
            combo.textWrappingMode = TextWrappingModes.NoWrap;
            RectTransform comboRect = combo.rectTransform;
            comboRect.anchorMin = comboRect.anchorMax = new Vector2(0.5f, 0.62f);
            comboRect.sizeDelta = new Vector2(1000f, 220f);

            var feedback = canvas.gameObject.AddComponent<FeedbackView>();
            var so = new SerializedObject(feedback);
            so.FindProperty("controller").objectReferenceValue = Object.FindFirstObjectByType<GameController>();
            so.FindProperty("boardView").objectReferenceValue = Object.FindFirstObjectByType<BoardView>();
            so.FindProperty("comboLabel").objectReferenceValue = combo;
            so.FindProperty("flash").objectReferenceValue = flash;
            SerializedProperty list = so.FindProperty("scorePopups");
            list.arraySize = popups.Length;
            for (int i = 0; i < popups.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = popups[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            return feedback;
        }

        // A white plate under a caption, as the in-level HUD of Match Villains lays out Moves and Goals.
        // Returns the plate; its parent is the whole column, caption included.
        private static RectTransform Plate(RectTransform bar, string caption, Vector2 left, float width)
        {
            RectTransform column = UiBuild.Rect(caption, bar);
            column.anchorMin = new Vector2(0f, 0f);
            column.anchorMax = new Vector2(0f, 1f);
            column.pivot = new Vector2(0f, 0.5f);
            column.sizeDelta = new Vector2(width, 0f);
            column.anchoredPosition = left;

            TMP_Text label = UiBuild.Text("Caption", column, caption, 44f, Caption, TextAlignmentOptions.Center);
            label.fontSharedMaterial = UiBuild.Outline;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(0f, 60f);
            labelRect.anchoredPosition = new Vector2(0f, -10f);

            RectTransform plateArea = UiBuild.Rect("PlateArea", column);
            plateArea.anchorMin = new Vector2(0f, 0f);
            plateArea.anchorMax = new Vector2(1f, 1f);
            plateArea.offsetMin = new Vector2(0f, 28f);
            plateArea.offsetMax = new Vector2(0f, -78f);

            Image plate = UiBuild.Picture("Plate", plateArea, UiBuild.Art("panel_cream"), Vector2.zero);
            UiBuild.Stretch(plate.rectTransform, 0f, 0f);
            return plate.rectTransform;
        }

        // Bottom right, where Match Villains keeps its settings gear.
        private static Button BuildSettingsButton(Transform canvas)
        {
            Button button = UiBuild.ArtButton("SettingsButton", canvas, UiBuild.Art("icon_gear"), new Vector2(170f, 170f), null, 0f);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-40f, 50f);
            return button;
        }

        // The gradient behind everything and the well the grid sits in. Both use sprites from
        // BlockAtlas, so they share the blocks' material and cost no extra draw call.
        // Invisible: the space between the top bar and the settings button, which the camera fits the
        // board into. Moving this moves the board.
        private static RectTransform BuildBoardArea(Transform safe)
        {
            RectTransform area = UiBuild.Rect("BoardArea", safe);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(20f, 230f);     // above the settings button
            area.offsetMax = new Vector2(-20f, -310f);   // below the top bar
            return area;
        }

        private static void BuildBackdrop(RectTransform boardArea)
        {
            var boardView = Object.FindFirstObjectByType<BoardView>();
            Transform game = boardView.transform;

            Replace(game, "Backdrop");
            Replace(game, "BoardFrame");

            var backdrop = new GameObject("Backdrop", typeof(SpriteRenderer));
            backdrop.transform.SetParent(game, false);

            // Wide and tall enough for the widest framing any allowed board can ask for; the sprite
            // is a plain vertical strip, so overshooting costs nothing.
            backdrop.transform.localScale = new Vector3(20f, 1f, 1f);

            var backdropRenderer = backdrop.GetComponent<SpriteRenderer>();
            backdropRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Board/Backdrop.png");
            backdropRenderer.sortingOrder = -20;

            var frame = new GameObject("BoardFrame", typeof(SpriteRenderer));
            frame.transform.SetParent(game, false);

            var frameRenderer = frame.GetComponent<SpriteRenderer>();
            frameRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Board/Frame.png");
            frameRenderer.drawMode = SpriteDrawMode.Sliced;      // corners keep their radius at any size
            frameRenderer.color = Well;
            frameRenderer.sortingOrder = -10;

            // Blocks are drawn only inside this; the square is scaled to the board by BoardView.
            Replace(game, "BoardMask");
            var maskObject = new GameObject("BoardMask", typeof(SpriteMask));
            maskObject.transform.SetParent(game, false);
            var mask = maskObject.GetComponent<SpriteMask>();
            mask.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Board/BoardMask.png");

            // The ornate frame over the board's edge, above the blocks, hiding the line they appear from.
            Replace(game, "BoardTrim");
            var trim = new GameObject("BoardTrim", typeof(SpriteRenderer));
            trim.transform.SetParent(game, false);
            var trimRenderer = trim.GetComponent<SpriteRenderer>();
            trimRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Board/board_frame.png");
            trimRenderer.drawMode = SpriteDrawMode.Sliced;
            trimRenderer.sortingOrder = 5;

            BuildLevelBackground();

            var so = new SerializedObject(boardView);
            so.FindProperty("boardFrame").objectReferenceValue = frameRenderer;
            so.FindProperty("boardMask").objectReferenceValue = mask;
            so.FindProperty("boardTrim").objectReferenceValue = trimRenderer;
            so.FindProperty("boardArea").objectReferenceValue = boardArea;
            so.FindProperty("cellTile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Board/Cell.png");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // The chapter's picture behind everything, on the camera so a shake leaves it still. Dimmed a
        // little: it is scenery, and the board in front of it is what has to read.
        private static void BuildLevelBackground()
        {
            Camera camera = Camera.main;
            Replace(camera.transform, "LevelBackground");

            var go = new GameObject("LevelBackground", typeof(SpriteRenderer));
            go.transform.SetParent(camera.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 20f);   // in front of the camera, behind the board

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = -15;   // over the plain backdrop, under the board's well
            renderer.color = new Color(0.82f, 0.82f, 0.88f, 1f);

            var background = go.AddComponent<LevelBackground>();
            var so = new SerializedObject(background);
            so.FindProperty("controller").objectReferenceValue = Object.FindFirstObjectByType<GameController>();
            so.FindProperty("viewCamera").objectReferenceValue = camera;

            string[] chapters =
            {
                "Assets/Art/Backgrounds/bg_level_vault.jpg",
                "Assets/Art/Backgrounds/bg_level_museum.jpg",
                "Assets/Art/Backgrounds/bg_level_rooftop.jpg",
            };

            SerializedProperty list = so.FindProperty("chapters");
            list.arraySize = chapters.Length;
            for (int i = 0; i < chapters.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(chapters[i]);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // A null parent means a root object of the open scene.
        private static void Replace(Transform parent, string name)
        {
            if (parent != null)
            {
                Transform existing = parent.Find(name);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
                return;
            }

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == name) Object.DestroyImmediate(root);
        }
    }
}
#endif

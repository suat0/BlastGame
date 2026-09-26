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
    // Builds the home scene from nothing and saves it, laid out after Match Villains' home screen:
    // a top bar (profile, coins, lives, settings), event icons down the sides, the level button above
    // a five-tab bar with Home raised in the middle.
    //
    // A scaffold, like the other builders: flat shapes and short words stand in for icons until the
    // generated art arrives, and once the layout settles the scene becomes the source of truth and
    // this is retired.
    public static class HomeBuilder
    {
        public const string ScenePath = "Assets/Scenes/Home.unity";

        private const string CatalogPath = "Assets/Levels/LevelCatalog.asset";
        private const string RequestPath = "Assets/Levels/LevelRequest.asset";

        private static readonly Color Sky = UiBuild.Hex("#2A1B5C");
        private static readonly Color SkyLow = UiBuild.Hex("#43299A");
        private static readonly Color Ink = Color.white;
        private static readonly Color Gold = UiBuild.Hex("#FFD84D");
        private static readonly Color Pill = UiBuild.Hex("#2E1F6B");
        private static readonly Color PillEdge = UiBuild.Hex("#1A1142");
        private static readonly Color Purple = UiBuild.Hex("#7A5AD6");
        private static readonly Color PurpleShade = UiBuild.Hex("#3E2A8A");
        private static readonly Color NavFill = UiBuild.Hex("#4B3494");
        private static readonly Color NavRaised = UiBuild.Hex("#8A6AE6");
        private static readonly Color GreenFace = UiBuild.Hex("#5BC236");
        private static readonly Color GreenShade = UiBuild.Hex("#2E7A1B");
        private static readonly Color Red = UiBuild.Hex("#E5483B");
        private static readonly Color HeartRed = UiBuild.Hex("#F0445A");
        private static readonly Color Lock = UiBuild.Hex("#E3A92B");
        private static readonly Color ToastFill = new Color(0.08f, 0.04f, 0.18f, 0.9f);

        private static Toast toast;

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

            BuildBackdrop(root);

            // Built before the buttons that point at it.
            toast = BuildToast(root);

            BuildTitle(root);
            CoinCounter coins = BuildTopBar(root, out Button settings);
            BuildEvents(root);
            Button levelButton = BuildLevelButton(root, out GameObject hardTag);
            BuildNavBar(root);

            Canvas popups = UiBuild.CreateCanvas("Popups", null, 10);
            var start = Instantiate<LevelStartPopup>(PopupBuilder.LevelStartPath, popups.transform);
            var settingsPopup = Instantiate<SettingsPopup>(PopupBuilder.SettingsPath, popups.transform);
            var confirm = Instantiate<ConfirmPopup>(PopupBuilder.ConfirmPath, popups.transform);

            // Above the popups, so a locked button's note is never hidden behind one.
            toast.transform.SetParent(popups.transform, true);

            var view = canvas.gameObject.AddComponent<HomeView>();
            var so = new SerializedObject(view);
            so.FindProperty("catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            so.FindProperty("request").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelRequest>(RequestPath);
            so.FindProperty("levelButton").objectReferenceValue = levelButton;
            so.FindProperty("levelLabel").objectReferenceValue = levelButton.GetComponentInChildren<TMP_Text>();
            so.FindProperty("hardTag").objectReferenceValue = hardTag;
            so.FindProperty("startPopup").objectReferenceValue = start;
            so.FindProperty("coins").objectReferenceValue = coins;
            so.FindProperty("settingsButton").objectReferenceValue = settings;
            so.FindProperty("settingsPopup").objectReferenceValue = settingsPopup;
            so.FindProperty("confirmPopup").objectReferenceValue = confirm;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Home scene built.");
        }

        // Two washes, darker above: a stand-in for the mansion hall until the generated background.
        private static void BuildBackdrop(Transform root)
        {
            UiBuild.Fill("Backdrop", root, Sky);

            Image low = UiBuild.Fill("Floor", root, SkyLow);
            RectTransform lowRect = low.rectTransform;
            lowRect.anchorMin = new Vector2(0f, 0f);
            lowRect.anchorMax = new Vector2(1f, 0.45f);
            lowRect.offsetMin = lowRect.offsetMax = Vector2.zero;
        }

        private static void BuildTitle(Transform root)
        {
            TMP_Text title = UiBuild.Text("Logo", root, "BLAST\nVILLAINS", 150f, Gold, TextAlignmentOptions.Center);
            title.lineSpacing = -30f;
            RectTransform rect = title.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.64f);
            rect.sizeDelta = new Vector2(900f, 420f);
        }

        // Profile, coins, lives, settings - left to right, as in Match Villains. The round icons sit
        // across their pill's left end, so each pill starts far enough right to keep them off its
        // neighbour.
        private static CoinCounter BuildTopBar(Transform root, out Button settings)
        {
            RectTransform bar = UiBuild.Rect("TopBar", root);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 150f);
            bar.anchoredPosition = new Vector2(0f, -40f);

            Button profile = Square(bar, "Profile", "?", new Vector2(0f, 0.5f), new Vector2(110f, 0f), 130f, Purple, PurpleShade);
            Locked(profile, "Profile", 10);

            // Coins: a real count, and a plus that would open the shop.
            RectTransform coinPill = PillAt(bar, "Coins", 200f, 300f);
            RectTransform coinIcon = Disc(coinPill, "Icon", Gold, 100f, new Vector2(30f, 0f), new Vector2(0f, 0.5f));
            TMP_Text coinLabel = UiBuild.Text("Count", coinPill, "0", 60f, Ink, TextAlignmentOptions.Center);
            Between(coinLabel.rectTransform);
            Button coinPlus = PlusButton(coinPill);
            Locked(coinPlus, "Shop", 15);

            // Lives: a mock, always full. Nothing in this build can run out of them.
            RectTransform lifePill = PillAt(bar, "Lives", 550f, 250f);
            RectTransform heart = Disc(lifePill, "Icon", HeartRed, 100f, new Vector2(30f, 0f), new Vector2(0f, 0.5f));
            TMP_Text hearts = UiBuild.Text("Count", heart, "5", 56f, Ink, TextAlignmentOptions.Center);
            UiBuild.Stretch(hearts.rectTransform, 0f, 0f);
            TMP_Text full = UiBuild.Text("Status", lifePill, "Full", 52f, Ink, TextAlignmentOptions.Center);
            Between(full.rectTransform);
            Button lifePlus = PlusButton(lifePill);
            Locked(lifePlus, "Lives shop", 15);

            settings = Square(bar, "Settings", "=", new Vector2(1f, 0.5f), new Vector2(-100f, 0f), 120f, Purple, PurpleShade);

            // Flyers for the post-win coin flight, hidden until then.
            var flyers = new RectTransform[8];
            for (int i = 0; i < flyers.Length; i++)
                flyers[i] = Disc(root, "CoinFlyer", Gold, 80f, Vector2.zero, new Vector2(0.5f, 0.5f));

            var counter = coinPill.gameObject.AddComponent<CoinCounter>();
            var so = new SerializedObject(counter);
            so.FindProperty("label").objectReferenceValue = coinLabel;
            so.FindProperty("icon").objectReferenceValue = coinIcon;
            SerializedProperty list = so.FindProperty("flyers");
            list.arraySize = flyers.Length;
            for (int i = 0; i < flyers.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = flyers[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            return counter;
        }

        // Three of Match Villains' events, down the sides, each with a live countdown.
        private static void BuildEvents(Transform root)
        {
            Event(root, "BankRob", "Bank\nRob", new Vector2(0f, 1f), new Vector2(120f, -330f), 25, 72f, 0f);
            Event(root, "BombHill", "Bomb\nHill", new Vector2(0f, 1f), new Vector2(120f, -560f), 30, 48f, 17f);
            Event(root, "Thunder", "Thun\nder", new Vector2(1f, 1f), new Vector2(-120f, -330f), 18, 96f, 41f);
        }

        private static void Event(Transform root, string name, string glyph, Vector2 anchor, Vector2 position,
                                  int unlockLevel, float periodHours, float offsetHours)
        {
            Button icon = Square(root, name, glyph, anchor, position, 160f, Purple, PurpleShade);
            icon.GetComponentInChildren<TMP_Text>().fontSize = 40f;
            Locked(icon, name == "BankRob" ? "Bank Rob" : name == "BombHill" ? "Bomb Hill" : "Thunder", unlockLevel);

            RectTransform timerPill = UiBuild.Panel("TimerPill", icon.transform, Pill, 0f, 0f);
            timerPill.anchorMin = timerPill.anchorMax = new Vector2(0.5f, 0f);
            timerPill.sizeDelta = new Vector2(170f, 56f);
            timerPill.anchoredPosition = new Vector2(0f, -30f);

            TMP_Text label = UiBuild.Text("Time", timerPill, "1d 16h", 38f, Ink, TextAlignmentOptions.Center);
            UiBuild.Stretch(label.rectTransform, 0f, 0f);

            var timer = icon.gameObject.AddComponent<EventTimer>();
            var so = new SerializedObject(timer);
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("periodHours").floatValue = periodHours;
            so.FindProperty("offsetHours").floatValue = offsetHours;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button BuildLevelButton(Transform root, out GameObject hardTag)
        {
            Button button = UiBuild.CreateButton("LevelButton", root, new Vector2(640f, 210f), Vector2.zero,
                                                 GreenFace, GreenShade, "Level 1", 110f, Ink);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 420f);

            RectTransform tag = UiBuild.Panel("HardTag", rect, Red, 0f, 0f);
            tag.anchorMin = tag.anchorMax = new Vector2(0.5f, 1f);
            tag.sizeDelta = new Vector2(240f, 70f);
            tag.anchoredPosition = new Vector2(0f, 20f);
            TMP_Text tagLabel = UiBuild.Text("Label", tag, "HARD", 48f, Ink, TextAlignmentOptions.Center);
            UiBuild.Stretch(tagLabel.rectTransform, 0f, 0f);

            hardTag = tag.gameObject;
            hardTag.SetActive(false);
            return button;
        }

        // Album, Leaderboard, Home, Team, Shop - Match Villains' order, Home raised in the middle.
        private static void BuildNavBar(Transform root)
        {
            RectTransform bar = UiBuild.Rect("NavBar", root);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 200f);
            bar.anchoredPosition = Vector2.zero;

            UiBuild.Fill("Fill", bar, NavFill);

            string[] names = { "Album", "Ranks", "Home", "Team", "Shop" };
            int[] unlocks = { 8, 12, 0, 20, 15 };

            for (int i = 0; i < names.Length; i++)
            {
                bool home = i == 2;

                RectTransform slot = UiBuild.Rect(names[i], bar);
                slot.anchorMin = new Vector2(i / 5f, 0f);
                slot.anchorMax = new Vector2((i + 1) / 5f, 1f);
                slot.offsetMin = new Vector2(6f, home ? 0f : 10f);
                slot.offsetMax = new Vector2(-6f, home ? 50f : -10f);    // Home stands proud of the bar

                RectTransform face = UiBuild.Panel("Face", slot, home ? NavRaised : Purple, 0f, 0f);
                TMP_Text label = UiBuild.Text("Label", face, names[i], home ? 52f : 42f, Ink, TextAlignmentOptions.Center);
                UiBuild.Stretch(label.rectTransform, 0f, 0f);

                if (home) continue;   // already here

                var image = face.GetComponent<Image>();
                image.raycastTarget = true;
                var button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = image;

                RectTransform lockBadge = Disc(face, "Lock", Lock, 54f, new Vector2(-10f, -10f), new Vector2(1f, 1f));
                TMP_Text lockGlyph = UiBuild.Text("Glyph", lockBadge, "L", 34f, Ink, TextAlignmentOptions.Center);
                UiBuild.Stretch(lockGlyph.rectTransform, 0f, 0f);

                Locked(button, names[i] == "Ranks" ? "Leaderboard" : names[i], unlocks[i]);
            }
        }

        private static Toast BuildToast(Transform root)
        {
            RectTransform area = UiBuild.Rect("Toast", root);
            UiBuild.Stretch(area, 0f, 0f);

            RectTransform body = UiBuild.Panel("Body", area, ToastFill, 0f, 0f);
            body.anchorMin = body.anchorMax = new Vector2(0.5f, 0.36f);
            body.sizeDelta = new Vector2(860f, 130f);
            body.anchoredPosition = Vector2.zero;

            TMP_Text label = UiBuild.Text("Label", body, "Unlocks at Level 10", 54f, Ink, TextAlignmentOptions.Center);
            UiBuild.Stretch(label.rectTransform, 0f, 0f);

            var group = area.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var created = area.gameObject.AddComponent<Toast>();
            var so = new SerializedObject(created);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            return created;
        }

        // --- small parts ----------------------------------------------------------------------

        private static Button Square(Transform parent, string name, string glyph, Vector2 anchor, Vector2 position,
                                     float size, Color face, Color shade)
        {
            Button button = UiBuild.CreateButton(name, parent, new Vector2(size, size), Vector2.zero,
                                                 face, shade, glyph, size * 0.45f, Ink);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            return button;
        }

        private static RectTransform PillAt(RectTransform bar, string name, float left, float width)
        {
            RectTransform pill = UiBuild.Rect(name, bar);
            pill.anchorMin = pill.anchorMax = new Vector2(0f, 0.5f);
            pill.pivot = new Vector2(0f, 0.5f);
            pill.sizeDelta = new Vector2(width, 96f);
            pill.anchoredPosition = new Vector2(left, 0f);

            UiBuild.Panel("Edge", pill, PillEdge, 0f, -6f);
            UiBuild.Panel("Fill", pill, Pill, 0f, 0f);
            return pill;
        }

        private static RectTransform Disc(Transform parent, string name, Color color, float size, Vector2 position, Vector2 anchor)
        {
            RectTransform disc = UiBuild.Panel(name, parent, color, 0f, 0f);
            disc.anchorMin = disc.anchorMax = anchor;
            disc.sizeDelta = new Vector2(size, size);
            disc.anchoredPosition = position;
            disc.GetComponent<Image>().pixelsPerUnitMultiplier = 0.1f;   // a tight radius reads as round
            return disc;
        }

        // The stretch of a pill between its round icon on the left and its plus on the right.
        private static void Between(RectTransform label)
        {
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.offsetMin = new Vector2(80f, 0f);
            label.offsetMax = new Vector2(-60f, -2f);
        }

        private static Button PlusButton(RectTransform pill)
        {
            Button plus = UiBuild.CreateButton("Plus", pill, new Vector2(70f, 70f), Vector2.zero,
                                               GreenFace, GreenShade, "+", 60f, Ink);
            var rect = (RectTransform)plus.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-10f, 0f);
            return plus;
        }

        private static void Locked(Button button, string feature, int level)
        {
            var locked = button.gameObject.AddComponent<LockedFeature>();
            var so = new SerializedObject(locked);
            so.FindProperty("featureName").stringValue = feature;
            so.FindProperty("unlockLevel").intValue = level;
            so.FindProperty("toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T Instantiate<T>(string prefabPath, Transform parent) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new System.IO.FileNotFoundException(prefabPath);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return instance.GetComponent<T>();
        }
    }
}
#endif

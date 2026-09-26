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
    // Builds the home scene from nothing and saves it, laid out after Match Villains' home screen: the
    // mansion hall behind everything, the family standing in it, a top bar (profile, coins, lives,
    // settings), events down the sides, the level button above a five-tab bar with Home raised in the
    // middle.
    //
    // A scaffold, like the other builders: once the layout settles the scene becomes the source of
    // truth and this is retired.
    public static class HomeBuilder
    {
        public const string ScenePath = "Assets/Scenes/Home.unity";

        private const string CatalogPath = "Assets/Levels/LevelCatalog.asset";
        private const string RequestPath = "Assets/Levels/LevelRequest.asset";
        private const string BackgroundPath = "Assets/Art/Backgrounds/bg_home.jpg";

        private static readonly Color Sky = UiBuild.Hex("#2A1B5C");
        private static readonly Color Ink = Color.white;

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

            BuildBackground(root);

            // The background above runs to the screen edges; everything after sits in the safe area.
            RectTransform safe = UiBuild.Rect("Safe", root);
            UiBuild.Stretch(safe, 0f, 0f);
            safe.gameObject.AddComponent<SafeArea>();

            // Built before the buttons that point at it.
            toast = BuildToast(root);

            BuildFamily(safe);
            BuildLogo(safe);
            CoinCounter coins = BuildTopBar(safe, root, out Button settings);
            BuildEvents(safe);
            Button levelButton = BuildLevelButton(safe, out GameObject hardTag);
            BuildNavBar(safe);

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

        // The mansion hall, covering the screen at its own aspect: cropped at the sides on a tall phone,
        // at the top and bottom on a tablet, never stretched.
        private static void BuildBackground(Transform root)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);

            Image image = UiBuild.Picture("Background", root, sprite, Vector2.zero);
            image.preserveAspect = false;
            UiBuild.Stretch(image.rectTransform, 0f, 0f);

            var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        // The family standing on the hall's floor, feet on a line above the level button: the Daughter
        // on the left, the Count in the middle and a step forward, the Butler on the right.
        private static void BuildFamily(Transform safe)
        {
            Stand(safe, "Daughter", "char_daughter", new Vector2(360f, 470f), -290f, 640f);
            Stand(safe, "Butler", "char_butler", new Vector2(440f, 600f), 290f, 640f);
            Stand(safe, "Count", "char_count", new Vector2(360f, 640f), 0f, 610f);
        }

        private static void Stand(Transform parent, string name, string sprite, Vector2 size, float x, float feet)
        {
            Image figure = UiBuild.Picture(name, parent, UiBuild.Art(sprite), size);
            figure.rectTransform.pivot = new Vector2(0.5f, 0f);
            UiBuild.Place(figure.rectTransform, new Vector2(0.5f, 0f), new Vector2(x, feet));
        }

        private static void BuildLogo(Transform safe)
        {
            Image logo = UiBuild.Picture("Logo", safe, UiBuild.Art("logo"), new Vector2(560f, 300f));
            UiBuild.Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -330f));
        }

        // Profile, coins, lives, settings - left to right, as in Match Villains.
        private static CoinCounter BuildTopBar(Transform safe, Transform canvasRoot, out Button settings)
        {
            RectTransform bar = UiBuild.Rect("TopBar", safe);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 150f);
            bar.anchoredPosition = new Vector2(0f, -30f);

            Button profile = UiBuild.ArtButton("Profile", bar, UiBuild.Art("icon_profile"), new Vector2(130f, 130f), null, 0f);
            UiBuild.Place((RectTransform)profile.transform, new Vector2(0f, 0.5f), new Vector2(100f, 0f));
            Locked(profile, "Profile", 10);

            // Coins: a real count, and a plus that would open the shop.
            RectTransform coinPill = Pill(bar, "Coins", 200f, 300f, "icon_coin", out TMP_Text coinLabel, out RectTransform coinIcon);
            Locked(PlusButton(coinPill), "Shop", 15);

            // Lives: a mock, always full. Nothing in this build can run out of them.
            RectTransform lifePill = Pill(bar, "Lives", 575f, 245f, "icon_heart", out TMP_Text full, out RectTransform heart);
            full.text = "Full";
            TMP_Text hearts = UiBuild.Text("Count", heart, "5", 52f, Ink, TextAlignmentOptions.Center);
            hearts.fontSharedMaterial = UiBuild.Outline;
            UiBuild.Stretch(hearts.rectTransform, 0f, 4f);
            Locked(PlusButton(lifePill), "Lives shop", 15);

            settings = UiBuild.ArtButton("Settings", bar, UiBuild.Art("icon_gear"), new Vector2(125f, 125f), null, 0f);
            UiBuild.Place((RectTransform)settings.transform, new Vector2(1f, 0.5f), new Vector2(-95f, 0f));

            // Coins for the post-win flight, hidden until then, on the canvas root so they can cross
            // the whole screen.
            var flyers = new RectTransform[8];
            for (int i = 0; i < flyers.Length; i++)
                flyers[i] = UiBuild.Picture("CoinFlyer", canvasRoot, UiBuild.Art("icon_coin"), new Vector2(90f, 90f)).rectTransform;

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

        // Three of Match Villains' events down the sides, each with a live countdown.
        private static void BuildEvents(Transform safe)
        {
            Event(safe, "BankRob", "event_bankrob", "Bank Rob", new Vector2(0f, 1f), new Vector2(110f, -320f), 25, 72f, 0f);
            Event(safe, "BombHill", "event_bombhill", "Bomb Hill", new Vector2(0f, 1f), new Vector2(110f, -560f), 30, 48f, 17f);
            Event(safe, "Thunder", "event_thunder", "Thunder", new Vector2(1f, 1f), new Vector2(-110f, -320f), 18, 96f, 41f);
        }

        private static void Event(Transform safe, string name, string sprite, string feature, Vector2 anchor, Vector2 position,
                                  int unlockLevel, float periodHours, float offsetHours)
        {
            Button icon = UiBuild.ArtButton(name, safe, UiBuild.Art(sprite), new Vector2(160f, 160f), null, 0f);
            UiBuild.Place((RectTransform)icon.transform, anchor, position);
            Locked(icon, feature, unlockLevel);

            Image timerPill = UiBuild.Picture("TimerPill", icon.transform, UiBuild.Art("pill_dark"), new Vector2(180f, 60f));
            UiBuild.Place(timerPill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -22f));

            TMP_Text label = UiBuild.Text("Time", timerPill.transform, "1d 16h", 38f, Ink, TextAlignmentOptions.Center);
            label.fontSharedMaterial = UiBuild.Outline;
            UiBuild.Stretch(label.rectTransform, 0f, 2f);

            var timer = icon.gameObject.AddComponent<EventTimer>();
            var so = new SerializedObject(timer);
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("periodHours").floatValue = periodHours;
            so.FindProperty("offsetHours").floatValue = offsetHours;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button BuildLevelButton(Transform safe, out GameObject hardTag)
        {
            Button button = UiBuild.ArtButton("LevelButton", safe, UiBuild.Art("btn_green"), new Vector2(640f, 220f), "Level 1", 110f);
            UiBuild.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0f, 400f));

            Sprite ribbon = UiBuild.Art("ribbon_red");
            Image tag = UiBuild.Picture("HardTag", button.transform, ribbon, new Vector2(110f * ribbon.rect.width / ribbon.rect.height, 110f));
            UiBuild.Place(tag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 20f));
            TMP_Text tagLabel = UiBuild.Text("Label", tag.transform, "HARD", 46f, Ink, TextAlignmentOptions.Center);
            tagLabel.fontSharedMaterial = UiBuild.Outline;
            UiBuild.Stretch(tagLabel.rectTransform, 0f, 8f);

            hardTag = tag.gameObject;
            hardTag.SetActive(false);
            return button;
        }

        // Album, Leaderboard, Home, Team, Shop - Match Villains' order, Home raised in the middle.
        private static void BuildNavBar(Transform safe)
        {
            RectTransform bar = UiBuild.Rect("NavBar", safe);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 210f);
            bar.anchoredPosition = Vector2.zero;

            Image fill = UiBuild.Picture("Fill", bar, UiBuild.Art("panel_purple"), Vector2.zero);
            UiBuild.Stretch(fill.rectTransform, 0f, 0f);
            fill.rectTransform.offsetMin = new Vector2(-20f, -40f);   // past the screen edges: no rim at the sides or bottom
            fill.rectTransform.offsetMax = new Vector2(20f, 0f);

            string[] names = { "Album", "Ranks", "Home", "Team", "Shop" };
            string[] icons = { "icon_album", "icon_leaderboard", "icon_home", "icon_team", "icon_shop" };
            string[] features = { "Album", "Leaderboard", null, "Team", "Shop" };
            int[] unlocks = { 8, 12, 0, 20, 15 };

            for (int i = 0; i < names.Length; i++)
            {
                bool home = i == 2;

                RectTransform slot = UiBuild.Rect(names[i], bar);
                slot.anchorMin = new Vector2(i / 5f, 0f);
                slot.anchorMax = new Vector2((i + 1) / 5f, 1f);
                slot.offsetMin = Vector2.zero;
                slot.offsetMax = Vector2.zero;

                if (home)
                {
                    // Standing proud of the bar on a panel of its own, labelled, as the current tab.
                    Image raised = UiBuild.Picture("Raised", slot, UiBuild.Art("panel_purple"), new Vector2(210f, 250f));
                    UiBuild.Place(raised.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 125f));
                    Image homeIcon = UiBuild.Picture("Icon", raised.transform, UiBuild.Art(icons[i]), new Vector2(160f, 160f));
                    UiBuild.Place(homeIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 25f));

                    TMP_Text label = UiBuild.Text("Label", raised.transform, "Home", 44f, Ink, TextAlignmentOptions.Center);
                    label.fontSharedMaterial = UiBuild.Outline;
                    label.rectTransform.sizeDelta = new Vector2(200f, 60f);
                    UiBuild.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f));
                    continue;
                }

                Image icon = UiBuild.Picture("Icon", slot, UiBuild.Art(icons[i]), new Vector2(140f, 140f));
                UiBuild.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero);
                icon.raycastTarget = true;

                Image padlock = UiBuild.Picture("Lock", icon.transform, UiBuild.Art("icon_lock"), new Vector2(60f, 60f));
                UiBuild.Place(padlock.rectTransform, new Vector2(1f, 1f), new Vector2(-8f, -8f));

                // On the slot, which keeps the tab's name; the icon is what takes the tap.
                var button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = icon;
                Locked(button, features[i], unlocks[i]);
            }
        }

        private static Toast BuildToast(Transform root)
        {
            RectTransform area = UiBuild.Rect("Toast", root);
            UiBuild.Stretch(area, 0f, 0f);

            Image body = UiBuild.Picture("Body", area, UiBuild.Art("pill_dark"), new Vector2(880f, 140f));
            UiBuild.Place(body.rectTransform, new Vector2(0.5f, 0.36f), Vector2.zero);

            TMP_Text label = UiBuild.Text("Label", body.transform, "Unlocks at Level 10", 52f, Ink, TextAlignmentOptions.Center);
            label.fontSharedMaterial = UiBuild.Outline;
            UiBuild.Stretch(label.rectTransform, 0f, 2f);

            var group = area.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var created = area.gameObject.AddComponent<Toast>();
            var so = new SerializedObject(created);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("body").objectReferenceValue = body.rectTransform;
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            return created;
        }

        // --- small parts ----------------------------------------------------------------------

        // A dark pill with an icon across its left end and a label in the rest.
        private static RectTransform Pill(RectTransform bar, string name, float left, float width, string iconName,
                                          out TMP_Text label, out RectTransform icon)
        {
            Image pill = UiBuild.Picture(name, bar, UiBuild.Art("pill_dark"), new Vector2(width, 96f));
            RectTransform rect = pill.rectTransform;
            rect.pivot = new Vector2(0f, 0.5f);
            UiBuild.Place(rect, new Vector2(0f, 0.5f), new Vector2(left, 0f));

            icon = UiBuild.Picture("Icon", rect, UiBuild.Art(iconName), new Vector2(110f, 110f)).rectTransform;
            UiBuild.Place(icon, new Vector2(0f, 0.5f), new Vector2(20f, 0f));

            label = UiBuild.Text("Count", rect, "0", 56f, Ink, TextAlignmentOptions.Center);
            label.fontSharedMaterial = UiBuild.Outline;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = 36f;
            label.fontSizeMax = 56f;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(80f, 0f);
            label.rectTransform.offsetMax = new Vector2(-60f, -2f);

            return rect;
        }

        private static Button PlusButton(RectTransform pill)
        {
            Button plus = UiBuild.ArtButton("Plus", pill, UiBuild.Art("icon_plus"), new Vector2(76f, 76f), null, 0f);
            UiBuild.Place((RectTransform)plus.transform, new Vector2(1f, 0.5f), new Vector2(-6f, 0f));
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

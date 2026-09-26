#if UNITY_EDITOR
using BlastGame.Game.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.EditorTools
{
    // Builds the popups as prefabs, so the start popup can sit in both scenes and every popup
    // has one definition. A scaffold like the others: flat colour panels until the generated art
    // replaces them, then retired.
    public static class PopupBuilder
    {
        public const string Folder = "Assets/Prefabs/UI";

        public const string LevelStartPath = Folder + "/Popup_LevelStart.prefab";
        public const string PausePath = Folder + "/Popup_Pause.prefab";
        public const string SettingsPath = Folder + "/Popup_Settings.prefab";
        public const string ConfirmPath = Folder + "/Popup_Confirm.prefab";
        public const string WinPath = Folder + "/Popup_Win.prefab";
        public const string LosePath = Folder + "/Popup_Lose.prefab";

        private const string BoxSpritePath = "Assets/Art/Box0.png";

        // Match Villains' popup palette: a purple card edged in gold, white headings, green to go on,
        // red to leave.
        private static readonly Color Dim = new Color(0.06f, 0.02f, 0.14f, 0.78f);
        private static readonly Color CardEdge = UiBuild.Hex("#E3A92B");
        private static readonly Color CardFill = UiBuild.Hex("#5B3FB8");
        private static readonly Color CardInset = UiBuild.Hex("#4A2F9E");
        private static readonly Color Heading = Color.white;
        private static readonly Color Body = UiBuild.Hex("#E6DDFF");
        private static readonly Color Gold = UiBuild.Hex("#FFD84D");
        private static readonly Color GreenFace = UiBuild.Hex("#5BC236");
        private static readonly Color GreenShade = UiBuild.Hex("#2E7A1B");
        private static readonly Color RedFace = UiBuild.Hex("#E5483B");
        private static readonly Color RedShade = UiBuild.Hex("#A3261C");
        private static readonly Color PurpleFace = UiBuild.Hex("#8A6AE6");
        private static readonly Color PurpleShade = UiBuild.Hex("#4B3494");

        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

            BuildLevelStart();
            // The same settings popup twice, differing in the one way out: a level leaves, home resets.
            BuildSettings(PausePath, "Popup_Pause", "Leave level");
            BuildSettings(SettingsPath, "Popup_Settings", "Reset progress");
            BuildConfirm();
            BuildWin();
            BuildLose();

            AssetDatabase.SaveAssets();
            Debug.Log("Popup prefabs written.");
        }

        private static void BuildLevelStart()
        {
            GameObject root = Shell<LevelStartPopup>("Popup_LevelStart", new Vector2(860f, 900f), out RectTransform card);

            TMP_Text title = Heading1(card, "Level 1", -70f);

            RectTransform ribbon = UiBuild.Panel("HardRibbon", card, RedFace, 0f, 0f);
            Place(ribbon, new Vector2(0.5f, 1f), new Vector2(420f, 90f), new Vector2(0f, -210f));
            TMP_Text ribbonText = UiBuild.Text("Label", ribbon, "HARD LEVEL", 52f, Heading, TextAlignmentOptions.Center);
            UiBuild.Stretch(ribbonText.rectTransform, 0f, 0f);

            RectTransform goal = UiBuild.Rect("Goal", card);
            Place(goal, new Vector2(0.5f, 0.5f), new Vector2(500f, 180f), new Vector2(0f, 40f));

            RectTransform goalWell = UiBuild.Panel("Well", goal, CardInset, 0f, 0f);
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)icon.transform;
            iconRect.SetParent(goalWell, false);
            Place(iconRect, new Vector2(0.5f, 0.5f), new Vector2(130f, 130f), new Vector2(-80f, 0f));
            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BoxSpritePath);
            UiBuild.MakeNonInteractive(iconImage);

            TMP_Text goalCount = UiBuild.Text("Count", goalWell, "8", 90f, Heading, TextAlignmentOptions.Left);
            Place(goalCount.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(200f, 130f), new Vector2(110f, -4f));

            TMP_Text moves = UiBuild.Text("Moves", card, "20 moves", 56f, Body, TextAlignmentOptions.Center);
            Place(moves.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(700f, 80f), new Vector2(0f, -110f));

            Button play = UiBuild.CreateButton("PlayButton", card, new Vector2(560f, 190f), Vector2.zero,
                                               GreenFace, GreenShade, "Play", 100f, Heading);
            Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(560f, 190f), new Vector2(0f, 150f));

            Button close = CloseButton(card);

            Wire(root, ("titleLabel", title), ("hardRibbon", ribbon.gameObject), ("goalGroup", goal.gameObject),
                 ("goalLabel", goalCount), ("movesLabel", moves), ("playButton", play), ("closeButton", close));

            ribbon.gameObject.SetActive(false);
            Save(root, LevelStartPath);
        }

        private static void BuildSettings(string path, string name, string action)
        {
            GameObject root = Shell<SettingsPopup>(name, new Vector2(820f, 860f), out RectTransform card);

            Heading1(card, "Settings", -70f);

            Button music = Wide(card, "MusicButton", "Music: On", PurpleFace, PurpleShade, 170f);
            Button sfx = Wide(card, "SfxButton", "Sound: On", PurpleFace, PurpleShade, -20f);
            Button actionButton = Wide(card, "ActionButton", action, RedFace, RedShade, -260f);
            Button close = CloseButton(card);

            Wire(root, ("closeButton", close), ("actionButton", actionButton), ("musicButton", music), ("sfxButton", sfx),
                 ("musicLabel", music.GetComponentInChildren<TMP_Text>()),
                 ("sfxLabel", sfx.GetComponentInChildren<TMP_Text>()));

            Save(root, path);
        }

        private static void BuildConfirm()
        {
            GameObject root = Shell<ConfirmPopup>("Popup_Confirm", new Vector2(820f, 620f), out RectTransform card);

            TMP_Text title = Heading1(card, "Are you sure?", -70f);

            TMP_Text body = UiBuild.Text("Body", card, "This cannot be undone.", 52f, Body,
                                         TextAlignmentOptions.Center);
            Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(700f, 150f), new Vector2(0f, 30f));

            Button confirm = UiBuild.CreateButton("ConfirmButton", card, new Vector2(330f, 160f), Vector2.zero,
                                                  RedFace, RedShade, "Yes", 72f, Heading);
            Place((RectTransform)confirm.transform, new Vector2(0.5f, 0f), new Vector2(330f, 160f), new Vector2(-185f, 130f));

            // Cancel on the right and in green: the safe choice is the one the thumb finds first.
            Button cancel = UiBuild.CreateButton("CancelButton", card, new Vector2(330f, 160f), Vector2.zero,
                                                 GreenFace, GreenShade, "Stay", 72f, Heading);
            Place((RectTransform)cancel.transform, new Vector2(0.5f, 0f), new Vector2(330f, 160f), new Vector2(185f, 130f));

            Wire(root, ("titleLabel", title), ("bodyLabel", body),
                 ("confirmLabel", confirm.GetComponentInChildren<TMP_Text>()),
                 ("confirmButton", confirm), ("cancelButton", cancel));
            Save(root, ConfirmPath);
        }

        private static void BuildWin()
        {
            GameObject root = Shell<WinPopup>("Popup_Win", new Vector2(860f, 820f), out RectTransform card);

            TMP_Text heading = Heading1(card, "Level Completed!", -70f);
            heading.color = Gold;

            TMP_Text score = UiBuild.Text("Score", card, "Score 0", 64f, Body, TextAlignmentOptions.Center);
            Place(score.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(700f, 90f), new Vector2(0f, 100f));

            RectTransform coinWell = UiBuild.Panel("CoinWell", card, CardInset, 0f, 0f);
            Place(coinWell, new Vector2(0.5f, 0.5f), new Vector2(420f, 150f), new Vector2(0f, -40f));
            TMP_Text coins = UiBuild.Text("Coins", coinWell, "+0", 96f, Gold, TextAlignmentOptions.Center);
            UiBuild.Stretch(coins.rectTransform, 0f, 0f);

            Button next = UiBuild.CreateButton("ContinueButton", card, new Vector2(560f, 180f), Vector2.zero,
                                               GreenFace, GreenShade, "Continue", 88f, Heading);
            Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(560f, 180f), new Vector2(0f, 140f));

            Wire(root, ("scoreLabel", score), ("coinsLabel", coins), ("continueButton", next));
            Save(root, WinPath);
        }

        private static void BuildLose()
        {
            GameObject root = Shell<LosePopup>("Popup_Lose", new Vector2(860f, 720f), out RectTransform card);

            Heading1(card, "Out of Moves", -70f);

            TMP_Text body = UiBuild.Text("Body", card, "So close! Give it another go.", 52f, Body,
                                         TextAlignmentOptions.Center);
            Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(700f, 120f), new Vector2(0f, 90f));

            Button retry = UiBuild.CreateButton("RetryButton", card, new Vector2(560f, 180f), Vector2.zero,
                                                GreenFace, GreenShade, "Retry", 88f, Heading);
            Place((RectTransform)retry.transform, new Vector2(0.5f, 0f), new Vector2(560f, 180f), new Vector2(0f, 290f));

            Button home = UiBuild.CreateButton("HomeButton", card, new Vector2(400f, 130f), Vector2.zero,
                                               PurpleFace, PurpleShade, "Home", 64f, Heading);
            Place((RectTransform)home.transform, new Vector2(0.5f, 0f), new Vector2(400f, 130f), new Vector2(0f, 110f));

            Wire(root, ("retryButton", retry), ("homeButton", home));
            Save(root, LosePath);
        }

        // --- shared parts ---------------------------------------------------------------------

        // Full-screen root with the popup component, a dim that swallows taps, and a gold-edged card.
        private static GameObject Shell<T>(string name, Vector2 cardSize, out RectTransform card) where T : Popup
        {
            var root = new GameObject(name, typeof(RectTransform));
            UiBuild.Stretch((RectTransform)root.transform, 0f, 0f);

            Image dimImage = UiBuild.Fill("Dim", root.transform, Dim);
            dimImage.raycastTarget = true;
            var dim = dimImage.gameObject.AddComponent<CanvasGroup>();

            card = UiBuild.Rect("Card", root.transform);
            Place(card, new Vector2(0.5f, 0.5f), cardSize, Vector2.zero);

            UiBuild.Panel("Edge", card, CardEdge, 0f, -14f);
            RectTransform fill = UiBuild.Panel("Fill", card, CardFill, 0f, 0f);
            fill.offsetMin += new Vector2(10f, 10f);
            fill.offsetMax -= new Vector2(10f, 10f);

            T popup = root.AddComponent<T>();
            var so = new SerializedObject(popup);
            so.FindProperty("dim").objectReferenceValue = dim;
            so.FindProperty("card").objectReferenceValue = card;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static TMP_Text Heading1(RectTransform card, string content, float y)
        {
            TMP_Text text = UiBuild.Text("Title", card, content, 100f, Heading, TextAlignmentOptions.Center);
            Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(760f, 140f), new Vector2(0f, y));

            // One line, shrunk to fit: a heading that wraps climbs out of the top of its card.
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = 56f;
            text.fontSizeMax = 100f;
            return text;
        }

        private static Button Wide(RectTransform card, string name, string label, Color face, Color shade, float y)
        {
            Button button = UiBuild.CreateButton(name, card, new Vector2(600f, 160f), Vector2.zero,
                                                 face, shade, label, 68f, Heading);
            Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(600f, 160f), new Vector2(0f, y));
            return button;
        }

        // A red disc with an X, hanging over the card's top-right corner as in Match Villains.
        private static Button CloseButton(RectTransform card)
        {
            Button close = UiBuild.CreateButton("CloseButton", card, new Vector2(130f, 130f), Vector2.zero,
                                                RedFace, RedShade, "X", 76f, Heading);
            Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(130f, 130f), new Vector2(-10f, -10f));
            return close;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Wire(GameObject root, params (string field, Object value)[] fields)
        {
            var so = new SerializedObject(root.GetComponent<Popup>());
            foreach ((string field, Object value) in fields)
            {
                SerializedProperty property = so.FindProperty(field);
                if (property == null) throw new MissingReferenceException($"{root.name} has no field '{field}'.");
                property.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Save(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
#endif

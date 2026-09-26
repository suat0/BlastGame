#if UNITY_EDITOR
using BlastGame.Game.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.EditorTools
{
    // Builds the popups as prefabs, so the start popup can sit in both scenes and every popup has one
    // definition. Dressed in the generated art after Match Villains: a purple card, a gold crest or a
    // ribbon for the title, glossy buttons lettered in the outlined font, a red disc to close.
    //
    // A scaffold like the other builders: once the layout settles the prefabs become the source of
    // truth and this is retired.
    public static class PopupBuilder
    {
        public const string Folder = "Assets/Prefabs/UI";

        public const string LevelStartPath = Folder + "/Popup_LevelStart.prefab";
        public const string PausePath = Folder + "/Popup_Pause.prefab";
        public const string SettingsPath = Folder + "/Popup_Settings.prefab";
        public const string ConfirmPath = Folder + "/Popup_Confirm.prefab";
        public const string WinPath = Folder + "/Popup_Win.prefab";
        public const string LosePath = Folder + "/Popup_Lose.prefab";

        private const string BoxSpritePath = "Assets/Art/Board/Box0.png";

        private static readonly Color Dim = new Color(0.06f, 0.02f, 0.14f, 0.78f);
        private static readonly Color Body = UiBuild.Hex("#F1E9FF");
        private static readonly Color Figure = UiBuild.Hex("#2E2153");
        private static readonly Color Gold = UiBuild.Hex("#FFD84D");

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

        // "Level 5" under a gold crest, the goal on a cream plate, the move limit, and Play.
        private static void BuildLevelStart()
        {
            GameObject root = Shell<LevelStartPopup>("Popup_LevelStart", new Vector2(860f, 880f), out RectTransform card);

            Image crest = UiBuild.Picture("Crest", card, UiBuild.Art("header_crest"), new Vector2(360f, 240f));
            UiBuild.Place(crest.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 20f));

            TMP_Text title = Heading(card, "Level 1", -150f);

            Image ribbon = UiBuild.Picture("HardRibbon", card, UiBuild.Art("ribbon_red"), new Vector2(520f, 120f));
            UiBuild.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -265f));
            Caption(ribbon.transform, "HARD LEVEL", 50f, 10f);

            Image goal = UiBuild.Picture("Goal", card, UiBuild.Art("panel_cream"), new Vector2(460f, 190f));
            UiBuild.Place(goal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f));

            Image box = UiBuild.Picture("Icon", goal.transform, AssetDatabase.LoadAssetAtPath<Sprite>(BoxSpritePath), new Vector2(120f, 120f));
            UiBuild.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-80f, 0f));

            TMP_Text goalCount = UiBuild.Text("Count", goal.transform, "8", 96f, Figure, TextAlignmentOptions.Left);
            goalCount.rectTransform.sizeDelta = new Vector2(200f, 130f);
            UiBuild.Place(goalCount.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(110f, -4f));

            TMP_Text moves = UiBuild.Text("Moves", card, "20 moves", 56f, Body, TextAlignmentOptions.Center);
            moves.rectTransform.sizeDelta = new Vector2(700f, 80f);
            UiBuild.Place(moves.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f));

            Button play = UiBuild.ArtButton("PlayButton", card, UiBuild.Art("btn_green"), new Vector2(560f, 190f), "Play", 100f);
            UiBuild.Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0f, 140f));

            Button close = CloseButton(card);

            Wire(root, ("titleLabel", title), ("hardRibbon", ribbon.gameObject), ("goalGroup", goal.gameObject),
                 ("goalLabel", goalCount), ("movesLabel", moves), ("playButton", play), ("closeButton", close));

            ribbon.gameObject.SetActive(false);
            Save(root, LevelStartPath);
        }

        private static void BuildSettings(string path, string name, string action)
        {
            GameObject root = Shell<SettingsPopup>(name, new Vector2(820f, 860f), out RectTransform card);

            Heading(card, "Settings", -90f);

            Button music = Wide(card, "MusicButton", "btn_purple", "Music: On", 170f);
            Button sfx = Wide(card, "SfxButton", "btn_purple", "Sound: On", -20f);
            Button actionButton = Wide(card, "ActionButton", "btn_red", action, -250f);
            Button close = CloseButton(card);

            Wire(root, ("closeButton", close), ("actionButton", actionButton), ("musicButton", music), ("sfxButton", sfx),
                 ("musicLabel", music.GetComponentInChildren<TMP_Text>()),
                 ("sfxLabel", sfx.GetComponentInChildren<TMP_Text>()));

            Save(root, path);
        }

        private static void BuildConfirm()
        {
            GameObject root = Shell<ConfirmPopup>("Popup_Confirm", new Vector2(820f, 640f), out RectTransform card);

            TMP_Text title = Heading(card, "Are you sure?", -90f);

            TMP_Text body = UiBuild.Text("Body", card, "This cannot be undone.", 52f, Body, TextAlignmentOptions.Center);
            body.rectTransform.sizeDelta = new Vector2(680f, 150f);
            UiBuild.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f));

            Button confirm = UiBuild.ArtButton("ConfirmButton", card, UiBuild.Art("btn_red"), new Vector2(320f, 150f), "Yes", 68f);
            UiBuild.Place((RectTransform)confirm.transform, new Vector2(0.5f, 0f), new Vector2(-180f, 130f));

            // Cancel on the right and in green: the safe choice is the one the thumb finds first.
            Button cancel = UiBuild.ArtButton("CancelButton", card, UiBuild.Art("btn_green"), new Vector2(320f, 150f), "Stay", 68f);
            UiBuild.Place((RectTransform)cancel.transform, new Vector2(0.5f, 0f), new Vector2(180f, 130f));

            Wire(root, ("titleLabel", title), ("bodyLabel", body),
                 ("confirmLabel", confirm.GetComponentInChildren<TMP_Text>()),
                 ("confirmButton", confirm), ("cancelButton", cancel));
            Save(root, ConfirmPath);
        }

        // The Count celebrating on top of the card, a purple ribbon across it, the score and the coins.
        private static void BuildWin()
        {
            GameObject root = Shell<WinPopup>("Popup_Win", new Vector2(860f, 700f), out RectTransform card);
            card.anchoredPosition = new Vector2(0f, -170f);

            Character(card, "char_count_celebrate", new Vector2(520f, 560f));
            Ribbon(card, "ribbon_purple", "Level Completed!");

            TMP_Text score = UiBuild.Text("Score", card, "Score 0", 60f, Body, TextAlignmentOptions.Center);
            score.rectTransform.sizeDelta = new Vector2(700f, 90f);
            UiBuild.Place(score.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f));

            Image pill = UiBuild.Picture("CoinPill", card, UiBuild.Art("pill_dark"), new Vector2(420f, 140f));
            UiBuild.Place(pill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f));

            Image coin = UiBuild.Picture("Coin", pill.transform, UiBuild.Art("icon_coin"), new Vector2(120f, 120f));
            UiBuild.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 0f));

            TMP_Text coins = UiBuild.Text("Coins", pill.transform, "+0", 84f, Gold, TextAlignmentOptions.Center);
            coins.fontSharedMaterial = UiBuild.Outline;
            UiBuild.Stretch(coins.rectTransform, 40f, 0f);

            Button next = UiBuild.ArtButton("ContinueButton", card, UiBuild.Art("btn_green"), new Vector2(560f, 180f), "Continue", 88f);
            UiBuild.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(0f, 130f));

            Wire(root, ("scoreLabel", score), ("coinsLabel", coins), ("continueButton", next));
            Save(root, WinPath);
        }

        // The Butler, glum, on top of the card, a red ribbon across it, Retry and Home.
        private static void BuildLose()
        {
            GameObject root = Shell<LosePopup>("Popup_Lose", new Vector2(860f, 660f), out RectTransform card);
            card.anchoredPosition = new Vector2(0f, -170f);

            Character(card, "char_butler_sad", new Vector2(460f, 560f));
            Ribbon(card, "ribbon_red", "Out of Moves");

            TMP_Text body = UiBuild.Text("Body", card, "So close! Give it another go.", 50f, Body, TextAlignmentOptions.Center);
            body.rectTransform.sizeDelta = new Vector2(700f, 110f);
            UiBuild.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f));

            Button retry = UiBuild.ArtButton("RetryButton", card, UiBuild.Art("btn_green"), new Vector2(560f, 180f), "Retry", 88f);
            UiBuild.Place((RectTransform)retry.transform, new Vector2(0.5f, 0f), new Vector2(0f, 280f));

            Button home = UiBuild.ArtButton("HomeButton", card, UiBuild.Art("btn_purple"), new Vector2(400f, 130f), "Home", 62f);
            UiBuild.Place((RectTransform)home.transform, new Vector2(0.5f, 0f), new Vector2(0f, 110f));

            Wire(root, ("retryButton", retry), ("homeButton", home));
            Save(root, LosePath);
        }

        // --- shared parts ---------------------------------------------------------------------

        // Full-screen root with the popup component, a dim that swallows taps, and the purple card.
        private static GameObject Shell<T>(string name, Vector2 cardSize, out RectTransform card) where T : Popup
        {
            var root = new GameObject(name, typeof(RectTransform));
            UiBuild.Stretch((RectTransform)root.transform, 0f, 0f);

            Image dimImage = UiBuild.Fill("Dim", root.transform, Dim);
            dimImage.raycastTarget = true;
            var dim = dimImage.gameObject.AddComponent<CanvasGroup>();

            card = UiBuild.Picture("Card", root.transform, UiBuild.Art("panel_purple"), cardSize).rectTransform;

            T popup = root.AddComponent<T>();
            var so = new SerializedObject(popup);
            so.FindProperty("dim").objectReferenceValue = dim;
            so.FindProperty("card").objectReferenceValue = card;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // One line, shrunk to fit: a heading that wraps climbs out of the top of its card.
        private static TMP_Text Heading(RectTransform card, string content, float y)
        {
            TMP_Text text = UiBuild.Text("Title", card, content, 100f, Color.white, TextAlignmentOptions.Center);
            text.fontSharedMaterial = UiBuild.Outline;
            text.rectTransform.sizeDelta = new Vector2(720f, 140f);
            UiBuild.Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y));

            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = 56f;
            text.fontSizeMax = 100f;
            return text;
        }

        // A ribbon across the card's top edge carrying the popup's title.
        private static void Ribbon(RectTransform card, string sprite, string title)
        {
            Image ribbon = UiBuild.Picture("Ribbon", card, UiBuild.Art(sprite), new Vector2(900f, 190f));
            UiBuild.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f));
            Caption(ribbon.transform, title, 76f, 14f);
        }

        // A character standing on the card's top edge, drawn behind the ribbon.
        private static void Character(RectTransform card, string sprite, Vector2 size)
        {
            Image figure = UiBuild.Picture("Character", card, UiBuild.Art(sprite), size);
            figure.rectTransform.pivot = new Vector2(0.5f, 0f);
            UiBuild.Place(figure.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f));
        }

        private static void Caption(Transform parent, string content, float size, float lift)
        {
            TMP_Text text = UiBuild.Text("Label", parent, content, size, Color.white, TextAlignmentOptions.Center);
            text.fontSharedMaterial = UiBuild.Outline;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.6f;
            text.fontSizeMax = size;
            UiBuild.Stretch(text.rectTransform, 0f, lift);
            text.rectTransform.offsetMin += new Vector2(90f, 0f);    // clear of the ribbon's folded ends
            text.rectTransform.offsetMax -= new Vector2(90f, 0f);
        }

        private static Button Wide(RectTransform card, string name, string sprite, string label, float y)
        {
            Button button = UiBuild.ArtButton(name, card, UiBuild.Art(sprite), new Vector2(600f, 160f), label, 64f);
            UiBuild.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, y));
            return button;
        }

        // The red disc with a white X, hanging over the card's top-right corner as in Match Villains.
        private static Button CloseButton(RectTransform card)
        {
            Button close = UiBuild.ArtButton("CloseButton", card, UiBuild.Art("icon_close"), new Vector2(140f, 140f), null, 0f);
            UiBuild.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-20f, -20f));
            return close;
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

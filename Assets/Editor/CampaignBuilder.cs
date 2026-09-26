#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BlastGame.Game.EditorTools
{
    // Writes the twelve campaign levels, the catalog that orders them and the request asset the two
    // scenes share. The table below is the level design: tuning a level is editing a row and running
    // this again, which rewrites the assets in place and keeps their GUIDs.
    public static class CampaignBuilder
    {
        private const string Folder = "Assets/Levels/Campaign";
        private const string CatalogPath = "Assets/Levels/LevelCatalog.asset";
        private const string RequestPath = "Assets/Levels/LevelRequest.asset";

        private readonly struct Row
        {
            public readonly int Rows, Cols, Colors, Boxes, Moves, Seed;
            public readonly bool Hard;

            public Row(int rows, int cols, int colors, int boxes, int moves, int seed, bool hard = false)
            {
                Rows = rows; Cols = cols; Colors = colors;
                Boxes = boxes; Moves = moves; Seed = seed;
                Hard = hard;
            }
        }

        // A sawtooth rather than a ramp: difficulty climbs, peaks on a Hard level every fifth, then
        // relaxes, which is the shape the genre uses. Seeds are fixed so the first attempt at a level
        // is always the same board; retries continue the same random stream and differ.
        // First-guess numbers, to be tuned against the bot's measured pass rate.
        private static readonly Row[] Levels =
        {
            new Row( 7,  7, 4,  5, 20, 1001),
            new Row( 8,  7, 4,  7, 20, 1002),
            new Row( 8,  8, 5,  8, 22, 1003),
            new Row( 8,  8, 5, 10, 22, 1004),
            new Row( 9,  8, 5, 14, 22, 1005, hard: true),
            new Row( 9,  9, 5, 12, 24, 1006),
            new Row( 9,  9, 6, 12, 25, 1007),
            new Row(10,  9, 6, 14, 26, 1008),
            new Row(10, 10, 6, 14, 26, 1009),
            new Row(10, 10, 6, 20, 26, 1010, hard: true),
            new Row(10, 10, 6, 18, 28, 1011),
            new Row(10, 10, 6, 20, 28, 1012),
        };

        // Example 1's thresholds from the case document, on every campaign level. They are what the
        // icons and the combo callouts key off, so keeping them fixed keeps that promise readable.
        private const int ThresholdA = 4, ThresholdB = 7, ThresholdC = 9;

        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Levels", "Campaign");

            var configs = new LevelConfig[Levels.Length];
            for (int i = 0; i < Levels.Length; i++)
                configs[i] = WriteLevel(i + 1, Levels[i]);

            var catalog = LoadOrCreate<LevelCatalog>(CatalogPath);
            var catalogSo = new SerializedObject(catalog);
            SerializedProperty list = catalogSo.FindProperty("levels");
            list.arraySize = configs.Length;
            for (int i = 0; i < configs.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = configs[i];
            catalogSo.ApplyModifiedPropertiesWithoutUndo();

            LoadOrCreate<LevelRequest>(RequestPath);

            AssetDatabase.SaveAssets();
            Debug.Log($"Campaign written: {configs.Length} levels.");
        }

        private static LevelConfig WriteLevel(int number, Row row)
        {
            var level = LoadOrCreate<LevelConfig>($"{Folder}/Level_{number:00}.asset");

            var so = new SerializedObject(level);
            so.FindProperty("rows").intValue = row.Rows;
            so.FindProperty("cols").intValue = row.Cols;
            so.FindProperty("colorCount").intValue = row.Colors;
            so.FindProperty("thresholdA").intValue = ThresholdA;
            so.FindProperty("thresholdB").intValue = ThresholdB;
            so.FindProperty("thresholdC").intValue = ThresholdC;
            so.FindProperty("boxCount").intValue = row.Boxes;
            so.FindProperty("moveLimit").intValue = row.Moves;
            so.FindProperty("seed").intValue = row.Seed;
            so.FindProperty("isHard").boolValue = row.Hard;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(level);
            return level;
        }

        // Rewritten in place when it exists, so references to it (the scenes, the catalog) survive.
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif

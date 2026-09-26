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
        // relaxes, which is the shape the genre uses. The numbers are measured, not guessed: a bot that
        // plays at the Boxes, with one move in five random, played every row a few thousand times and
        // these are the smallest move limits that reach each target pass rate with as many Boxes as
        // that allows - the goal is what the player looks at, so a level carries as much of it as it
        // can. Colour count is the main lever: every extra colour shrinks the groups and starves the
        // Boxes of neighbours.
        //
        // Each seed is the one of two hundred whose fixed first board plays closest to the level's
        // average, so the first attempt is typical of the level rather than lucky or cruel.
        //
        //                rows cols  K  box  mv  seed        target  measured
        private static readonly Row[] Levels =
        {
            new Row( 7,  7, 3, 20, 34,  1025),                // 98%   97%
            new Row( 8,  7, 3, 20, 33,  2019),                // 97%   96%
            new Row( 8,  8, 3, 21, 35,  3048),                // 96%   95%
            new Row( 8,  8, 4,  6, 35,  4118),                // 90%   90%
            new Row( 9,  8, 5,  9, 35,  5153, hard: true),    // 55%   50%
            new Row( 9,  9, 4,  7, 30,  6020),                // 80%   80%
            new Row( 9,  9, 4, 10, 34,  7053),                // 77%   74%
            new Row(10,  9, 4, 11, 34,  8138),                // 73%   71%
            new Row(10, 10, 4, 11, 34,  9040),                // 70%   69%
            new Row(10, 10, 5,  7, 32, 10007, hard: true),    // 55%   53%
            new Row(10, 10, 4, 12, 35, 11100),                // 68%   67%
            new Row(10, 10, 4, 12, 35, 12045),                // 65%   67%
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

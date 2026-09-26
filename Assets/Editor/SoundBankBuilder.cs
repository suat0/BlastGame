#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BlastGame.Game.EditorTools
{
    // Writes the sound bank from the table below and puts the audio service on the App prefab. Like
    // the campaign table, this is where the sound design is read and changed: swapping a clip or
    // turning a sound down is editing a row and running this again.
    //
    // The clips were chosen by name and length from the Kenney packs, not by ear; any of them is
    // one row away from being replaced.
    public static class SoundBankBuilder
    {
        private const string BankPath = "Assets/Audio/SoundBank.asset";
        private const string AppPrefabPath = "Assets/Resources/App.prefab";
        private const string Sfx = "Assets/Audio/Sfx/";

        private readonly struct Row
        {
            public readonly SfxId Id;
            public readonly string[] Clips;
            public readonly float Volume, PitchVariance, Cooldown;
            public readonly int MaxVoices;
            public readonly bool Ducks;

            public Row(SfxId id, float volume, int maxVoices, float cooldown, float pitchVariance, bool ducks, params string[] clips)
            {
                Id = id; Volume = volume; MaxVoices = maxVoices; Cooldown = cooldown;
                PitchVariance = pitchVariance; Ducks = ducks; Clips = clips;
            }
        }

        //                id                  vol  voices cooldown pitch± ducks  clips
        private static readonly Row[] Rows =
        {
            new Row(SfxId.Blast,      0.75f, 2, 0.04f, 0.04f, false, "drop_002", "drop_003", "drop_004"),
            new Row(SfxId.BlastBig,   0.45f, 1, 0.10f, 0.05f, false, "impactGlass_medium_000", "impactGlass_medium_001", "impactGlass_medium_002"),
            new Row(SfxId.Land,       0.18f, 3, 0.05f, 0.10f, false, "impactSoft_medium_000", "impactSoft_medium_001", "impactSoft_medium_002"),
            new Row(SfxId.Reject,     0.50f, 1, 0.10f, 0.03f, false, "impactSoft_heavy_000", "impactSoft_heavy_001"),
            new Row(SfxId.BoxHit,     0.60f, 2, 0.05f, 0.06f, false, "impactWood_medium_000", "impactWood_medium_001", "impactWood_medium_002"),
            new Row(SfxId.BoxBreak,   0.75f, 2, 0.05f, 0.05f, false, "impactPlank_medium_000", "impactPlank_medium_001", "impactPlank_medium_002", "impactWood_heavy_000"),
            new Row(SfxId.Collect,    0.45f, 3, 0.05f, 0.04f, false, "glass_001", "glass_002", "glass_003"),
            new Row(SfxId.Combo,      0.60f, 1, 0.20f, 0.00f, false, "maximize_004"),
            new Row(SfxId.LowMoves,   0.55f, 1, 1.00f, 0.00f, false, "bong_001"),
            new Row(SfxId.Shuffle,    0.70f, 1, 0.30f, 0.00f, false, "card-fan-1", "card-fan-2"),
            new Row(SfxId.LevelStart, 0.65f, 1, 0.50f, 0.00f, true,  "jingles_PIZZI01"),
            new Row(SfxId.Win,        0.80f, 1, 0.50f, 0.00f, true,  "jingles_PIZZI07"),
            new Row(SfxId.Lose,       0.70f, 1, 0.50f, 0.00f, true,  "jingles_SAX07"),
            new Row(SfxId.Click,      0.50f, 2, 0.03f, 0.03f, false, "click_002"),
            new Row(SfxId.PopupOpen,  0.45f, 1, 0.10f, 0.00f, false, "open_001"),
            new Row(SfxId.Locked,     0.40f, 1, 0.20f, 0.00f, false, "error_002"),
            new Row(SfxId.CoinLand,   0.45f, 3, 0.03f, 0.02f, false, "chip-lay-1", "chip-lay-2", "chip-lay-3"),
        };

        [MenuItem("Tools/Blast/Rebuild Sound Bank")]
        public static void Build()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<SoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }

            var so = new SerializedObject(bank);
            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = Rows.Length;

            for (int i = 0; i < Rows.Length; i++)
            {
                Row row = Rows[i];
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").enumValueIndex = (int)row.Id;
                entry.FindPropertyRelative("volume").floatValue = row.Volume;
                entry.FindPropertyRelative("maxVoices").intValue = row.MaxVoices;
                entry.FindPropertyRelative("cooldown").floatValue = row.Cooldown;
                entry.FindPropertyRelative("pitchVariance").floatValue = row.PitchVariance;
                entry.FindPropertyRelative("ducksMusic").boolValue = row.Ducks;

                SerializedProperty clips = entry.FindPropertyRelative("clips");
                clips.arraySize = row.Clips.Length;
                for (int c = 0; c < row.Clips.Length; c++)
                    clips.GetArrayElementAtIndex(c).objectReferenceValue = Clip(Sfx + row.Clips[c] + ".ogg");
            }

            so.FindProperty("homeMusic").objectReferenceValue = Clip("Assets/Audio/Music/music_home.ogg");
            so.FindProperty("levelMusic").objectReferenceValue = Clip("Assets/Audio/Music/music_level.ogg");
            so.ApplyModifiedPropertiesWithoutUndo();

            // The service rides on the App prefab, so the music survives a scene change.
            using (var scope = new PrefabUtility.EditPrefabContentsScope(AppPrefabPath))
            {
                AudioService service = scope.prefabContentsRoot.GetComponent<AudioService>();
                if (service == null) service = scope.prefabContentsRoot.AddComponent<AudioService>();

                var serviceSo = new SerializedObject(service);
                serviceSo.FindProperty("bank").objectReferenceValue = bank;
                serviceSo.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Sound bank written: {Rows.Length} sounds.");
        }

        private static AudioClip Clip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new System.IO.FileNotFoundException(path);
            return clip;
        }
    }
}
#endif

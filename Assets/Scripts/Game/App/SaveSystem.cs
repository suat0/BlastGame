using System;
using System.IO;
using UnityEngine;

namespace BlastGame.Game
{
    // Reads and writes SaveData as JSON under persistentDataPath.
    //
    // A write goes to a temporary file first and is then swapped in, keeping the previous file as a
    // backup. A phone kills apps without warning; writing in place would let that land half-way
    // through and leave a file that parses as nothing. With the swap, the worst case is losing the
    // last save, never all of them.
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        private static string MainPath => Path.Combine(Application.persistentDataPath, FileName);
        private static string TempPath => MainPath + ".tmp";
        private static string BackupPath => MainPath + ".bak";

        public static SaveData Load()
        {
            // The backup is only ever read when the main file is missing or unreadable, which is
            // exactly the state an interrupted swap leaves behind.
            if (TryRead(MainPath, out SaveData data)) return data;
            if (TryRead(BackupPath, out data)) return data;

            return new SaveData();
        }

        public static void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            data.version = SaveData.CurrentVersion;

            try
            {
                File.WriteAllText(TempPath, JsonUtility.ToJson(data));

                if (File.Exists(MainPath)) File.Replace(TempPath, MainPath, BackupPath);
                else File.Move(TempPath, MainPath);
            }
            catch (IOException e)
            {
                // A full disk or a revoked permission. The game keeps running on what is in memory;
                // losing a save is bad, crashing the level the player is in is worse.
                Debug.LogError($"Save failed: {e.Message}");
            }
        }

        public static void Delete()
        {
            File.Delete(MainPath);
            File.Delete(TempPath);
            File.Delete(BackupPath);
        }

        private static bool TryRead(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;

            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (ArgumentException)
            {
                return false;   // malformed JSON
            }
            catch (IOException)
            {
                return false;
            }

            if (data == null) return false;   // an empty file parses to nothing rather than throwing

            Migrate(data);
            return true;
        }

        // Nothing to migrate from yet. When version 2 changes a field's meaning, this is where the old
        // value is converted; everything outside this class only ever sees the current format.
        private static void Migrate(SaveData data)
        {
            data.levelIndex = Math.Max(data.levelIndex, 0);
            data.coins = Math.Max(data.coins, 0);
        }
    }
}

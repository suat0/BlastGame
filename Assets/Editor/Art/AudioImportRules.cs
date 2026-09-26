#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BlastGame.Game.EditorTools
{
    // Import settings for sound by folder, as ArtImportRules does for images.
    //
    //   Audio/Sfx    short, played often, needs to start instantly: mono, decompressed into memory
    //                when loaded, so playing one is a copy rather than a decode
    //   Audio/Music  long, one at a time: stereo, streamed from disk, never whole in memory
    public sealed class AudioImportRules : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            if (assetPath.StartsWith("Assets/Audio/Sfx/"))
            {
                importer.forceToMono = true;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.6f;
            }
            else if (assetPath.StartsWith("Assets/Audio/Music/"))
            {
                importer.forceToMono = false;
                importer.loadInBackground = true;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
            }
            else return;

            importer.defaultSampleSettings = settings;
        }
    }
}
#endif

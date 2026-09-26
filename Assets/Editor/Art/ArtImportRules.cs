#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BlastGame.Game.EditorTools
{
    // Import settings decided by folder, not by whoever imported the file. Dropping an image into the
    // right folder is all it takes: a forgotten compression setting or a background left at 4096 is
    // not something anyone has to remember to avoid.
    //
    //   Art/Board        board sprites, packed into BlockAtlas with the blocks; only generated
    //                    board_ pieces are configured here, the case's sprites are left alone
    //   Art/UI           interface sprites, packed into UiAtlas
    //   Art/Backgrounds  full-screen art, unpacked, 2048 at most
    //   Art/Characters   portraits and poses, unpacked, 1024 at most
    //
    // Re-applied on every import, so the rules win over a hand edit. The one exception is a nine-slice
    // border, which is set once from the image's size and afterwards left to whoever tunes it in the
    // Sprite Editor.
    public sealed class ArtImportRules : AssetPostprocessor
    {
        private const string Root = "Assets/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;

            var importer = (TextureImporter)assetImporter;
            string folder = assetPath.Substring(Root.Length);

            if (folder.StartsWith("Board/board_")) BoardPiece(importer);
            else if (folder.StartsWith("UI/")) Ui(importer);
            else if (folder.StartsWith("Backgrounds/")) Sprite(importer, 2048, "ASTC_6x6");
            else if (folder.StartsWith("Characters/")) Sprite(importer, 1024, "ASTC_4x4");
        }

        private void Ui(TextureImporter importer)
        {
            Sprite(importer, 2048, "ASTC_4x4");

            // Panels, pills and buttons stretch; icons and ribbons do not. A fresh stretchable sprite gets
            // a border of a third of its shorter side, which suits a rounded panel, until someone
            // tunes it.
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            // Ribbons are not in the list: their curve is the shape, and slicing stretches it flat.
            bool stretches = name.StartsWith("panel_") || name.StartsWith("pill_") || name.StartsWith("btn_") ||
                             name.StartsWith("nav_") || name.StartsWith("board_frame");

            if (!stretches || importer.spriteBorder != Vector4.zero) return;

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            float border = Mathf.Min(width, height) / 3f;

            importer.spriteBorder = new Vector4(border, border, border, border);
        }

        // Generated board art: a sprite on the board, nine-sliced, at a density that makes its border
        // about half a cell. The case's own block sprites are left exactly as they were imported.
        private static void BoardPiece(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 250f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            // A full rectangle, not a tight mesh: nine-slicing needs the whole rect, and BlockAtlas packs
            // tightly, which would otherwise hand the frame a mesh it cannot slice.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            if (importer.spriteBorder != Vector4.zero) return;

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            float border = Mathf.Min(width, height) / 3f;
            importer.spriteBorder = new Vector4(border, border, border, border);
        }

        private static void Sprite(TextureImporter importer, int maxSize, string mobileFormat)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;          // UI and backgrounds are drawn at about their size
            importer.alphaIsTransparency = true;     // no dark fringe where transparent meets opaque
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = maxSize;

            // Phones get ASTC, which every current iOS and Android GPU decodes in hardware; the desktop
            // build keeps Unity's default. 6x6 for backgrounds, which are soft and large; 4x4 where
            // edges are crisp and small.
            foreach (string platform in new[] { "Android", "iPhone" })
            {
                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
                settings.overridden = true;
                settings.maxTextureSize = maxSize;
                settings.format = mobileFormat == "ASTC_6x6"
                    ? TextureImporterFormat.ASTC_6x6
                    : TextureImporterFormat.ASTC_4x4;
                importer.SetPlatformTextureSettings(settings);
            }
        }
    }
}
#endif

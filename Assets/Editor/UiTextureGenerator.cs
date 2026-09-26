#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlastGame.Game.EditorTools
{
    // Generates the shapes the board needs behind it. Written as code and dropped into the block
    // folder on purpose: everything in Assets/Art is packed into BlockAtlas, so the backdrop and the
    // frame share the blocks' material and the whole game world stays one draw call.
    public static class UiTextureGenerator
    {
        private const string BackdropPath = "Assets/Art/Backdrop.png";
        private const string FramePath = "Assets/Art/Frame.png";
        private const string CellPath = "Assets/Art/Cell.png";

        public static void Generate()
        {
            WriteBackdrop();
            WriteFrame();
            WriteCell();

            AssetDatabase.Refresh();

            // Sixteen pixels per unit: the backdrop is a strip stretched over the whole view, so its
            // own scale only has to be big, not exact.
            ConfigureSprite(BackdropPath, 16f, Vector4.zero);

            // A twenty pixel border on a sixty-four pixel square, so the corners keep their radius at
            // any board size.
            ConfigureSprite(FramePath, 64f, new Vector4(20f, 20f, 20f, 20f));

            // Sixty-four pixels per unit on a sixty-four pixel tile: one tile, one cell.
            ConfigureSprite(CellPath, 64f, Vector4.zero);

            Debug.Log("Backdrop, frame and cell tile written.");
        }

        // Vertical gradient, darkest at the bottom. Lifting the top separates the board from the HUD
        // without a second panel behind it.
        private static void WriteBackdrop()
        {
            const int Width = 16;
            const int Height = 512;

            Color bottom = Hex("#140E2B");
            Color top = Hex("#2F2160");

            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);

            for (int y = 0; y < Height; y++)
            {
                Color row = Color.Lerp(bottom, top, y / (Height - 1f));
                for (int x = 0; x < Width; x++) texture.SetPixel(x, y, row);
            }

            texture.Apply();
            File.WriteAllBytes(FramePathSafe(BackdropPath), texture.EncodeToPNG());
        }

        // A white rounded square. White so a tint decides the colour, which keeps one texture serving
        // the frame, any panel behind it, and anything else that wants the same corner.
        private static void WriteFrame()
        {
            const int Size = 64;
            const float Radius = 18f;

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Distance to the rounded rectangle, sampled at the pixel centre so the edge
                    // antialiases over one pixel instead of stepping.
                    float dx = Mathf.Max(Radius - (x + 0.5f), (x + 0.5f) - (Size - Radius));
                    float dy = Mathf.Max(Radius - (y + 0.5f), (y + 0.5f) - (Size - Radius));

                    float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) +
                                               Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) - Radius;

                    float alpha = Mathf.Clamp01(0.5f - outside);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            File.WriteAllBytes(FramePathSafe(FramePath), texture.EncodeToPNG());
        }

        // The tile under each cell of the board. White so the board tints it into a checkerboard, a
        // little inset so neighbouring tiles read as separate, and lit from the top so it reads as a
        // shallow well the block sits in. In Assets/Art like the rest, so it packs into BlockAtlas and
        // a hundred of them cost no draw call of their own.
        private static void WriteCell()
        {
            const int Size = 64;
            const float Inset = 2f;
            const float Radius = 10f;

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float lo = Inset + Radius, hi = Size - Inset - Radius;
                    float dx = Mathf.Max(lo - (x + 0.5f), (x + 0.5f) - hi);
                    float dy = Mathf.Max(lo - (y + 0.5f), (y + 0.5f) - hi);

                    float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) +
                                               Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) - Radius;

                    float alpha = Mathf.Clamp01(0.5f - outside);
                    float shade = Mathf.Lerp(0.82f, 1f, y / (Size - 1f));
                    texture.SetPixel(x, y, new Color(shade, shade, shade, alpha));
                }
            }

            texture.Apply();
            File.WriteAllBytes(FramePathSafe(CellPath), texture.EncodeToPNG());
        }

        private static void ConfigureSprite(string path, float pixelsPerUnit, Vector4 border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;

            // Clamped, not repeated: a stretched strip that wraps shows its opposite edge.
            importer.wrapMode = TextureWrapMode.Clamp;

            importer.SaveAndReimport();
        }

        private static string FramePathSafe(string assetPath) =>
            Path.Combine(Directory.GetCurrentDirectory(), assetPath);

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
#endif

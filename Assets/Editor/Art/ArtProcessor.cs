#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlastGame.Game.EditorTools
{
    // Turns the generated images in ArtSource/ into game sprites under Assets/Art/.
    //
    // The generator cannot produce transparency, so an image that needs it arrives on a flat key
    // colour - magenta, or green where the subject itself is pink. This removes that background, cuts
    // sheets into single sprites, trims the empty margin and writes each result where the import rules
    // configure it. The raw images stay outside Assets so Unity never imports them. An image that
    // already has transparency - as ChatGPT can produce - skips the keying.
    //
    // Keying is a flood fill, not a colour test alone. It starts from the image's edge and from every
    // pixel that is almost exactly the key colour, and spreads through anything near it. A purple coat
    // or a pink bow is near the key but neither at the edge nor almost exactly the key, so it survives;
    // the inside of a hollow frame is almost exactly the key, so it goes.
    //
    // Sheets are cut by shape rather than by grid: the generator never lines its cells up exactly.
    // Each separate shape is found, nearby fragments are merged, and the shapes are named in reading
    // order. When the count does not match the names, the sheet falls back to its grid with a warning.
    public static class ArtProcessor
    {
        private const string SourceRoot = "ArtSource";
        private const string OutputRoot = "Assets/Art";

        // Distance from the key colour in RGB units: below Inner is background wherever it is, below
        // Outer is background when connected to background, between the two is the soft edge.
        private const float Inner = 0.22f;
        private const float Outer = 0.45f;

        private readonly struct Sheet
        {
            public readonly int Columns, Rows;
            public readonly string[] Names;
            public readonly string Folder;

            public Sheet(int columns, int rows, string folder, params string[] names)
            {
                Columns = columns; Rows = rows; Folder = folder; Names = names;
            }
        }

        // Named in reading order: left to right, top to bottom.
        private static readonly Dictionary<string, Sheet> Sheets = new Dictionary<string, Sheet>
        {
            ["characters_sheet"] = new Sheet(3, 1, "Characters", "char_count", "char_daughter", "char_butler"),
            // The generator drew the portrait twice, stacked on the left; the top one is used and the
            // spare kept under its own name rather than breaking the count.
            ["poses_sheet"] = new Sheet(3, 1, "Characters",
                "portrait_count", "portrait_count_alt", "char_count_celebrate", "char_butler_sad"),
            ["ui_sheet"] = new Sheet(4, 3, "UI",
                "panel_cream", "panel_purple", "pill_dark", "frame_portrait",
                "header_crest", "ribbon_purple", "ribbon_red", "board_frame",
                "btn_green", "btn_orange", "btn_red", "btn_purple"),
            ["icons_sheet"] = new Sheet(5, 4, "UI/Icons",
                "icon_coin", "icon_heart", "icon_gear", "icon_close", "icon_lock",
                "icon_plus", "icon_music", "icon_speaker", "icon_album", "icon_leaderboard",
                "icon_home", "icon_team", "icon_shop", "icon_profile", "icon_check",
                "icon_badge", "icon_star", "event_bankrob", "event_bombhill", "event_thunder"),
        };

        [MenuItem("Tools/Blast/Process Art Source")]
        public static void ProcessAll()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), SourceRoot);
            if (!Directory.Exists(root))
            {
                Debug.LogWarning($"No {SourceRoot}/ folder next to Assets/; nothing to process.");
                return;
            }

            int written = 0;
            foreach (string file in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(file).ToLowerInvariant();
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg") written += Process(file);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Art processed: {written} sprites written.");
        }

        private static int Process(string file)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string category = new DirectoryInfo(Path.GetDirectoryName(file)).Name;

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(file));

            // Opaque, soft and large: JPEG, which is a quarter of the PNG's size here and loses nothing
            // that survives the texture compression afterwards.
            if (category == "Backgrounds")
            {
                string directory = Path.Combine(Directory.GetCurrentDirectory(), OutputRoot, "Backgrounds");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name + ".jpg"), source.EncodeToJPG(92));
                return 1;
            }

            Texture2D keyed = HasTransparency(source) ? source : Key(source);

            if (Sheets.TryGetValue(name, out Sheet sheet)) return Cut(keyed, sheet, name);

            Write(Trim(keyed), category == "Characters" ? "Characters" : "UI", name);
            return 1;
        }

        private static bool HasTransparency(Texture2D image)
        {
            int w = image.width, h = image.height;
            return image.GetPixel(0, 0).a < 0.5f && image.GetPixel(w - 1, 0).a < 0.5f &&
                   image.GetPixel(0, h - 1).a < 0.5f && image.GetPixel(w - 1, h - 1).a < 0.5f;
        }

        // --- keying ---------------------------------------------------------------------------

        private static Texture2D Key(Texture2D image)
        {
            int w = image.width, h = image.height;
            Color[] pixels = image.GetPixels();

            Color key = KeyColour(pixels, w, h);

            // A black key sits right next to the dark outlines the art is drawn with, so it gets a much
            // narrower band - and no despill, since a dark fringe on a dark outline is invisible.
            bool black = key.r + key.g + key.b < 0.3f;
            float inner = black ? 0.05f : Inner;
            float outer = black ? 0.14f : Outer;

            EraseGridLines(pixels, w, h, key);

            var distance = new float[pixels.Length];
            for (int i = 0; i < pixels.Length; i++) distance[i] = Distance(pixels[i], key);

            var background = new bool[pixels.Length];
            var stack = new Stack<int>();

            void Seed(int i)
            {
                if (background[i] || distance[i] >= outer) return;
                background[i] = true;
                stack.Push(i);
            }

            for (int x = 0; x < w; x++) { Seed(x); Seed((h - 1) * w + x); }
            for (int y = 0; y < h; y++) { Seed(y * w); Seed(y * w + w - 1); }
            for (int i = 0; i < pixels.Length; i++) if (distance[i] < inner) Seed(i);

            while (stack.Count > 0)
            {
                int i = stack.Pop();
                int x = i % w, y = i / w;

                if (x > 0) Seed(i - 1);
                if (x < w - 1) Seed(i + 1);
                if (y > 0) Seed(i - w);
                if (y < h - 1) Seed(i + w);
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                if (!background[i]) continue;

                float alpha = Mathf.InverseLerp(inner, outer, distance[i]);
                Color c = pixels[i];

                // Despill only on this edge band: the key bleeds into antialiased edges, and removing
                // it from solid pixels would also strip the purple out of a purple coat.
                if (alpha > 0f && !black) c = Despill(c, key, 1f - alpha);

                c.a = alpha;
                pixels[i] = c;
            }

            var result = new Texture2D(w, h, TextureFormat.RGBA32, false);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        // Some sheets come back ruled into cells with thin grey or white lines running edge to edge.
        // Left in, they join every shape into one. A line is a row or column that is almost entirely
        // line-coloured - light and colourless - and its line-coloured pixels, with those of its
        // immediate neighbours, are painted over with the key.
        //
        // Colourless matters: a bright magenta background is light too, and taking brightness alone
        // turns every mostly-background row into a "line" and paints the faces in it with the key.
        private static void EraseGridLines(Color[] pixels, int w, int h, Color key)
        {
            const float Coverage = 0.85f;
            const int Spread = 2;   // JPEG smears a one-pixel line over a few

            bool IsLight(Color c) => IsLine(c);

            for (int y = 0; y < h; y++)
            {
                int light = 0;
                for (int x = 0; x < w; x++) if (IsLight(pixels[y * w + x])) light++;
                if (light < w * Coverage) continue;

                for (int yy = Math.Max(0, y - Spread); yy <= Math.Min(h - 1, y + Spread); yy++)
                for (int x = 0; x < w; x++)
                    if (IsLight(pixels[yy * w + x]) || yy != y) Blend(pixels, yy * w + x, key);
            }

            for (int x = 0; x < w; x++)
            {
                int light = 0;
                for (int y = 0; y < h; y++) if (IsLight(pixels[y * w + x])) light++;
                if (light < h * Coverage) continue;

                for (int xx = Math.Max(0, x - Spread); xx <= Math.Min(w - 1, x + Spread); xx++)
                for (int y = 0; y < h; y++)
                    if (IsLight(pixels[y * w + xx]) || xx != x) Blend(pixels, y * w + xx, key);
            }
        }

        // Only colourless pixels are replaced - the line itself, and the dim grey JPEG smears on either
        // side of it, which are too dark to count as line but belong to it. A neighbour row crossing
        // an actual shape keeps the shape's coloured pixels.
        private static void Blend(Color[] pixels, int i, Color key)
        {
            Color c = pixels[i];
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            if (max - min < 0.12f || Distance(c, key) < 0.2f) pixels[i] = key;
        }

        private static bool IsLine(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max > 0.3f && max - min < 0.12f;
        }

        private static Color KeyColour(Color[] pixels, int w, int h) =>
            (pixels[0] + pixels[w - 1] + pixels[(h - 1) * w] + pixels[h * w - 1]) / 4f;

        private static float Distance(Color a, Color b)
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        // Pulls the key's dominant channels down to the level of the other: for magenta, red and blue
        // towards green; for green, green towards the larger of red and blue.
        private static Color Despill(Color c, Color key, float amount)
        {
            if (key.r > key.g && key.b > key.g)
            {
                float spill = Mathf.Min(c.r, c.b) - c.g;
                if (spill > 0f) { c.r -= spill * amount; c.b -= spill * amount; }
            }
            else
            {
                float limit = Mathf.Max(c.r, c.b);
                if (c.g > limit) c.g = Mathf.Lerp(c.g, limit, amount);
            }

            return c;
        }

        // --- cutting --------------------------------------------------------------------------

        private static int Cut(Texture2D image, Sheet sheet, string sheetName)
        {
            List<RectInt> shapes = FindShapes(image);

            if (shapes.Count == sheet.Names.Length)
            {
                for (int i = 0; i < shapes.Count; i++)
                    Write(Crop(image, shapes[i]), FolderOf(sheet.Names[i], sheet.Folder), sheet.Names[i]);
                return shapes.Count;
            }

            var found = new System.Text.StringBuilder();
            foreach (RectInt shape in shapes) found.Append($"\n  {shape.width}x{shape.height} at ({shape.x}, {shape.y})");

            Debug.LogWarning($"{sheetName}: found {shapes.Count} shapes for {sheet.Names.Length} names; " +
                             $"cutting on the grid instead. Check the results.{found}");

            int cellWidth = image.width / sheet.Columns;
            int cellHeight = image.height / sheet.Rows;

            for (int i = 0; i < sheet.Names.Length; i++)
            {
                int column = i % sheet.Columns;
                int row = i / sheet.Columns;
                var cell = new RectInt(column * cellWidth, image.height - (row + 1) * cellHeight, cellWidth, cellHeight);
                Write(Trim(Crop(image, cell)), FolderOf(sheet.Names[i], sheet.Folder), sheet.Names[i]);
            }

            return sheet.Names.Length;
        }

        // Connected visible regions, merged when close enough to belong to one icon (a gear and its
        // circle, a sparkle beside a coin), in reading order.
        private static List<RectInt> FindShapes(Texture2D image)
        {
            int w = image.width, h = image.height;
            Color[] pixels = image.GetPixels();

            var label = new int[pixels.Length];
            var boxes = new List<RectInt>();
            var stack = new Stack<int>();

            for (int start = 0; start < pixels.Length; start++)
            {
                if (label[start] != 0 || pixels[start].a < 0.1f) continue;

                int id = boxes.Count + 1;
                int minX = w, minY = h, maxX = 0, maxY = 0;

                label[start] = id;
                stack.Push(start);

                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    int x = i % w, y = i / w;
                    minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);

                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;

                        int n = ny * w + nx;
                        if (label[n] != 0 || pixels[n].a < 0.1f) continue;

                        label[n] = id;
                        stack.Push(n);
                    }
                }

                boxes.Add(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
            }

            // Specks are noise, not shapes.
            int minimum = Mathf.Max(8, Mathf.Min(w, h) / 60);
            boxes.RemoveAll(b => b.width < minimum && b.height < minimum);

            // Merge anything closer than a small gap, until nothing changes.
            int gap = Mathf.Max(4, Mathf.Min(w, h) / 80);
            bool merged = true;
            while (merged)
            {
                merged = false;
                for (int i = 0; i < boxes.Count && !merged; i++)
                for (int j = i + 1; j < boxes.Count && !merged; j++)
                {
                    RectInt a = boxes[i], b = boxes[j];
                    bool near = a.xMin - gap <= b.xMax && b.xMin - gap <= a.xMax &&
                                a.yMin - gap <= b.yMax && b.yMin - gap <= a.yMax;
                    if (!near) continue;

                    int x0 = Math.Min(a.xMin, b.xMin), y0 = Math.Min(a.yMin, b.yMin);
                    int x1 = Math.Max(a.xMax, b.xMax), y1 = Math.Max(a.yMax, b.yMax);
                    boxes[i] = new RectInt(x0, y0, x1 - x0, y1 - y0);
                    boxes.RemoveAt(j);
                    merged = true;
                }
            }

            // Fragments - a coin thrown beside a character, the sparkles around a star, loose bolts
            // around a lightning ball - join the nearest real shape instead of counting as their own.
            int largest = 0;
            foreach (RectInt b in boxes) largest = Math.Max(largest, b.width * b.height);

            float reach = Mathf.Min(w, h) * 0.15f;
            for (int i = boxes.Count - 1; i >= 0; i--)
            {
                RectInt minor = boxes[i];
                if (minor.width * minor.height >= largest * 0.06f) continue;

                int nearest = -1;
                float best = reach;
                for (int j = 0; j < boxes.Count; j++)
                {
                    RectInt major = boxes[j];
                    if (j == i || major.width * major.height < largest * 0.06f) continue;

                    float dx = Mathf.Max(0, Mathf.Max(major.xMin - minor.xMax, minor.xMin - major.xMax));
                    float dy = Mathf.Max(0, Mathf.Max(major.yMin - minor.yMax, minor.yMin - major.yMax));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < best) { best = d; nearest = j; }
                }

                if (nearest < 0) continue;

                RectInt target = boxes[nearest];
                int x0 = Math.Min(target.xMin, minor.xMin), y0 = Math.Min(target.yMin, minor.yMin);
                int x1 = Math.Max(target.xMax, minor.xMax), y1 = Math.Max(target.yMax, minor.yMax);
                boxes[nearest] = new RectInt(x0, y0, x1 - x0, y1 - y0);
                boxes.RemoveAt(i);
            }

            // Reading order: rows from the top (texture y grows upwards), then left to right. A shape
            // joins the current row when its centre is within half the row's tallest shape.
            boxes.Sort((a, b) => b.center.y.CompareTo(a.center.y));

            var ordered = new List<RectInt>();
            var row = new List<RectInt>();
            float rowY = 0f, rowHeight = 0f;

            foreach (RectInt box in boxes)
            {
                if (row.Count > 0 && Mathf.Abs(box.center.y - rowY) > rowHeight * 0.5f)
                {
                    row.Sort((a, b) => a.center.x.CompareTo(b.center.x));
                    ordered.AddRange(row);
                    row.Clear();
                }

                if (row.Count == 0) { rowY = box.center.y; rowHeight = 0f; }
                row.Add(box);
                rowHeight = Mathf.Max(rowHeight, box.height);
            }

            row.Sort((a, b) => a.center.x.CompareTo(b.center.x));
            ordered.AddRange(row);
            return ordered;
        }

        // --- helpers --------------------------------------------------------------------------

        // Board pieces are drawn by SpriteRenderers on the board, so they go where BlockAtlas packs
        // them rather than into the UI atlas - a board drawn from two textures is two batches.
        private static string FolderOf(string name, string sheetFolder) =>
            name.StartsWith("board_") ? "Board" : sheetFolder;

        // A few pixels of margin, so filtering never samples the texture's edge.
        private const int Margin = 4;

        private static Texture2D Crop(Texture2D image, RectInt area)
        {
            int x0 = Math.Max(area.xMin - Margin, 0), y0 = Math.Max(area.yMin - Margin, 0);
            int x1 = Math.Min(area.xMax + Margin, image.width), y1 = Math.Min(area.yMax + Margin, image.height);

            var cropped = new Texture2D(x1 - x0, y1 - y0, TextureFormat.RGBA32, false);
            cropped.SetPixels(image.GetPixels(x0, y0, x1 - x0, y1 - y0));
            cropped.Apply();
            return cropped;
        }

        private static Texture2D Trim(Texture2D image)
        {
            int w = image.width, h = image.height;
            Color[] pixels = image.GetPixels();

            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a <= 0.02f) continue;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }

            return maxX < 0 ? image : Crop(image, new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
        }

        private static void Write(Texture2D image, string folder, string name)
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), OutputRoot, folder);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());

            // A regenerated image is a different size, so a nine-slice border set for the old one is
            // wrong for it. Clearing it lets the import rules measure the new image; a border tuned by
            // hand for the old art had no meaning left anyway.
            string assetPath = $"{OutputRoot}/{folder}/{name}.png";
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer && importer.spriteBorder != Vector4.zero)
            {
                importer.spriteBorder = Vector4.zero;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif

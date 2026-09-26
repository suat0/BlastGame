#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlastGame.Game.EditorTools
{
    // Writes the particle sheet and its material, and builds the level scene's particle systems.
    // Every shape is drawn here rather than imported, so the sheet has no licence to track and its
    // premultiplied pixels are exactly what the shader expects.
    //
    // The systems are configured in code for the same reason the HUD is: a particle system's settings
    // are hundreds of lines of scene YAML, and here they are thirty readable ones per effect.
    public static class VfxBuilder
    {
        private const string Folder = "Assets/Vfx";
        private const string SheetPath = Folder + "/VfxSheet.png";
        private const string MaterialPath = Folder + "/Vfx.mat";
        private const string ShaderName = "Blast/Particle Premultiplied";

        // Outside Assets/Art on purpose: everything there is packed into BlockAtlas, and this sheet
        // is drawn by the particle material, not the sprite one.
        private const int Tile = 128;
        private const int Columns = 4;
        private const int Rows = 2;

        private enum Shape { Glow = 0, Star = 1, Splinter = 2, Confetti = 3, Puff = 4, Ring = 5 }

        public static void Build()
        {
            WriteSheet();
            Material material = WriteMaterial();

            Scene scene = EditorSceneManager.OpenScene(SceneSetup.LevelScenePath, OpenSceneMode.Single);

            var boardView = Object.FindFirstObjectByType<BoardView>();
            Transform game = boardView.transform;

            Transform old = game.Find("Vfx");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = new GameObject("Vfx");
            root.transform.SetParent(game, false);
            var vfx = root.AddComponent<Vfx>();

            var so = new SerializedObject(vfx);
            so.FindProperty("sparkles").objectReferenceValue = Sparkles(root.transform, material);
            so.FindProperty("glows").objectReferenceValue = Glows(root.transform, material);
            so.FindProperty("splinters").objectReferenceValue = Splinters(root.transform, material);
            so.FindProperty("dust").objectReferenceValue = Dust(root.transform, material);
            so.FindProperty("confetti").objectReferenceValue = Confetti(root.transform, material);
            so.ApplyModifiedPropertiesWithoutUndo();

            var viewSo = new SerializedObject(boardView);
            viewSo.FindProperty("vfx").objectReferenceValue = vfx;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Particles built.");
        }

        // --- the systems ----------------------------------------------------------------------

        private static ParticleSystem Sparkles(Transform parent, Material material)
        {
            ParticleSystem ps = Create(parent, "Sparkles", Shape.Star, material, 400);

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(Hex("#FFF6C8"), Hex("#FFD84D"));
            main.gravityModifier = 0.6f;

            Circle(ps, 0.2f);
            GrowThenShrink(ps);
            FadeOut(ps, 0.5f);
            return ps;
        }

        private static ParticleSystem Glows(Transform parent, Material material)
        {
            ParticleSystem ps = Create(parent, "Glows", Shape.Glow, material, 30);

            var main = ps.main;
            main.startLifetime = 0.28f;
            main.startSpeed = 0f;
            main.startSize = 2f;   // each emit sets its own, from the group size

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.25f));

            FadeOut(ps, 0f);
            return ps;
        }

        private static ParticleSystem Splinters(Transform parent, Material material)
        {
            ParticleSystem ps = Create(parent, "Splinters", Shape.Splinter, material, 300);

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.95f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(Hex("#E0A060"), Hex("#8C5423"));
            main.gravityModifier = 2.5f;

            Circle(ps, 0.3f);
            Spin(ps, 9f);
            FadeOut(ps, 0.7f);
            return ps;
        }

        private static ParticleSystem Dust(Transform parent, Material material)
        {
            ParticleSystem ps = Create(parent, "Dust", Shape.Puff, material, 60);

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
            main.startColor = new Color(0.8f, 0.72f, 0.62f, 0.7f);

            Circle(ps, 0.25f);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

            FadeOut(ps, 0f);
            return ps;
        }

        // A fountain from the middle of the board on a win.
        private static ParticleSystem Confetti(Transform parent, Material material)
        {
            ParticleSystem ps = Create(parent, "Confetti", Shape.Confetti, material, 300);

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 13f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.34f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 1.6f;

            var palette = new Gradient();
            palette.SetKeys(
                new[]
                {
                    new GradientColorKey(Hex("#F0445A"), 0f), new GradientColorKey(Hex("#FFD84D"), 0.2f),
                    new GradientColorKey(Hex("#5BC236"), 0.4f), new GradientColorKey(Hex("#3AB4F2"), 0.6f),
                    new GradientColorKey(Hex("#FF6FD8"), 0.8f), new GradientColorKey(Hex("#9B5CF6"), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(palette) { mode = ParticleSystemGradientMode.RandomColor };

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.5f;
            shape.rotation = new Vector3(-90f, 0f, 0f);   // the cone opens upwards

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.8f;
            noise.frequency = 0.6f;

            Spin(ps, 6f);
            FadeOut(ps, 0.75f);
            return ps;
        }

        // --- shared configuration -------------------------------------------------------------

        // Never plays or emits by itself: the game calls Emit. World space, so a burst stays where it
        // was emitted while the camera shakes.
        private static ParticleSystem Create(Transform parent, string name, Shape shape, Material material, int max)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);

            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;

            var emission = ps.emission;
            emission.enabled = false;

            var shapeModule = ps.shape;
            shapeModule.enabled = false;

            // One tile of the sheet, held for the particle's whole life.
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = Columns;
            sheet.numTilesY = Rows;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(((int)shape + 0.5f) / (Columns * Rows));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Above every sprite on the board. The particles' one draw call comes after the board's.
            renderer.sortingOrder = 20;

            return ps;
        }

        // A flat disc in the board's plane, throwing particles outwards within it.
        private static void Circle(ParticleSystem ps, float radius)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.rotation = Vector3.zero;
        }

        private static void GrowThenShrink(ParticleSystem ps)
        {
            var curve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f));
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void Spin(ParticleSystem ps, float radiansPerSecond)
        {
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-radiansPerSecond, radiansPerSecond);
        }

        // Opaque until fadeFrom of the lifetime, transparent at the end.
        private static void FadeOut(ParticleSystem ps, float fadeFrom)
        {
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeFrom), new GradientAlphaKey(0f, 1f) });

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(fade);
        }

        // --- the sheet ------------------------------------------------------------------------

        // Premultiplied throughout. A light (glow, star, ring) has colour and zero alpha, so it adds;
        // a solid (splinter, confetti, puff) has colour equal to its alpha, so it covers.
        private static void WriteSheet()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Vfx");

            var texture = new Texture2D(Tile * Columns, Tile * Rows, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[texture.width * texture.height]);

            for (int i = 0; i <= (int)Shape.Ring; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                int x0 = column * Tile;
                int y0 = (Rows - 1 - row) * Tile;   // the sheet's first row is the texture's top

                for (int y = 0; y < Tile; y++)
                for (int x = 0; x < Tile; x++)
                {
                    float u = (x + 0.5f) / Tile * 2f - 1f;
                    float v = (y + 0.5f) / Tile * 2f - 1f;
                    texture.SetPixel(x0 + x, y0 + y, Pixel((Shape)i, u, v));
                }
            }

            texture.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), SheetPath), texture.EncodeToPNG());
            AssetDatabase.Refresh();

            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = false;   // it would bleed colour into the zero-alpha lights
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Color Pixel(Shape shape, float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);

            switch (shape)
            {
                case Shape.Glow:
                {
                    float light = Mathf.Pow(Mathf.Clamp01(1f - r), 2.2f);
                    return new Color(light, light, light, 0f);
                }
                case Shape.Star:
                {
                    // A bright core and four thin rays tapering to the edge.
                    float core = Mathf.Exp(-r * r * 30f);
                    float rayX = Mathf.Exp(-Mathf.Abs(v) * 28f) * Mathf.Clamp01(1f - Mathf.Abs(u));
                    float rayY = Mathf.Exp(-Mathf.Abs(u) * 28f) * Mathf.Clamp01(1f - Mathf.Abs(v));
                    float light = Mathf.Clamp01(core + rayX + rayY);
                    return new Color(light, light, light, 0f);
                }
                case Shape.Splinter:
                {
                    // A sliver: thickest in the middle, pointed at both ends.
                    float halfWidth = 0.2f * (1f - u * u);
                    float edge = Mathf.Clamp01((halfWidth - Mathf.Abs(v)) * Tile * 0.5f);
                    float a = Mathf.Abs(u) < 0.95f ? edge : 0f;
                    return new Color(a, a, a, a);
                }
                case Shape.Confetti:
                {
                    float a = Mathf.Clamp01((0.6f - Mathf.Abs(u)) * Tile * 0.5f) *
                              Mathf.Clamp01((0.36f - Mathf.Abs(v)) * Tile * 0.5f);
                    return new Color(a, a, a, a);
                }
                case Shape.Puff:
                {
                    float a = Mathf.Pow(Mathf.Clamp01(1f - r), 1.5f) * 0.85f;
                    return new Color(a, a, a, a);
                }
                case Shape.Ring:
                {
                    float d = (r - 0.7f) / 0.08f;
                    float light = Mathf.Exp(-d * d);
                    return new Color(light, light, light, 0f);
                }
            }

            return Color.clear;
        }

        private static Material WriteMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Shader shader = Shader.Find(ShaderName);
            if (shader == null) throw new System.InvalidOperationException($"Shader '{ShaderName}' not found.");

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
#endif

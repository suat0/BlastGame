#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlastGame.Game.EditorTools
{
    // The small builders every screen generator shares: a panel, a label, a button, a stretched rect.
    // Lifted out of HudBuilder once the home screen and the popups needed the same ones.
    public static class UiBuild
    {
        public const string FontPath = "Assets/Fonts/Baloo2-ExtraBold SDF.asset";

        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        private static TMP_FontAsset font;
        private static Sprite panelSprite;

        public static TMP_FontAsset Font
        {
            get
            {
                if (font != null) return font;

                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (font == null) throw new System.IO.FileNotFoundException(FontPath);
                return font;
            }
        }

        public const string OutlinePath = "Assets/Fonts/Baloo2-ExtraBold SDF Outline.mat";

        private static Material outline;

        // The font with a thick dark purple outline, as Match Villains letters its callouts and
        // buttons. A material of its own, so text using it costs a SetPass of its own - kept to the
        // few places that are meant to shout.
        public static Material Outline
        {
            get
            {
                if (outline != null) return outline;

                outline = AssetDatabase.LoadAssetAtPath<Material>(OutlinePath);
                if (outline == null)
                {
                    outline = new Material(Font.material) { name = "Baloo2-ExtraBold SDF Outline" };
                    AssetDatabase.CreateAsset(outline, OutlinePath);
                }

                outline.EnableKeyword("OUTLINE_ON");
                outline.SetFloat("_OutlineWidth", 0.28f);
                outline.SetColor("_OutlineColor", Hex("#2E1F6B"));
                outline.SetFloat("_FaceDilate", 0.15f);
                EditorUtility.SetDirty(outline);
                return outline;
            }
        }

        // Unity's own rounded nine-slice, the one a fresh Image starts with. Tinted per panel, it
        // carries every panel until the generated art replaces it.
        public static Sprite PanelSprite =>
            panelSprite != null
                ? panelSprite
                : panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Match 0.5 between width and height, as the level scene has always used.
        public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                                    typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Panel(string name, Transform parent, Color color, float offsetX, float offsetY)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Stretch(rect, offsetX, offsetY);

            var image = go.GetComponent<Image>();
            image.sprite = PanelSprite;
            image.type = Image.Type.Sliced;
            image.color = color;

            MakeNonInteractive(image);

            // Below one, the nine-slice borders scale up: Unity's sprite has a small corner made for
            // a small button, and these panels are hundreds of pixels wide.
            image.pixelsPerUnitMultiplier = 0.25f;

            return rect;
        }

        // A flat wash, not a rounded panel: the dim behind a popup, a full-screen backdrop.
        public static Image Fill(string name, Transform parent, Color color)
        {
            RectTransform rect = Panel(name, parent, color, 0f, 0f);
            var image = rect.GetComponent<Image>();
            image.sprite = null;
            return image;
        }

        public static TMP_Text Text(string name, Transform parent, string content, float size,
                                    Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = Font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.text = content;

            MakeNonInteractive(text);

            return text;
        }

        // A drop-shadowed button: a darker copy offset downwards, the face on top, a label across it.
        // Returns the Button; the face is its target graphic and the only raycast target in it.
        public static Button CreateButton(string name, Transform parent, Vector2 size, Vector2 position,
                                    Color face, Color shade, string label, float labelSize, Color labelColor)
        {
            RectTransform area = Rect(name, parent);
            area.anchorMin = area.anchorMax = new Vector2(0.5f, 0.5f);
            area.sizeDelta = size;
            area.anchoredPosition = position;

            Panel("Shadow", area, shade, 0f, -14f);

            RectTransform faceRect = Panel("Face", area, face, 0f, 0f);
            var faceImage = faceRect.GetComponent<Image>();
            faceImage.raycastTarget = true;

            var button = area.gameObject.AddComponent<Button>();
            button.targetGraphic = faceImage;

            TMP_Text text = Text("Label", faceRect, label, labelSize, labelColor, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 0f, 0f);

            return button;
        }

        // Two opt-outs, both for graphics nobody interacts with. raycastTarget keeps the graphic off
        // the list the raycaster walks on every tap; maskable keeps it out of the stencil test uGUI
        // otherwise runs in case a Mask is above it. Safe only because these canvases have no Mask or
        // RectMask2D - under one, maskable false would make the graphic ignore the mask entirely.
        public static void MakeNonInteractive(MaskableGraphic graphic)
        {
            if (graphic == null) return;

            graphic.raycastTarget = false;
            graphic.maskable = false;
        }

        public static void Stretch(RectTransform rect, float offsetX, float offsetY)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(offsetX, offsetY);
            rect.offsetMax = new Vector2(offsetX, offsetY);
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
#endif

using UnityEngine;

namespace BlastGame.Game.UI
{
    // Shrinks its RectTransform to the part of the screen no notch, rounded corner or home indicator
    // covers. Buttons and text go inside it; backgrounds stay outside and run to the edges.
    //
    // Re-applied only when the safe area actually changes - a rotation, a resized editor window - so
    // on almost every frame this is one Rect comparison.
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform rect;
        private Rect applied;
        private Vector2Int appliedScreen;

        private void Awake()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != applied || Screen.width != appliedScreen.x || Screen.height != appliedScreen.y)
                Apply();
        }

        private void Apply()
        {
            applied = Screen.safeArea;
            appliedScreen = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width <= 0 || Screen.height <= 0) return;

            // Anchors in the canvas's own 0..1 space, so the canvas scaler has nothing to undo.
            rect.anchorMin = new Vector2(applied.xMin / Screen.width, applied.yMin / Screen.height);
            rect.anchorMax = new Vector2(applied.xMax / Screen.width, applied.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

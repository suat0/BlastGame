using UnityEngine;

namespace BlastGame.Game
{
    // The picture behind the board: one per chapter of the campaign, chosen when the level starts and
    // kept covering the camera's view whatever size the board made it.
    //
    // A child of the camera, so a screen shake moves the board against a still background - the knock
    // reads as the board's, not the world's.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class LevelBackground : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private Camera viewCamera;

        [Tooltip("One per chapter, in campaign order. Past the last, the last one stays.")]
        [SerializeField] private Sprite[] chapters;

        [SerializeField] private int levelsPerChapter = 4;

        private SpriteRenderer spriteRenderer;
        private float coveredSize = -1f;
        private float coveredAspect = -1f;

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        // On the board's ready event rather than in Start: the controller decides which level this is
        // in its own Start, and nothing orders the two.
        private void OnEnable() => controller.OnBoardReady += HandleBoardReady;

        private void OnDisable() => controller.OnBoardReady -= HandleBoardReady;

        private void HandleBoardReady(BlastGame.Core.Board board)
        {
            if (chapters.Length == 0) return;

            // A debug level has no chapter; it gets the first.
            int chapter = Mathf.Max(controller.CampaignIndex, 0) / levelsPerChapter;
            spriteRenderer.sprite = chapters[Mathf.Min(chapter, chapters.Length - 1)];
        }

        // Only when the view changed - a new board size, a resized window.
        private void LateUpdate()
        {
            if (spriteRenderer.sprite == null) return;
            if (viewCamera.orthographicSize == coveredSize && viewCamera.aspect == coveredAspect) return;

            coveredSize = viewCamera.orthographicSize;
            coveredAspect = viewCamera.aspect;

            // Cover, not fit: the larger of the two scales, so no edge of the view is left bare.
            Vector2 view = new Vector2(coveredSize * 2f * coveredAspect, coveredSize * 2f);
            Vector2 sprite = spriteRenderer.sprite.bounds.size;
            float scale = Mathf.Max(view.x / sprite.x, view.y / sprite.y);

            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}

using BlastGame.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.UI
{
    // What a move says back: the points it scored rising from where the tap landed, a word for a big
    // group, a flash for the biggest. On a canvas of its own between the HUD and the popups, so text
    // that fades every frame rebuilds this canvas and not the HUD's.
    //
    // The words key off the level's icon thresholds, the same numbers that change a block's icon
    // before it is tapped: the icon is the promise, the word is it being kept.
    public sealed class FeedbackView : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private BoardView boardView;

        [Header("Score")]
        [Tooltip("Reused in turn. A move comes long after the previous popup has faded.")]
        [SerializeField] private TMP_Text[] scorePopups;
        [SerializeField] private float scoreRise = 140f;
        [SerializeField] private float scoreDuration = 0.8f;

        [Header("Combo")]
        [SerializeField] private TMP_Text comboLabel;
        [SerializeField] private float comboHold = 0.45f;

        [Header("Flash")]
        [SerializeField] private Image flash;
        [SerializeField] private float flashAlpha = 0.35f;
        [SerializeField] private float flashDuration = 0.3f;

        private int lastScore;
        private int nextPopup;
        private Vector3 tapWorld;
        private int groupSize;

        private void Awake()
        {
            foreach (TMP_Text popup in scorePopups) popup.gameObject.SetActive(false);
            comboLabel.gameObject.SetActive(false);
            SetFlash(0f);
        }

        private void OnEnable()
        {
            controller.OnBoardReady += HandleBoardReady;
            controller.OnBoardChanged += HandleBoardChanged;
            controller.OnStatusChanged += HandleStatusChanged;
        }

        private void OnDisable()
        {
            controller.OnBoardReady -= HandleBoardReady;
            controller.OnBoardChanged -= HandleBoardChanged;
            controller.OnStatusChanged -= HandleStatusChanged;
        }

        private void OnDestroy()
        {
            // Both the transforms and the labels: the fades target the label itself.
            foreach (TMP_Text popup in scorePopups)
            {
                Tween.StopAll(popup.transform);
                Tween.StopAll(popup);
            }

            Tween.StopAll(comboLabel.transform);
            Tween.StopAll(comboLabel);
            Tween.StopAll(flash);
        }

        private void HandleBoardReady(Board board) => lastScore = 0;

        // Read during the call and not kept - the result is Core's single reused instance.
        private void HandleBoardChanged(BlastResult result)
        {
            tapWorld = boardView.CellToWorld(result.TappedIndex);
            groupSize = result.BlastedGroupSize;
        }

        // The score is read as a difference rather than recomputed, so the formula lives in Core only.
        private void HandleStatusChanged()
        {
            int score = controller.Session.Score;
            int gained = score - lastScore;
            lastScore = score;

            if (gained <= 0) return;

            ShowScore(gained);
            ShowCombo();
        }

        private void ShowScore(int points)
        {
            TMP_Text popup = scorePopups[nextPopup];
            nextPopup = (nextPopup + 1) % scorePopups.Length;

            Transform t = popup.transform;
            Tween.StopAll(t);

            popup.SetText("+{0:0}", points);
            popup.gameObject.SetActive(true);

            Vector3 start = boardView.Camera.WorldToScreenPoint(tapWorld);
            t.position = start;
            t.localScale = Vector3.one * 0.6f;
            popup.alpha = 1f;

            Tween.Scale(t, 1f, 0.18f, Ease.OutBack);
            Tween.PositionY(t, start.y + scoreRise * t.lossyScale.y, scoreDuration, Ease.OutQuad);
            Tween.Alpha(popup, 0f, scoreDuration * 0.4f, Ease.InQuad, startDelay: scoreDuration * 0.6f)
                 .OnComplete(popup, self => self.gameObject.SetActive(false));
        }

        private void ShowCombo()
        {
            switch (controller.Config.TierFor(groupSize))
            {
                case BoardConfig.TierC:
                    Combo("Amazing!");
                    Flash();
                    break;
                case BoardConfig.TierB: Combo("Great!"); break;
                case BoardConfig.TierA: Combo("Good!"); break;
            }
        }

        private void Combo(string word)
        {
            Transform t = comboLabel.transform;
            Tween.StopAll(t);
            Tween.StopAll(comboLabel);

            comboLabel.text = word;
            Sfx.Play(SfxId.Combo);
            comboLabel.alpha = 1f;
            comboLabel.gameObject.SetActive(true);

            t.localScale = Vector3.zero;
            t.localRotation = Quaternion.Euler(0f, 0f, -6f);

            Tween.Scale(t, 1f, 0.3f, Ease.OutBack);
            Tween.LocalRotation(t, Vector3.zero, 0.3f, Ease.OutBack);
            Tween.Alpha(comboLabel, 0f, 0.25f, Ease.InQuad, startDelay: 0.3f + comboHold)
                 .OnComplete(comboLabel, self => self.gameObject.SetActive(false));
        }

        // A white wash over everything under the popups. Also the level's cue for a win.
        public void Flash()
        {
            Tween.StopAll(flash);
            SetFlash(flashAlpha);
            Tween.Alpha(flash, 0f, flashDuration, Ease.OutQuad);
        }

        private void SetFlash(float alpha)
        {
            Color c = flash.color;
            c.a = alpha;
            flash.color = c;
        }
    }
}

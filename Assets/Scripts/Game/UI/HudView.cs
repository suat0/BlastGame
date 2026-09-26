using BlastGame.Core;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace BlastGame.Game.UI
{
    // Moves and the objective. Nothing else: the score moved to the end-of-level card, as in the games
    // this one is modelled on, so the only numbers in view during play are the two that decide it.
    //
    // A broken Box flies from the board to the objective, and the count drops when it lands rather
    // than when Core removed it: the number the player reads follows the thing they watched. So the
    // label shows what Core has left plus what is still in the air.
    //
    // Canvas UI here, deliberately not on the board: what the board avoids is the canvas rebuild, and
    // two labels changing once per move are the opposite of a hundred blocks moving every frame.
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private GameController controller;
        [SerializeField] private BoardView boardView;

        [Header("Labels")]
        [SerializeField] private TMP_Text movesLabel;
        [SerializeField] private TMP_Text objectiveLabel;

        [Tooltip("Hidden whole when the level has no objective, so the icon goes with the count.")]
        [SerializeField] private GameObject objectiveGroup;

        [SerializeField] private RectTransform objectiveIcon;

        [Header("Flying Boxes")]
        [Tooltip("Preplaced and hidden. More Boxes broken at once than there are flyers still count; " +
                 "the extra ones just land without flying.")]
        [SerializeField] private RectTransform[] flyers;

        [SerializeField] private float flightDuration = 0.6f;

        [Header("Low moves")]
        [SerializeField] private int lowMovesAt = 5;
        [SerializeField] private Color lowMovesColor = new Color32(0xE5, 0x48, 0x3B, 0xFF);

        private int inFlight;
        private int nextFlyer;
        private Color movesColor;
        private bool lowMoves;

        // Nothing in the air: the level waits on this, as on the board, before its end card.
        public bool IsIdle => inFlight == 0;

        private void Awake()
        {
            movesColor = movesLabel.color;
            foreach (RectTransform flyer in flyers) flyer.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            controller.OnBoardReady += HandleBoardReady;
            controller.OnStatusChanged += HandleStatusChanged;
            boardView.OnBoxBroken += HandleBoxBroken;
        }

        private void OnDisable()
        {
            controller.OnBoardReady -= HandleBoardReady;
            controller.OnStatusChanged -= HandleStatusChanged;
            boardView.OnBoxBroken -= HandleBoxBroken;
        }

        private void OnDestroy()
        {
            Tween.StopAll(this);   // pending landings
            foreach (RectTransform flyer in flyers) Tween.StopAll(flyer);
            Tween.StopAll(objectiveIcon);
            Tween.StopAll(movesLabel.transform);
        }

        // A restart: anything still flying belongs to the board that was just replaced.
        private void HandleBoardReady(Board board)
        {
            Tween.StopAll(this);   // pending landings
            foreach (RectTransform flyer in flyers)
            {
                Tween.StopAll(flyer);
                flyer.gameObject.SetActive(false);
            }

            inFlight = 0;
            SetLowMoves(false);
        }

        private void HandleStatusChanged()
        {
            GameSession session = controller.Session;

            // SetText with an argument, not an interpolated string: TMP formats into a buffer it
            // already owns, so a label that changes every move allocates nothing.
            movesLabel.SetText("{0:0}", session.HasMoveLimit ? session.MovesLeft : session.Moves);

            // The objective is data that can be absent, not a second game mode.
            objectiveGroup.SetActive(session.HasObjective);
            ShowObjective();

            SetLowMoves(session.HasMoveLimit && session.MovesLeft <= lowMovesAt && session.MovesLeft > 0);
        }

        private void ShowObjective()
        {
            if (controller.Session.HasObjective)
                objectiveLabel.SetText("{0:0}", controller.Session.RemainingBoxes + inFlight);
        }

        // Raised during the move, before the status change that updates the label - so the Box is
        // counted as in the air by the time the label is written, and the number does not drop early.
        private void HandleBoxBroken(Vector3 world)
        {
            inFlight++;

            RectTransform flyer = flyers.Length > 0 ? flyers[nextFlyer] : null;
            if (flyer == null || flyer.gameObject.activeSelf)
            {
                Land();   // every flyer busy: count it at once rather than lose it
                return;
            }

            nextFlyer = (nextFlyer + 1) % flyers.Length;

            // An overlay canvas's world space is screen pixels, so the Box's screen position is where
            // its flyer starts.
            Vector3 start = boardView.Camera.WorldToScreenPoint(world);
            Vector3 end = objectiveIcon.position;

            flyer.gameObject.SetActive(true);
            flyer.position = start;
            flyer.localScale = Vector3.one * 1.3f;

            // X eases in and Y eases out, so the straight line between them bends into an arc.
            Tween.PositionX(flyer, end.x, flightDuration, Ease.InQuad);
            Tween.Scale(flyer, 0.8f, flightDuration, Ease.InQuad);
            Tween.PositionY(flyer, end.y, flightDuration, Ease.OutQuad)
                 .OnComplete(flyer, self => self.gameObject.SetActive(false));

            // The landing is timed separately from the flyer's own completion, so the count still
            // drops if the flyer is stopped by a restart - HandleBoardReady then zeroes it anyway.
            Tween.Delay(this, flightDuration, self => self.Land());
        }

        private void Land()
        {
            if (inFlight == 0) return;

            inFlight--;
            ShowObjective();
            Sfx.Play(SfxId.Collect);

            Tween.StopAll(objectiveIcon);
            objectiveIcon.localScale = Vector3.one;
            Tween.PunchScale(objectiveIcon, new Vector3(0.3f, 0.3f, 0f), 0.25f, frequency: 6f);
        }

        // Red and pulsing from the fifth-last move: the one warning the player gets before the end.
        private void SetLowMoves(bool low)
        {
            if (low == lowMoves) return;
            lowMoves = low;

            Transform label = movesLabel.transform;
            Tween.StopAll(label);
            label.localScale = Vector3.one;

            movesLabel.color = low ? lowMovesColor : movesColor;
            if (low) Sfx.Play(SfxId.LowMoves);
            if (low) Tween.Scale(label, 1.15f, 0.35f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
        }
    }
}

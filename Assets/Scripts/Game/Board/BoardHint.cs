using System;
using BlastGame.Core;
using UnityEngine;

namespace BlastGame.Game
{
    // Points at the largest group on the board: found once when the hint starts, then pulsed until the
    // player does anything. Split out of BoardView for the same reason BoardCamera was - it owns state
    // of its own (which cells, how far into the pulse) that nothing else in the view reads.
    //
    // The members are the cells sharing the seed's group id, so Core's own scan answers "which cells"
    // and this class needs no flood fill of its own.
    public sealed class BoardHint
    {
        // The view's array, read here and never written: the hint scales blocks, it does not own them.
        private readonly BlockView[] blockAt;

        private readonly int[] cells;
        private readonly float pulse;
        private readonly float period;

        private int count;
        private float time;

        public BoardHint(BlockView[] blockAt, float pulse, float period)
        {
            this.blockAt = blockAt ?? throw new ArgumentNullException(nameof(blockAt));

            if (period <= 0f)
                throw new ArgumentOutOfRangeException(nameof(period), period, "Period must be positive.");

            this.pulse = pulse;
            this.period = period;

            cells = new int[blockAt.Length];
        }

        public void Show(Board board)
        {
            Hide();

            int seed = -1;
            int largest = 1;
            for (int i = 0; i < board.CellCount; i++)
            {
                if (!board.IsBlastable(i) || board.GroupSizeAt(i) <= largest) continue;

                largest = board.GroupSizeAt(i);
                seed = i;
            }

            if (seed < 0) return;

            int group = board.GroupIdAt(seed);
            for (int i = 0; i < board.CellCount; i++)
                if (board.GroupIdAt(i) == group) cells[count++] = i;

            time = 0f;
        }

        public void Hide()
        {
            SetScale(1f);
            count = 0;
        }

        // A breath rather than a blink: up and back on a cosine, starting from rest.
        public void Tick(float deltaTime)
        {
            if (count == 0) return;

            time += deltaTime;
            SetScale(1f + pulse * (0.5f - 0.5f * Mathf.Cos(time * 2f * Mathf.PI / period)));
        }

        private void SetScale(float scale)
        {
            for (int i = 0; i < count; i++)
            {
                BlockView block = blockAt[cells[i]];
                if (block != null) block.Scale = scale;
            }
        }
    }
}

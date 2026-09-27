using System;

namespace BlastGame.Core
{
    // Everything Core needs to build a board. LevelConfig is a ScriptableObject and cannot cross into
    // Core, so it copies its values into one of these - the single conversion point between the two.
    // Move limit and seed are absent on purpose: neither shapes a board.
    public readonly struct BoardConfig
    {
        public readonly int Rows;
        public readonly int Cols;
        public readonly int ColorCount;

        public readonly int ThresholdA;
        public readonly int ThresholdB;
        public readonly int ThresholdC;

        public readonly int BoxCount;   // a request; generation clamps it to what the board can hold

        // Icon tiers as TierFor returns them: the default icon, then the A, B and C icons.
        public const int TierDefault = 0;
        public const int TierA = 1;
        public const int TierB = 2;
        public const int TierC = 3;

        public BoardConfig(int rows, int cols, int colorCount,
                           int thresholdA, int thresholdB, int thresholdC,
                           int boxCount)
        {
            Rows = rows;
            Cols = cols;
            ColorCount = colorCount;
            ThresholdA = thresholdA;
            ThresholdB = thresholdB;
            ThresholdC = thresholdC;
            BoxCount = boxCount;
        }

        // Every cell but the top row, which never holds a Box - see Board.Generate for why.
        public int BoxCapacity => (Rows - 1) * Cols;

        // What generation actually places. Anything that promises the player a goal before the board
        // exists reads this, not BoxCount.
        public int BoxesToPlace => Math.Min(BoxCount, BoxCapacity);

        // Strictly greater, high to low, so a size equal to a threshold stays on the tier below.
        public int TierFor(int groupSize)
        {
            if (groupSize > ThresholdC) return TierC;
            if (groupSize > ThresholdB) return TierB;
            if (groupSize > ThresholdA) return TierA;
            return TierDefault;
        }

        // Also lives here, not only in LevelConfig.OnValidate, because tests build configs directly.
        public void Validate()
        {
            // The case document's 2-10 range is an authoring constraint enforced in LevelConfig.
            // Core rejects only what it cannot represent.
            if (Rows < 1) throw new ArgumentOutOfRangeException(nameof(Rows), Rows, "Board needs at least one row.");
            if (Cols < 1) throw new ArgumentOutOfRangeException(nameof(Cols), Cols, "Board needs at least one column.");

            if (ColorCount < 1 || ColorCount > 256)
                throw new ArgumentOutOfRangeException(nameof(ColorCount), ColorCount, "Colour count must be in [1, 256].");

            if (BoxCount < 0)
                throw new ArgumentOutOfRangeException(nameof(BoxCount), BoxCount, "Box count cannot be negative.");

            // Tiers are tested C, B, A. With A=7, B=4 a group of 8 would match ">B" first and the
            // first icon would be unreachable, so the thresholds must strictly ascend.
            if (ThresholdA < 1)
                throw new ArgumentOutOfRangeException(nameof(ThresholdA), ThresholdA, "Thresholds must be positive.");
            if (ThresholdB <= ThresholdA || ThresholdC <= ThresholdB)
                throw new ArgumentException(
                    $"Thresholds must strictly ascend, got A={ThresholdA}, B={ThresholdB}, C={ThresholdC}.");
        }
    }
}

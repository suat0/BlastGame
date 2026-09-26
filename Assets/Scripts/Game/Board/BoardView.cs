using System;
using BlastGame.Core;
using UnityEngine;

namespace BlastGame.Game
{
    // Draws a Board: one pooled sprite per non-empty cell. Reads Core, never writes it.
    //
    // One world unit per cell (256px sprites at 256 PPU). The board itself is never scaled - the
    // camera is fitted to it - because transform scale would leak into the input maths and into every
    // fall distance.
    public sealed class BoardView : MonoBehaviour
    {
        public const float CellSize = 1f;

        [Serializable]
        private sealed class ColorSprites
        {
            public Sprite defaultIcon;
            public Sprite iconA;
            public Sprite iconB;
            public Sprite iconC;

            public Sprite ForTier(int tier)
            {
                switch (tier)
                {
                    case 3: return iconC;
                    case 2: return iconB;
                    case 1: return iconA;
                    default: return defaultIcon;
                }
            }
        }

        [Header("Wiring")]
        [SerializeField] private GameController controller;

        [Header("Blocks")]
        [SerializeField] private BlockView blockPrefab;

        [Tooltip("Indexed by Core's colour index. Must cover the level's ColorCount.")]
        [SerializeField] private ColorSprites[] colorSprites;

        [Tooltip("Indexed by damage taken: element 0 is an undamaged Box.")]
        [SerializeField] private Sprite[] boxSprites;

        [Header("Framing")]
        [SerializeField] private Camera boardCamera;

        [Tooltip("The part of the screen the board is fitted into: an invisible rect in the HUD between " +
                 "its top bar and its bottom buttons. Moving it in the UI moves the board with it. " +
                 "Without one, the board is fitted to the whole screen.")]
        [SerializeField] private RectTransform boardArea;

        [Tooltip("World units of empty space around the board.")]
        [SerializeField] private float cameraPadding = 0.5f;

        [Tooltip("Nine-sliced panel drawn behind the grid. Optional, and decorative only - it is " +
                 "sized here because only this class knows how big the board turned out to be.")]
        [SerializeField] private SpriteRenderer boardFrame;

        [SerializeField] private float framePadding = 0.3f;

        [Tooltip("Clips the board's blocks to the board, so new blocks appear from behind the frame " +
                 "rather than in mid-air above it. Scaled to the board here.")]
        [SerializeField] private SpriteMask boardMask;

        [Tooltip("The ornate frame drawn over the board's edge, nine-sliced to fit it.")]
        [SerializeField] private SpriteRenderer boardTrim;

        [Tooltip("How far the trim reaches past the cells, in world units - enough to cover the edge " +
                 "blocks disappear behind.")]
        [SerializeField] private float trimPadding = 0.3f;

        [Tooltip("Drawn under every cell, holes included, so the board keeps its shape when a column " +
                 "under a Box stays empty. From the block atlas, so the tiles join the board's batch.")]
        [SerializeField] private Sprite cellTile;

        [SerializeField] private Color tileLight = new Color32(0x3A, 0x2A, 0x7A, 0xFF);
        [SerializeField] private Color tileDark = new Color32(0x2E, 0x21, 0x66, 0xFF);

        [Tooltip("Below the blocks and above the frame. Only the order changes - the material is the " +
                 "same - so the three layers still draw as one batch.")]
        [SerializeField] private int tileSortingOrder = -5;

        [Header("Motion")]
        [Tooltip("Fall acceleration in cells per second squared. Distance still sets the duration, " +
                 "so a long fall takes longer - it just does not travel at a constant rate.")]
        [SerializeField] private float fallGravity = 55f;

        [Tooltip("Seconds for the whole shuffle: blocks shrink away, the board is redrawn, they grow back.")]
        [SerializeField] private float shuffleDuration = 0.4f;

        [Header("Effects")]
        [Tooltip("Sprites reserved for blast effects. The cap is deliberate: a full board blasting at " +
                 "once would otherwise ask for hundreds. Effects past it are dropped, not queued.")]
        [SerializeField] private int effectCapacity = 128;

        [Tooltip("Seconds a blasted block takes to swell and vanish.")]
        [SerializeField] private float popDuration = 0.16f;

        [SerializeField] private int shardsPerBlock = 3;
        [SerializeField] private int shardsPerBox = 6;
        [SerializeField] private float shardScale = 0.3f;
        [SerializeField] private float shardSpeed = 3.5f;
        [SerializeField] private float shardDuration = 0.45f;

        [Tooltip("How far a landing block flexes, as a fraction of a cell. Cosmetic only: a squashing " +
                 "block is settled, so it stays tappable the frame it lands.")]
        [SerializeField] private float landingSquash = 0.18f;

        [SerializeField] private float landingSquashDuration = 0.12f;

        [Header("Particles")]
        [Tooltip("Optional. Without it the board still pops and throws shards; it just has no sparkle.")]
        [SerializeField] private Vfx vfx;

        [Tooltip("Sparkles per blasted block. Capped by the particle system itself on a full-board blast.")]
        [SerializeField] private int sparklesPerBlock = 2;

        [SerializeField] private Color blastGlow = new Color(1f, 0.93f, 0.7f, 0.9f);

        [Tooltip("Seconds between idle twinkles on a random block, drawn from this range.")]
        [SerializeField] private Vector2 twinkleInterval = new Vector2(1.2f, 2.6f);

        [Header("Feedback")]
        [SerializeField] private float rejectedWiggle = 12f;
        [SerializeField] private float rejectedWiggleDuration = 0.3f;

        [Tooltip("Cells above the board the intro drop starts from, plus this much per column so the " +
                 "columns land one after another rather than as a slab.")]
        [SerializeField] private float dropInHeight = 2f;
        [SerializeField] private float dropInColumnStagger = 0.45f;

        [SerializeField] private float hintPulse = 0.08f;
        [SerializeField] private float hintPeriod = 0.9f;

        [Header("Box hits")]
        [SerializeField] private float boxHitWiggle = 9f;
        [SerializeField] private float boxHitDuration = 0.35f;
        [SerializeField] private int splintersPerHit = 5;
        [SerializeField] private int splintersPerBreak = 12;

        [Header("Camera shake")]
        [Tooltip("Cell fractions the camera swings at the start of a shake.")]
        [SerializeField] private float shakeMagnitude = 0.09f;

        [SerializeField] private float shakeDuration = 0.22f;

        [Tooltip("Blocks removed in one move before the blast alone earns a shake. Shaking on every " +
                 "move would make the whole game feel unsteady; a Box breaking always shakes.")]
        [SerializeField] private int shakeBlastThreshold = 7;

        [Tooltip("Depth offset for effect sprites. Same sorting layer and material as the board, so " +
                 "the batch holds; z alone decides that a shard draws in front of the blocks.")]
        [SerializeField] private float effectDepth = -0.1f;

        // Raised once per Box broken, with where it was. The HUD flies a Box from here to its goal
        // counter. A struct argument and a named listener, so raising it allocates nothing.
        public event Action<Vector3> OnBoxBroken;

        public Camera Camera => boardCamera;

        public Vector3 WorldOfCell(int cell) => CellToWorld(cell);

        private Board board;
        private BlockPool pool;
        private FallAnimator fallAnimator;

        private BlockPool effectPool;
        private EffectRunner effectRunner;

        private BlockView[] movingBlocks;

        private BlockView[] blockAt;

        private SpriteRenderer[] tiles;

        // Centre of cell (0, 0) in world space. Row 0 is the bottom row, as everywhere else.
        private Vector3 origin;

        private float shuffleElapsed = NotShuffling;

        private BoardCamera framing;

        // The framing depends on where the UI puts the board area, which the canvas scaler only knows
        // after its own update. So a bind asks for framing and LateUpdate does it - also whenever the
        // screen size changes, as a resized editor window or a rotation does.
        private bool framingDirty;
        private Vector2Int framedScreen;
        private readonly Vector3[] areaCorners = new Vector3[4];

        private bool shuffleRedrawn;

        private const float NotShuffling = -1f;

        private bool IsShuffling => shuffleElapsed >= 0f;

        // True once every block has landed and no shuffle is playing: the board looks like the board
        // Core already has.
        public bool IsIdle => fallAnimator == null || (fallAnimator.IsIdle && !IsShuffling);

        // OnEnable/OnDisable rather than Start/OnDestroy, and named methods rather than lambdas. An
        // object disabled and re-enabled would otherwise subscribe twice, and a lambda cannot be
        // unsubscribed at all.
        private void OnEnable()
        {
            controller.OnBoardReady += HandleBoardReady;
            controller.OnBoardChanged += HandleBoardChanged;
            controller.OnDeadlockResolved += HandleDeadlockResolved;
            controller.OnTapRejected += HandleTapRejected;
        }

        private void OnDisable()
        {
            controller.OnBoardReady -= HandleBoardReady;
            controller.OnBoardChanged -= HandleBoardChanged;
            controller.OnDeadlockResolved -= HandleDeadlockResolved;
            controller.OnTapRejected -= HandleTapRejected;
        }

        private void HandleBoardReady(Board readyBoard)
        {
            Bind(readyBoard);
            Redraw();
        }

        private void HandleBoardChanged(BlastResult result)
        {
            HideHint();
            ApplyBlast(result);
        }

        // A lone block or a Box shrugs off the tap. Only settled blocks reach here - TryPickCell
        // swallows taps on anything still falling - so the wiggle never fights a fall.
        private void HandleTapRejected(int cell)
        {
            if (blockAt[cell] == null) return;
            effectRunner.Wiggle(blockAt[cell], rejectedWiggle, rejectedWiggleDuration);
        }

        private void HandleDeadlockResolved() => BeginShuffleAnimation();

        public void Bind(Board newBoard)
        {
            board = newBoard ?? throw new ArgumentNullException(nameof(newBoard));

            ValidateSprites();

            origin = transform.position + new Vector3(
                -(board.Cols - 1) * 0.5f * CellSize,
                -(board.Rows - 1) * 0.5f * CellSize,
                0f);

            // Built once. A restart regenerates the same board object, so rebuilding here would strand
            // a board's worth of objects and allocate a second set.
            if (pool == null)
            {
                blockAt = new BlockView[board.CellCount];

                hintCells = new int[board.CellCount];
                hintStack = new int[board.CellCount];
                hintMark = new int[board.CellCount];

                movingBlocks = new BlockView[board.CellCount];

                fallAnimator = new FallAnimator(board.CellCount, fallGravity, HandleBlockLanded);

                // Redraw returns every block before renting any, so the peak is exactly CellCount.
                pool = new BlockPool(blockPrefab, transform, board.CellCount,
                                     boardMask != null ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None);

                // A pool of its own rather than headroom in the board's. ApplyBlast releases the
                // blasted blocks before renting the ones that replace them, so an effect holding one
                // back would starve the board of the slot it is about to need.
                var effectsRoot = new GameObject("Effects").transform;
                effectsRoot.SetParent(transform, false);

                effectPool = new BlockPool(blockPrefab, effectsRoot, effectCapacity);
                effectRunner = new EffectRunner(effectPool, effectCapacity);

                framing = new BoardCamera(boardCamera, cameraPadding, shakeMagnitude, shakeDuration);
            }

            framingDirty = true;
            FitFrame();
            LayTiles();
        }

        // A full rebuild, for the first draw and the post-shuffle redraw. Ordinary moves go through
        // ApplyBlast and touch only what changed.
        public void Redraw()
        {
            if (board == null) throw new InvalidOperationException("Redraw before Bind.");

            fallAnimator.Clear();
            HideHint();

            // Before the blocks are pooled. An effect borrowing one has to give it back at rest, or
            // the next cell to rent it inherits a squashed scale.
            effectRunner.Clear();

            for (int i = 0; i < blockAt.Length; i++)
            {
                if (blockAt[i] == null) continue;

                pool.Return(blockAt[i]);
                blockAt[i] = null;
            }

            for (int i = 0; i < blockAt.Length; i++)
            {
                Sprite sprite = SpriteFor(i);
                if (sprite == null) continue;          // an empty cell: a hole under a Box

                BlockView block = pool.Rent();
                block.Sprite = sprite;
                block.Position = CellToWorld(i);

                blockAt[i] = block;
            }
        }

        // Catches up with a move Core already resolved. result is read here and never kept.
        // Detach before attach, or a block leaving cell 40 and another arriving on it - the same move -
        // would hand one block to two cells.
        public void ApplyBlast(BlastResult result)
        {
            if (board == null) throw new InvalidOperationException("ApplyBlast before Bind.");

            BurstBlast(result);

            ReleaseBlocksAt(result.Removed, shardsPerBlock);

            // A Box takes two moves to break, so its one break is worth more than a colour block's.
            BurstBoxes(result);
            ReleaseBlocksAt(result.BrokenBoxes, shardsPerBox);

            ReadOnlySpan<int> from = result.MoveFrom;
            ReadOnlySpan<int> to = result.MoveTo;

            for (int i = 0; i < from.Length; i++)
            {
                int source = from[i];

                if (result.IsSpawn(source))
                {
                    BlockView spawned = pool.Rent();
                    spawned.Position = CellToWorld(source);

                    movingBlocks[i] = spawned;
                    continue;
                }

                // Still in the air from an earlier move. Cancelling leaves it where it is and the new
                // fall starts from there, so a fast player sees a redirection rather than a jump.
                fallAnimator.Cancel(source);

                movingBlocks[i] = blockAt[source];
                blockAt[source] = null;
            }

            for (int i = 0; i < to.Length; i++)
            {
                int target = to[i];
                BlockView block = movingBlocks[i];

                blockAt[target] = block;
                fallAnimator.Begin(block, block.Position, CellToWorld(target), target);

                movingBlocks[i] = null;   // nothing here outlives the call
            }

            RefreshSprites();

            // A Box break costs two moves to earn, so it always lands. An ordinary blast has to be big
            // before it gets the same treatment.
            if (result.BrokenBoxes.Length > 0) framing.Shake(1f);
            else if (result.Removed.Length >= shakeBlastThreshold) framing.Shake(0.6f);
        }

        // Here because this class owns both halves of the mapping - the layout, and the animator that
        // knows which blocks have landed.
        //
        // The settled filter asks about the tapped cell alone, never the group: landed blocks stay
        // tappable while others fall, so a group with one member mid-air must still blast.
        public bool TryPickCell(Vector3 screenPosition, out int cellIndex)
        {
            cellIndex = -1;
            if (board == null) return false;

            if (IsShuffling) return false;

            Vector3 world = boardCamera.ScreenToWorldPoint(screenPosition);

            // origin is the centre of cell (0,0), so half a cell shifts it to the lower-left corner.
            // Floor rather than a cast, which truncates towards zero and folds -0.4 onto cell 0.
            int col = Mathf.FloorToInt((world.x - origin.x) / CellSize + 0.5f);
            int row = Mathf.FloorToInt((world.y - origin.y) / CellSize + 0.5f);

            if (row < 0 || row >= board.Rows || col < 0 || col >= board.Cols) return false;

            int candidate = row * board.Cols + col;
            if (!fallAnimator.IsSettled(candidate)) return false;

            cellIndex = candidate;
            return true;
        }

        // The single per-frame loop for the whole board. No block has an Update of its own.
        private void LateUpdate()
        {
            if (board == null) return;

            if (framingDirty || Screen.width != framedScreen.x || Screen.height != framedScreen.y)
                Reframe();
        }

        public void Reframe()
        {
            framingDirty = false;
            framedScreen = new Vector2Int(Screen.width, Screen.height);

            framing.Frame(board.Rows, board.Cols, CellSize, transform.position, BoardAreaOnScreen());
        }

        // The area's rectangle in screen pixels. Asked of the canvas's camera, so it holds for an
        // overlay canvas and a camera-space one alike.
        private Rect BoardAreaOnScreen()
        {
            if (boardArea == null) return default;

            Canvas canvas = boardArea.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            boardArea.GetWorldCorners(areaCorners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCamera, areaCorners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(uiCamera, areaCorners[2]);

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void Update()
        {
            if (fallAnimator == null) return;   // before Bind

            float deltaTime = Time.deltaTime;

            fallAnimator.Tick(deltaTime);
            effectRunner.Tick(deltaTime);
            TickShuffleAnimation(deltaTime);
            TickTwinkle(deltaTime);
            TickHint(deltaTime);
            framing.Tick(deltaTime);
        }

        // Particles for a blast, read before the blocks are released: a glow where the tap landed,
        // sized by the group, and sparkles from every block in it.
        private void BurstBlast(BlastResult result)
        {
            if (vfx == null) return;

            ReadOnlySpan<int> removed = result.Removed;
            for (int i = 0; i < removed.Length; i++)
                vfx.Sparkles(CellToWorld(removed[i]), sparklesPerBlock);

            float size = 1.4f + 0.18f * result.BlastedGroupSize;
            vfx.Glow(CellToWorld(result.TappedIndex), size, blastGlow);
        }

        // A hit Box rocks and throws splinters; a broken one throws more and leaves dust. The damaged
        // list holds survivors only, so no Box gets both.
        private void BurstBoxes(BlastResult result)
        {
            ReadOnlySpan<int> damaged = result.DamagedBoxes;
            for (int i = 0; i < damaged.Length; i++)
            {
                int cell = damaged[i];

                effectRunner.Wiggle(blockAt[cell], boxHitWiggle, boxHitDuration);
                if (vfx != null) vfx.Splinters(CellToWorld(cell), splintersPerHit);
            }

            ReadOnlySpan<int> broken = result.BrokenBoxes;
            for (int i = 0; i < broken.Length; i++)
            {
                Vector3 where = CellToWorld(broken[i]);
                OnBoxBroken?.Invoke(where);

                if (vfx == null) continue;

                vfx.Splinters(where, splintersPerBreak);
                vfx.Dust(where, 3);
            }
        }

        // The level's entrance: every block starts above the board and falls into its cell through the
        // same animator a move uses, landing squash included. A column starts a little higher than the
        // one to its left, and a longer fall takes longer, so the board fills left to right without a
        // delay mechanism of its own.
        public void DropIn()
        {
            if (board == null) return;

            HideHint();

            float rise = board.Rows * CellSize + dropInHeight;

            for (int i = 0; i < blockAt.Length; i++)
            {
                BlockView block = blockAt[i];
                if (block == null) continue;

                fallAnimator.Cancel(i);
                effectRunner.Cancel(block);

                int col = i % board.Cols;
                Vector3 to = CellToWorld(i);
                Vector3 from = to + new Vector3(0f, rise + col * dropInColumnStagger, 0f);

                block.Position = from;
                fallAnimator.Begin(block, from, to, i);
            }
        }

        // --- hint -----------------------------------------------------------------------------

        // The largest group on the board, found once when the hint starts and then pulsed until the
        // player does anything. Found by the view rather than asked of Core: Core knows each cell's
        // group size, and a flood fill over same-coloured neighbours recovers the members with
        // arrays allocated at bind time.
        private int[] hintCells;
        private int[] hintStack;
        private int[] hintMark;
        private int hintStamp;
        private int hintCount;
        private float hintTime;

        public void ShowHint()
        {
            if (board == null || IsShuffling) return;

            HideHint();

            int seed = -1;
            int largest = 1;
            for (int i = 0; i < board.CellCount; i++)
            {
                if (!board.IsBlastable(i) || board.GroupSizeAt(i) <= largest) continue;

                largest = board.GroupSizeAt(i);
                seed = i;
            }

            if (seed < 0) return;

            byte color = board.CellAt(seed).Color;
            hintStamp++;

            int top = 0;
            hintStack[top++] = seed;
            hintMark[seed] = hintStamp;

            while (top > 0)
            {
                int cell = hintStack[--top];
                hintCells[hintCount++] = cell;

                for (int direction = 0; direction < BlastGame.Core.Grid.DirectionCount; direction++)
                {
                    if (!board.TryNeighbor(cell, direction, out int next)) continue;
                    if (hintMark[next] == hintStamp) continue;

                    Cell neighbour = board.CellAt(next);
                    if (!neighbour.IsColor || neighbour.Color != color) continue;

                    hintMark[next] = hintStamp;
                    hintStack[top++] = next;
                }
            }

            hintTime = 0f;
        }

        public void HideHint()
        {
            for (int i = 0; i < hintCount; i++)
            {
                BlockView block = blockAt[hintCells[i]];
                if (block != null) block.Scale = 1f;
            }

            hintCount = 0;
        }

        // A breath rather than a blink: up and back on a cosine, starting from rest.
        private void TickHint(float deltaTime)
        {
            if (hintCount == 0) return;

            hintTime += deltaTime;
            float scale = 1f + hintPulse * (0.5f - 0.5f * Mathf.Cos(hintTime * 2f * Mathf.PI / hintPeriod));

            for (int i = 0; i < hintCount; i++)
            {
                BlockView block = blockAt[hintCells[i]];
                if (block != null) block.Scale = scale;
            }
        }

        // A fountain of confetti up from the middle of the board, for a win.
        public void Celebrate()
        {
            if (vfx == null || board == null) return;

            vfx.Confetti(transform.position - new Vector3(0f, board.Rows * 0.25f * CellSize, 0f), 160);
        }

        private float twinkleIn = 1f;

        // Now and then a glint on a random block, so a board nobody is touching still looks alive.
        // Only on settled colour blocks, and never during a shuffle.
        private void TickTwinkle(float deltaTime)
        {
            if (vfx == null || IsShuffling) return;

            twinkleIn -= deltaTime;
            if (twinkleIn > 0f) return;

            twinkleIn = UnityEngine.Random.Range(twinkleInterval.x, twinkleInterval.y);

            int cell = UnityEngine.Random.Range(0, board.CellCount);
            if (blockAt[cell] == null || !board.CellAt(cell).IsColor || !fallAnimator.IsSettled(cell)) return;

            // The upper-right corner, where the block art catches its highlight.
            vfx.Sparkles(CellToWorld(cell) + new Vector3(0.28f, 0.28f, 0f), 1);
        }

        // A shuffle swaps colour values rather than moving blocks, so without feedback the whole board
        // would change identity between two frames and read as a glitch. Shrink-and-grow carries the
        // same information as flying each colour to its cell, for a tenth of the work.
        //
        // Scale is written per block, never on this transform - a scaled parent would stop one world
        // unit meaning one cell, and a child under a zero-scaled parent is a division by zero.
        private void BeginShuffleAnimation()
        {
            HideHint();

            // Landing squashes from the move that caused the shuffle are still running, and the
            // shuffle is about to take over every block's scale.
            effectRunner.CancelBorrowed();

            shuffleElapsed = 0f;
            shuffleRedrawn = false;
        }

        private void TickShuffleAnimation(float deltaTime)
        {
            if (!IsShuffling) return;

            shuffleElapsed += deltaTime;
            float t = shuffleElapsed / shuffleDuration;

            if (t >= 1f)
            {
                if (!shuffleRedrawn) Redraw();   // a duration short enough to skip the midpoint frame

                shuffleElapsed = NotShuffling;
                SetAllBlockScales(1f);
                return;
            }

            // Swapped while nothing is on screen, which also hides Redraw clearing the fall animator.
            if (t >= 0.5f && !shuffleRedrawn)
            {
                Redraw();
                shuffleRedrawn = true;
            }

            // 1 at both ends and 0 in the middle - one expression for shrink-then-grow, no phase flag.
            SetAllBlockScales(Easing.SmoothStep(Mathf.Abs(1f - 2f * t)));
        }

        // The animator has already released the cell, so the block is settled and tappable while this
        // runs. Impact feedback must never cost a tap.
        private void HandleBlockLanded(BlockView block) =>
            effectRunner.Squash(block, landingSquash, landingSquashDuration);

        private void SetAllBlockScales(float scale)
        {
            for (int i = 0; i < blockAt.Length; i++)
            {
                if (blockAt[i] == null) continue;

                blockAt[i].Scale = scale;
            }
        }

        // The effect copies the sprite and position and runs on a sprite of its own, so the board's
        // block is free immediately and nothing downstream waits for an animation.
        private void ReleaseBlocksAt(ReadOnlySpan<int> cells, int shardCount)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                int cell = cells[i];

                BlockView block = blockAt[cell];
                if (block == null) continue;

                Vector3 where = block.Position;
                where.z += effectDepth;

                effectRunner.Pop(block.Sprite, where, popDuration);
                effectRunner.Shards(block.Sprite, where, shardCount, shardScale, shardSpeed, shardDuration);

                // A block blasted within a squash of landing still has an effect writing its scale.
                effectRunner.Cancel(block);

                fallAnimator.Cancel(cell);
                pool.Return(block);
                blockAt[cell] = null;
            }
        }

        // Every block, not only the ones that moved. A group growing several columns away changes the
        // icon tier of blocks that did not move, so the move list cannot answer "what changed".
        // Damaged Boxes need no case of their own - the same lookup returns the cracked sprite.
        private void RefreshSprites()
        {
            for (int i = 0; i < blockAt.Length; i++)
            {
                if (blockAt[i] == null) continue;

                blockAt[i].Sprite = SpriteFor(i);
            }
        }

        // Valid for rows above the board, which is where new blocks start.
        public Vector3 CellToWorld(int index)
        {
            int row = index / board.Cols;
            int col = index - row * board.Cols;

            return origin + new Vector3(col * CellSize, row * CellSize, 0f);
        }

        // Tier is asked of Core per cell rather than cached: three comparisons over data Core already
        // has, where a local copy would go stale silently, as a wrong sprite.
        private Sprite SpriteFor(int index)
        {
            Cell cell = board.CellAt(index);

            if (cell.IsBox) return boxSprites[Cell.BoxMaxHealth - cell.Health];
            if (!cell.IsColor) return null;

            return colorSprites[cell.Color].ForTier(board.TierAt(index));
        }

        // Created with the pool and only repositioned after that. A checkerboard, as boards in the
        // genre are, so the eye can count columns without the grid lines a flat well would need.
        private void LayTiles()
        {
            if (cellTile == null) return;

            if (tiles == null)
            {
                var root = new GameObject("Tiles").transform;
                root.SetParent(transform, false);

                tiles = new SpriteRenderer[board.CellCount];
                for (int i = 0; i < tiles.Length; i++)
                {
                    var tile = new GameObject("Tile", typeof(SpriteRenderer));
                    tile.transform.SetParent(root, false);

                    tiles[i] = tile.GetComponent<SpriteRenderer>();
                    tiles[i].sprite = cellTile;
                    tiles[i].sortingOrder = tileSortingOrder;
                }
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                int row = i / board.Cols;
                int col = i - row * board.Cols;

                tiles[i].transform.position = CellToWorld(i);
                tiles[i].color = (row + col) % 2 == 0 ? tileLight : tileDark;
            }
        }

        // Sized from the board rather than authored in the scene, so a 2x2 level and a 10x10 level
        // both get a frame that fits without anyone remembering to resize it.
        private void FitFrame()
        {
            Vector2 cells = new Vector2(board.Cols * CellSize, board.Rows * CellSize);

            if (boardMask != null)
            {
                boardMask.transform.position = transform.position;
                boardMask.transform.localScale = new Vector3(cells.x, cells.y, 1f);   // a one-unit square
            }

            if (boardTrim != null)
            {
                boardTrim.transform.position = transform.position;
                boardTrim.size = cells + Vector2.one * (trimPadding * 2f);
            }

            if (boardFrame == null) return;

            boardFrame.transform.position = transform.position;
            boardFrame.size = new Vector2(
                board.Cols * CellSize + framePadding * 2f,
                board.Rows * CellSize + framePadding * 2f);
        }

        // Checked once, at bind time. An unassigned sprite otherwise surfaces as an invisible block or
        // a null reference from inside the draw loop, naming no colour.
        private void ValidateSprites()
        {
            if (blockPrefab == null) throw new InvalidOperationException($"{name}: block prefab is not assigned.");
            if (boardCamera == null) throw new InvalidOperationException($"{name}: board camera is not assigned.");

            if (boxSprites == null || boxSprites.Length != Cell.BoxMaxHealth)
                throw new InvalidOperationException(
                    $"{name}: needs exactly {Cell.BoxMaxHealth} Box sprites, one per damage level.");

            // The board is the authority on how many colours are in play: a four-colour level must not
            // be rejected for having six sprite slots, or accepted with only three filled.
            int highestColor = -1;
            for (int i = 0; i < board.CellCount; i++)
            {
                Cell cell = board.CellAt(i);
                if (cell.IsColor && cell.Color > highestColor) highestColor = cell.Color;
            }

            if (colorSprites == null || highestColor >= colorSprites.Length)
                throw new InvalidOperationException(
                    $"{name}: board uses colour index {highestColor} but only {colorSprites?.Length ?? 0} " +
                    "colour sprite sets are assigned.");

            for (int color = 0; color <= highestColor; color++)
            {
                ColorSprites set = colorSprites[color];

                if (set == null || set.defaultIcon == null || set.iconA == null || set.iconB == null || set.iconC == null)
                    throw new InvalidOperationException($"{name}: colour {color} is missing one of its four sprites.");
            }
        }
    }
}

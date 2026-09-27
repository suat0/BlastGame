using BlastGame.Core;
using UnityEngine;

namespace BlastGame.Game
{
    // Everything drawn around and under the blocks: the checkerboard tiles, the panel behind the grid,
    // the trim over its edge and the mask that clips falling blocks. Sized from the board rather than
    // authored in the scene, so a 2x2 level and a 10x10 level both fit without anyone resizing them.
    //
    // Decorative only - every part is optional, and a board without any of them still plays.
    public sealed class BoardDecor
    {
        private readonly Transform root;

        private readonly SpriteRenderer frame;
        private readonly float framePadding;

        private readonly SpriteMask mask;

        private readonly SpriteRenderer trim;
        private readonly float trimPadding;

        private readonly Sprite cellTile;
        private readonly Color tileLight;
        private readonly Color tileDark;
        private readonly int tileSortingOrder;

        private SpriteRenderer[] tiles;

        public BoardDecor(Transform root,
                          SpriteRenderer frame, float framePadding,
                          SpriteMask mask,
                          SpriteRenderer trim, float trimPadding,
                          Sprite cellTile, Color tileLight, Color tileDark, int tileSortingOrder)
        {
            this.root = root;
            this.frame = frame;
            this.framePadding = framePadding;
            this.mask = mask;
            this.trim = trim;
            this.trimPadding = trimPadding;
            this.cellTile = cellTile;
            this.tileLight = tileLight;
            this.tileDark = tileDark;
            this.tileSortingOrder = tileSortingOrder;
        }

        // origin is the centre of cell (0, 0); center the centre of the whole board.
        public void Fit(Board board, Vector3 center, Vector3 origin, float cellSize)
        {
            Vector2 cells = new Vector2(board.Cols * cellSize, board.Rows * cellSize);

            if (mask != null)
            {
                mask.transform.position = center;
                mask.transform.localScale = new Vector3(cells.x, cells.y, 1f);   // a one-unit square
            }

            FitSliced(frame, center, cells, framePadding);
            FitSliced(trim, center, cells, trimPadding);

            LayTiles(board, origin, cellSize);
        }

        private static void FitSliced(SpriteRenderer sliced, Vector3 center, Vector2 cells, float padding)
        {
            if (sliced == null) return;

            sliced.transform.position = center;
            sliced.size = cells + Vector2.one * (padding * 2f);
        }

        // Created on the first fit and only repositioned after that. A checkerboard, as boards in the
        // genre are, so the eye can count columns without the grid lines a flat well would need.
        private void LayTiles(Board board, Vector3 origin, float cellSize)
        {
            if (cellTile == null) return;

            if (tiles == null)
            {
                var tileRoot = new GameObject("Tiles").transform;
                tileRoot.SetParent(root, false);

                tiles = new SpriteRenderer[board.CellCount];
                for (int i = 0; i < tiles.Length; i++)
                {
                    var tile = new GameObject("Tile", typeof(SpriteRenderer));
                    tile.transform.SetParent(tileRoot, false);

                    tiles[i] = tile.GetComponent<SpriteRenderer>();
                    tiles[i].sprite = cellTile;
                    tiles[i].sortingOrder = tileSortingOrder;
                }
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                int row = board.RowOf(i);
                int col = board.ColOf(i);

                tiles[i].transform.position = origin + new Vector3(col * cellSize, row * cellSize, 0f);
                tiles[i].color = (row + col) % 2 == 0 ? tileLight : tileDark;
            }
        }
    }
}

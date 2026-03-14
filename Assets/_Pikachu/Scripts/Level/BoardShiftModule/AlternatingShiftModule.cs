namespace CahtFramework.Pikachu
{
    using System;
    using UnityEngine;

    public enum AlternatingMode
    {
        RowsLeftRight,
        ColumnsUpDown
    }

    [Serializable]
    public class AlternatingShiftModule : BoardShiftModule
    {
        public AlternatingMode mode = AlternatingMode.RowsLeftRight;

        public override bool ApplyShift(Node[,] grid, int width, int height)
        {
            var hasChanged = false;

            if (this.mode == AlternatingMode.RowsLeftRight)
                for (var y = 0; y < height; y++)
                    if (y % 2 == 0)
                    {
                        var writeX = 0;
                        for (var x = 0; x < width; x++)
                            if (!grid[x, y].IsEmpty)
                            {
                                if (writeX != x)
                                {
                                    grid[writeX, y].SetPiece(grid[x, y].Piece);
                                    grid[x, y].ClearPiece();
                                    hasChanged = true;
                                }

                                writeX++;
                            }
                    }
                    else
                    {
                        var writeX = width - 1;
                        for (var x = width - 1; x >= 0; x--)
                            if (!grid[x, y].IsEmpty)
                            {
                                if (writeX != x)
                                {
                                    grid[writeX, y].SetPiece(grid[x, y].Piece);
                                    grid[x, y].ClearPiece();
                                    hasChanged = true;
                                }

                                writeX--;
                            }
                    }

            if (hasChanged) Debug.Log($"[BoardShiftModule] Alternating shift executed: {this.mode}");

            return hasChanged;
        }

        public override object Clone() { return new AlternatingShiftModule { mode = this.mode }; }
    }
}
namespace CahtFramework.Pikachu
{
    using System;
    using UnityEngine;

    public enum CenterEdgeMode
    {
        ToCenterHorizontal,
        ToCenterVertical,
        ToEdgesHorizontal,
        ToEdgesVertical
    }

    [Serializable]
    public class CenterEdgeShiftModule : BoardShiftModule
    {
        public CenterEdgeMode mode = CenterEdgeMode.ToCenterHorizontal;

        public override bool ApplyShift(Node[,] grid, int width, int height)
        {
            var hasChanged = false;

            if (this.mode == CenterEdgeMode.ToCenterHorizontal)
            {
                var mid = width / 2;
                for (var y = 0; y < height; y++)
                {
                    var writeX = mid - 1;
                    for (var x = mid - 1; x >= 0; x--)
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

                    writeX = mid;
                    for (var x = mid; x < width; x++)
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
            }
            else if (this.mode == CenterEdgeMode.ToEdgesHorizontal)
            {
                var mid = width / 2;
                for (var y = 0; y < height; y++)
                {
                    var writeX = 0;
                    for (var x = 0; x < mid; x++)
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

                    writeX = width - 1;
                    for (var x = width - 1; x >= mid; x--)
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
            }

            if (hasChanged) Debug.Log($"[BoardShiftModule] Shifted pieces using mode: {this.mode}.");

            return hasChanged;
        }

        public override object Clone() { return new CenterEdgeShiftModule { mode = this.mode }; }
    }
}
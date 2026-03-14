namespace CahtFramework.Pikachu
{
    using System;
    using UnityEngine;

    public enum ShiftDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    [Serializable]
    public class DirectionalShiftModule : BoardShiftModule
    {
        public ShiftDirection direction = ShiftDirection.Down;

        public override bool ApplyShift(Node[,] grid, int width, int height)
        {
            var hasChanged = false;

            if (this.direction == ShiftDirection.Left)
                for (var y = 0; y < height; y++)
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
            else if (this.direction == ShiftDirection.Right)
                for (var y = 0; y < height; y++)
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
            else if (this.direction == ShiftDirection.Down)
                for (var x = 0; x < width; x++)
                {
                    var writeY = 0;
                    for (var y = 0; y < height; y++)
                        if (!grid[x, y].IsEmpty)
                        {
                            if (writeY != y)
                            {
                                grid[x, writeY].SetPiece(grid[x, y].Piece);
                                grid[x, y].ClearPiece();
                                hasChanged = true;
                            }

                            writeY++;
                        }
                }
            else if (this.direction == ShiftDirection.Up)
                for (var x = 0; x < width; x++)
                {
                    var writeY = height - 1;
                    for (var y = height - 1; y >= 0; y--)
                        if (!grid[x, y].IsEmpty)
                        {
                            if (writeY != y)
                            {
                                grid[x, writeY].SetPiece(grid[x, y].Piece);
                                grid[x, y].ClearPiece();
                                hasChanged = true;
                            }

                            writeY--;
                        }
                }

            if (hasChanged) Debug.Log($"[BoardShiftModule] Shifted pieces to {this.direction}.");

            return hasChanged;
        }

        public override object Clone() { return new DirectionalShiftModule { direction = this.direction }; }
    }
}
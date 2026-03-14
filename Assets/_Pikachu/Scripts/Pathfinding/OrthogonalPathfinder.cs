namespace CahtFramework.Pikachu
{
    using System.Collections.Generic;
    using UnityEngine;

    public class OrthogonalPathfinder : IPathfinding
    {
        private Node[,] grid;
        private int     cols;
        private int     rows;

        private List<Vector2Int> cachedPath = new List<Vector2Int>(4);

        public List<Vector2Int> FindPath(Node[,] grid, Node start, Node end)
        {
            this.grid = grid;
            this.cols = grid.GetLength(0);
            this.rows = grid.GetLength(1);

            this.cachedPath.Clear(); 

            if (this.CheckLine(start.X, start.Y, end.X, end.Y))
            {
                this.cachedPath.Add(new Vector2Int(start.X, start.Y));
                this.cachedPath.Add(new Vector2Int(end.X, end.Y));
                return this.cachedPath;
            }

            if (this.CheckLShape(start, end)) return this.cachedPath;

            if (this.CheckZShape(start, end)) return this.cachedPath;

            return null;
        }

        private bool CheckLShape(Node start, Node end)
        {
            if (this.IsEmpty(start.X, end.Y) && this.CheckLine(start.X, start.Y, start.X, end.Y) && this.CheckLine(start.X, end.Y, end.X, end.Y))
            {
                this.BuildPath(start, new Vector2Int(start.X, end.Y), end);
                return true;
            }
            if (this.IsEmpty(end.X, start.Y) && this.CheckLine(start.X, start.Y, end.X, start.Y) && this.CheckLine(end.X, start.Y, end.X, end.Y))
            {
                this.BuildPath(start, new Vector2Int(end.X, start.Y), end);
                return true;
            }

            return false;
        }

        private bool CheckZShape(Node start, Node end)
        {
            for (var dir = -1; dir <= 1; dir += 2)
            {
                var y = start.Y + dir;
                while (y >= -1 && y <= this.rows && this.IsEmpty(start.X, y))
                {
                    if (this.IsEmpty(end.X, y) && this.CheckLine(start.X, y, end.X, y) && this.CheckLine(end.X, y, end.X, end.Y))
                    {
                        this.BuildPath(start, new Vector2Int(start.X, y), new Vector2Int(end.X, y), end);
                        return true;
                    }
                    y += dir;
                }
            }

            for (var dir = -1; dir <= 1; dir += 2)
            {
                var x = start.X + dir;
                while (x >= -1 && x <= this.cols && this.IsEmpty(x, start.Y))
                {
                    if (this.IsEmpty(x, end.Y) && this.CheckLine(x, start.Y, x, end.Y) && this.CheckLine(x, end.Y, end.X, end.Y))
                    {
                        this.BuildPath(start, new Vector2Int(x, start.Y), new Vector2Int(x, end.Y), end);
                        return true;
                    }
                    x += dir;
                }
            }

            return false;
        }


        private void BuildPath(Node start, Vector2Int corner, Node end)
        {
            this.cachedPath.Add(new Vector2Int(start.X, start.Y));
            this.cachedPath.Add(corner);
            this.cachedPath.Add(new Vector2Int(end.X, end.Y));
        }

        private void BuildPath(Node start, Vector2Int corner1, Vector2Int corner2, Node end)
        {
            this.cachedPath.Add(new Vector2Int(start.X, start.Y));
            this.cachedPath.Add(corner1);
            this.cachedPath.Add(corner2);
            this.cachedPath.Add(new Vector2Int(end.X, end.Y));
        }

        private bool CheckLine(int x1, int y1, int x2, int y2)
        {
            if (x1 == x2)
            {
                var min = Mathf.Min(y1, y2);
                var max = Mathf.Max(y1, y2);
                for (var y = min + 1; y < max; y++)
                    if (!this.IsEmpty(x1, y)) return false;
                return true;
            }
            if (y1 == y2)
            {
                var min = Mathf.Min(x1, x2);
                var max = Mathf.Max(x1, x2);
                for (var x = min + 1; x < max; x++)
                    if (!this.IsEmpty(x, y1)) return false;
                return true;
            }
            return false;
        }

        private bool IsEmpty(int x, int y)
        {
            if (x < 0 || x >= this.cols || y < 0 || y >= this.rows) return true;
            return this.grid[x, y].IsEmpty;
        }
    }
}
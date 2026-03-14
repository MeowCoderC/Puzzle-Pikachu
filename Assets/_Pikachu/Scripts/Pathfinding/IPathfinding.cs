namespace CahtFramework.Pikachu
{
    using System.Collections.Generic;
    using UnityEngine;

    public interface IPathfinding
    {
        List<Vector2Int> FindPath(Node[,] grid, Node start, Node end);
    }
}
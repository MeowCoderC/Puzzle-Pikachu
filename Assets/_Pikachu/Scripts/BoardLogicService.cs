namespace CahtFramework.Pikachu
{
    using System.Collections.Generic;
    using UnityEngine;

    public class NodePair
    {
        public Node node1;
        public Node node2;
    }

    public class BoardLogicService
    {
        private readonly IPathfinding pathfinder;

        public BoardLogicService(IPathfinding pathfinder)
        {
            this.pathfinder = pathfinder;
        }

        public List<Vector2Int> CheckMatchAndGetPath(BoardData data, Node first, Node second)
        {
            if (first.Piece.ID != second.Piece.ID) return null;
            return this.pathfinder.FindPath(data.Grid, first, second);
        }

        public NodePair FindValidMatch(BoardData data)
        {
            Dictionary<int, List<Node>> nodesByID = new();
            
            for (int x = 0; x < data.Columns; x++)
            {
                for (int y = 0; y < data.Rows; y++)
                {
                    Node node = data.Grid[x, y];
                    if (node != null && !node.IsEmpty)
                    {
                        int id = node.Piece.ID;
                        if (!nodesByID.ContainsKey(id)) nodesByID[id] = new List<Node>();
                        nodesByID[id].Add(node);
                    }
                }
            }

            foreach (var kvp in nodesByID)
            {
                var group = kvp.Value;
                if (group.Count < 2) continue; 

                for (int i = 0; i < group.Count; i++)
                {
                    for (int j = i + 1; j < group.Count; j++)
                    {
                        if (this.pathfinder.FindPath(data.Grid, group[i], group[j]) != null)
                            return new NodePair { node1 = group[i], node2 = group[j] };
                    }
                }
            }
            return null;
        }

        public bool HasAnyValidMatch(BoardData data)
        {
            return this.FindValidMatch(data) != null;
        }
    }
}
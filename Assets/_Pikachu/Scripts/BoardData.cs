namespace CahtFramework.Pikachu
{
    using System.Collections.Generic;

    public class BoardData
    {
        public int     Columns { get; private set; }
        public int     Rows    { get; private set; }
        public Node[,] Grid    { get; private set; }

        private readonly Dictionary<Node, NodeView> nodeToView = new();
        private readonly Dictionary<NodeView, Node> viewToNode = new();

        public void Initialize(int columns, int rows)
        {
            this.Columns = columns;
            this.Rows    = rows;
            this.Grid    = new Node[this.Columns, this.Rows];
        }

        public void RegisterNode(Node node, NodeView view)
        {
            this.Grid[node.X, node.Y] = node;
            this.nodeToView[node]     = view;
            this.viewToNode[view]     = node;
        }
        
        public Node GetNode(NodeView view)
        {
            return this.viewToNode.TryGetValue(view, out var node) ? node : null;
        }

        public NodeView GetView(Node node)
        {
            return this.nodeToView.TryGetValue(node, out var view) ? view : null;
        }

        public List<NodeView> GetAllViews()
        {
            return new List<NodeView>(this.nodeToView.Values);
        }

        public void Clear()
        {
            this.nodeToView.Clear();
            this.viewToNode.Clear();
            this.Grid    = null;
            this.Columns = 0;
            this.Rows    = 0;
        }
    }
}
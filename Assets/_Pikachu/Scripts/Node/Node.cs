namespace CahtFramework.Pikachu
{
    public class Node
    {
        public int       X     { get; private set; }
        public int       Y     { get; private set; }
        public PieceData Piece { get; private set; }

        public bool IsEmpty => this.Piece == null;

        public Node(int x, int y)
        {
            this.X     = x;
            this.Y     = y;
            this.Piece = null;
        }

        public void SetPiece(PieceData pieceData) { this.Piece = pieceData; }

        public void ClearPiece() { this.Piece = null; }
    }
}
namespace CahtFramework.Pikachu
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public abstract class PieceGeneratorModule : ICloneable
    {
        public abstract List<PieceData> GeneratePieces(int totalNodes, List<PieceData> availablePieces);

        public abstract object Clone();
    }
}
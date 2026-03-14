namespace CahtFramework.Pikachu
{
    using System.Collections.Generic;
    using UnityEngine;

    public class LevelData : IdentifiedObject
    {
        [UnderlineTitle("Board Configuration", 5)] [Min(2)]
        public int columns = 16;

        [Min(2)] public int rows = 9;

        [UnderlineTitle("Piece Settings", 10)] public List<PieceData> availablePieces = new();

        [UnderlineTitle("Modules & Mechanics", 10)] [SerializeReference] [SubclassSelector]
        public PieceGeneratorModule generatorModule;

        [SerializeReference] [SubclassSelector]
        public BoardEntranceModule entranceModule;

        [SerializeReference] [SubclassSelector]
        public BoardShiftModule shiftModule;

        public bool HasGeneratorModule => this.generatorModule != null;
        public bool HasEntranceModule  => this.entranceModule != null;
        public bool HasShiftModule     => this.shiftModule != null;

        public override object Clone() { return CreateInstance<LevelData>(); }
    }
}
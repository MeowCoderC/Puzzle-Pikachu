namespace CahtFramework.Pikachu
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Random = UnityEngine.Random;

    [Serializable]
    public class FisherYatesPairedGenerator : PieceGeneratorModule
    {
        public override List<PieceData> GeneratePieces(int totalNodes, List<PieceData> availablePieces)
        {
            var pool = new List<PieceData>();

            if (availablePieces == null || availablePieces.Count == 0)
            {
                Debug.LogWarning("[PieceGenerator] Available pieces list is empty! Returning empty pool.");

                return pool;
            }

            var pairsNeeded = totalNodes / 2;

            for (var i = 0; i < pairsNeeded; i++)
            {
                var randomPiece = availablePieces[Random.Range(0, availablePieces.Count)];
                pool.Add(randomPiece);
                pool.Add(randomPiece);
            }

            for (var i = 0; i < pool.Count; i++)
            {
                var temp        = pool[i];
                var randomIndex = Random.Range(i, pool.Count);
                pool[i]           = pool[randomIndex];
                pool[randomIndex] = temp;
            }

            return pool;
        }

        public override object Clone() { return new FisherYatesPairedGenerator(); }
    }
}
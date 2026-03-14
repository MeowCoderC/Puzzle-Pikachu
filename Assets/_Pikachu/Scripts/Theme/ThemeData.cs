namespace CahtFramework.Pikachu
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class ThemeData : IdentifiedObject
    {
        [Serializable]
        public class PieceSkin
        {
            public PieceData PieceData;
            public Sprite    SkinSprite;
        }

        [SerializeField] private Sprite          backgroundSprite;
        [SerializeField] private AudioClip       backgroundMusic;
        [SerializeField] private List<PieceSkin> pieceSkins = new();

        public Sprite                   BackgroundSprite => this.backgroundSprite;
        public AudioClip                BackgroundMusic  => this.backgroundMusic;
        public IReadOnlyList<PieceSkin> PieceSkins       => this.pieceSkins;

        public Sprite GetSkinForPiece(PieceData pieceData)
        {
            if (pieceData == null) return null;

            var skin = this.pieceSkins.Find(s => s.PieceData != null && s.PieceData.ID == pieceData.ID);

            if (skin != null && skin.SkinSprite != null)
                return skin.SkinSprite;

            return pieceData.Icon;
        }

        public void LoadSkinsFromList(List<PieceData> allPieces, List<Sprite> allSprites)
        {
            this.pieceSkins.Clear();

            for (var i = 0; i < allPieces.Count; i++)
            {
                var    piece = allPieces[i];
                Sprite skin  = null;

                if (i < allSprites.Count) skin = allSprites[i];

                this.pieceSkins.Add(new PieceSkin { PieceData = piece, SkinSprite = skin });
            }
        }
    }
}
namespace CahtFramework.Pikachu
{
    using System;

    [Serializable]
    public abstract class BoardShiftModule : ICloneable
    {
        public abstract bool ApplyShift(Node[,] grid, int width, int height);

        public abstract object Clone();
    }
}
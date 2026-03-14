namespace CahtFramework.Pikachu
{
    using System.Collections.Generic;

    public interface IShuffleStrategy
    {
        void Shuffle<T>(List<T> list);
    }
}
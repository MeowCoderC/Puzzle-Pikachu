namespace CahtFramework.Pikachu
{
    using System.Collections.Generic;
    using UnityEngine;

    public class FisherYatesShuffleStrategy : IShuffleStrategy
    {
        public void Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T   temp        = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i]           = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
}
namespace CahtFramework.Pikachu
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public abstract class BoardEntranceModule : ICloneable
    {
        public abstract void AnimateEntrance(List<NodeView> views, Action onComplete);

        public abstract object Clone();
    }
}
namespace CahtFramework.Pikachu
{
    using System;

    public interface IInput
    {
        event Action<NodeView> OnNodeClicked;

        void EnableInput();

        void DisableInput();
    }
}
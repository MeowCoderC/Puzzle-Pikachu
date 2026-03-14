namespace CahtFramework.Pikachu
{
    using UnityEngine;

    [RequireComponent(typeof(BoxCollider2D))]
    public class NodeView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private GameObject     selectedIcon;

        public void SetSkin(Sprite skinSprite)
        {
            if (skinSprite == null)
            {
                this.gameObject.SetActive(false);
                this.iconRenderer.sprite = null;
            }
            else
            {
                this.gameObject.SetActive(true);
                this.iconRenderer.sprite = skinSprite;
            }

            this.SetSelected(false);
        }

        public void SetSelected(bool isSelected)
        {
            if (this.selectedIcon != null) this.selectedIcon.SetActive(isSelected);
        }
    }
}
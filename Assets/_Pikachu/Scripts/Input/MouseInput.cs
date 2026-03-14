namespace CahtFramework.Pikachu
{
    using System;
    using UnityEngine;

    public class MouseInput : MonoBehaviour, IInput
    {
        public event Action<NodeView> OnNodeClicked;

        [Header("Settings")] [SerializeField] private LayerMask nodeLayer;

        private bool   isActive = true;
        private Camera mainCamera;

        private void Awake()
        {
            this.mainCamera = Camera.main;
            if (this.mainCamera == null) Debug.LogError("[MouseInput] Main Camera not found!");
        }

        public void EnableInput()
        {
            this.isActive = true;
            Debug.Log("[MouseInput] Input Enabled.");
        }

        public void DisableInput()
        {
            this.isActive = false;
            Debug.Log("[MouseInput] Input Disabled.");
        }

        private void Update()
        {
            if (!this.isActive) return;

            if (Input.GetMouseButtonDown(0)) this.HandleClick(Input.mousePosition);
        }

        private void HandleClick(Vector3 screenPosition)
        {
            if (this.mainCamera == null) return;

            Vector2 worldPosition = this.mainCamera.ScreenToWorldPoint(screenPosition);

            var hit = Physics2D.Raycast(worldPosition, Vector2.zero, Mathf.Infinity, this.nodeLayer);

            if (hit.collider != null)
                if (hit.collider.TryGetComponent<NodeView>(out var clickedView))
                    this.OnNodeClicked?.Invoke(clickedView);
        }
    }
}
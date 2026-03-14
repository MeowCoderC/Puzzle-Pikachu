namespace CahtFramework.Pikachu
{
    using System.Collections;
    using UnityEngine;

    [RequireComponent(typeof(LineRenderer))]
    public class PathRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private float        displayDuration = 0.3f;

        private void Awake()
        {
            if (this.lineRenderer == null) this.lineRenderer = this.GetComponent<LineRenderer>();
            this.lineRenderer.enabled = false;
        }

        public IEnumerator ShowPathRoutine(Vector3[] worldPoints, int pointCount)
        {
            if (this.lineRenderer == null || worldPoints == null || pointCount == 0) yield break;

            this.lineRenderer.positionCount = pointCount;
            this.lineRenderer.SetPositions(worldPoints);
            this.lineRenderer.enabled = true;

            yield return new WaitForSeconds(this.displayDuration);

            this.lineRenderer.enabled = false;
        }

        public void HidePath()
        {
            if (this.lineRenderer != null) this.lineRenderer.enabled = false;
        }
    }
}
using UnityEngine;


namespace BOTF3D.Core
{

    public class MapLineFixed : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        private Vector3[] points;

        public void GetLineRenderer()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        public void SetUpLine(Vector3[] points)
        {
            lineRenderer.positionCount = points.Length;
            this.points = points;
            if (lineRenderer != null && points != null)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    lineRenderer.SetPosition(i, points[i]);
                }
            }
        }
    }
}

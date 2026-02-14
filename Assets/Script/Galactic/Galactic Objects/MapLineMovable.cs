using UnityEngine;
//using BOTF3D_Core;
//using BOTF3D_Combat;
//using Assets.Script;

namespace BOTF3D.Core
{

    public class MapLineMovable : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        private Vector3[] points;

        public void GetLineRenderer()
        {
            lineRenderer = GetComponentInChildren<LineRenderer>();
            lineRenderer.startColor = Color.clear;
            lineRenderer.endColor = Color.clear;
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

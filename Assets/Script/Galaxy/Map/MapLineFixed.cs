using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;




namespace BOTF3D.Galaxy
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

        public void SetVisible(bool visible)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = visible;
        }
    }
}

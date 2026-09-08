using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;


//using BOTF3D_Core;
//using BOTF3D_Combat;
//using Assets.Script;

namespace BOTF3D.Galaxy
{

    [ExecuteAlways]
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

        /// <summary>
        /// Edit-mode-only preview. FleetController only ever calls SetUpLine (from Update/
        /// MoveToDesitinationGO) while actually playing - Unity doesn't run those callbacks in Edit
        /// Mode, so a fleet prefab just sitting in the Scene view (or opened in Prefab Edit Mode)
        /// shows whichever points happened to be serialized last, which is usually stale and visibly
        /// disconnected from wherever the sprites currently sit. This keeps the line anchored to this
        /// object's own live position purely as an editing aid while NOT playing; SetUpLine's real
        /// runtime values (the server-authoritative fleet position) take over the instant Play begins
        /// and every frame after, exactly as before - this never runs during Play (see the guard).
        /// -60f matches FleetController.Update()'s own galaxyPlanePoint math exactly.
        /// </summary>
        private void Update()
        {
            if (Application.isPlaying) return;
            if (lineRenderer == null) lineRenderer = GetComponentInChildren<LineRenderer>();
            if (lineRenderer == null) return;

            Vector3 top = transform.position;
            Vector3 bottom = new Vector3(top.x, -60f, top.z);
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, top);
            lineRenderer.SetPosition(1, bottom);
        }
    }
}

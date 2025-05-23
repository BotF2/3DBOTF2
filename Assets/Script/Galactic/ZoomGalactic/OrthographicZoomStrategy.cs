using UnityEngine;

namespace Assets.Core
{
    public class OrthographicZoomStrategy : IZoomStrategy
    {
        public OrthographicZoomStrategy(Camera cam, float startingZoom)
        {
            cam.orthographicSize = startingZoom;
        }
        public void ZoomIn(Camera cam, float delta, float nearZoomLimit)
        {
            if (cam.orthographicSize == nearZoomLimit)
            {
                return;
            }
            cam.orthographicSize = Mathf.Max(cam.orthographicSize - delta, nearZoomLimit);
        }

        public void ZoomOut(Camera cam, float delta, float farZoomLimit)
        {
            if (cam.orthographicSize == farZoomLimit)
            {
                return;
            }
            cam.orthographicSize = Mathf.Max(cam.orthographicSize + delta, farZoomLimit);
        }

    }
}

using UnityEngine;

namespace BOTF3D.Core
{
    public interface IZoomStrategy

    {
        void ZoomIn(Camera cam, float delta, float nearZoomLimit);
        void ZoomOut(Camera cam, float delta, float farZoomLimit);
    }
}

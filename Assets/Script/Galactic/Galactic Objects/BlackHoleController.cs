using UnityEngine;

namespace BOTF3D.GamePlay
{
    public class BlackHoleController : MonoBehaviour
    {
        public Canvas CanvasToolTip;
        public Camera galaxyEventCamera;

        void Start()
        {
            CanvasToolTip.worldCamera = galaxyEventCamera;
        }
    }
}

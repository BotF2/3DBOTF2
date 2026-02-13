using UnityEngine;

namespace Assets.GamePlay
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

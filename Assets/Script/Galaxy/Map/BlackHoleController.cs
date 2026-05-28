using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Galaxy
{
    public class BlackHoleController : MonoBehaviour, IController
    {
        public void Initialize() { }
        public void UpdateState() { }
        public Canvas CanvasToolTip;
        public Camera galaxyEventCamera;

        void Start()
        {
            CanvasToolTip.worldCamera = galaxyEventCamera;
        }
    }
}

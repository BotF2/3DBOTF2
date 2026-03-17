using BOTF3D.GamePlay;
using UnityEngine;

namespace BOTF3D.UI
{
    public class IntelligenceUIController : MonoBehaviour
    {
        private Camera galaxyEventCamera;
        [SerializeField]
        private Canvas parentCanvas;
        public IntelligenceController IntelligenceController;
        public GameObject IntelUIToggle; // GameObject controlles this active UI on/off
        public GameObject IntelUITable;
    }
}

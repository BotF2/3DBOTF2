
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    public class IntelligenceUIController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        private Camera galaxyEventCamera;
        [SerializeField]
        private Canvas parentCanvas;
        public IntelligenceController IntelligenceController;
        public GameObject IntelUIToggle; // GameObject controlles this active UI on/off
        public GameObject IntelUITable;
    }
}

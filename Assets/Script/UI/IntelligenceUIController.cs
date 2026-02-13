using Assets.GamePlay;
using UnityEngine;

namespace Assets.UI
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

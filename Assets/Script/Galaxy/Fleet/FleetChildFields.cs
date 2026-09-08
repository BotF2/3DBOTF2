using TMPro;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;


//using UnityEngine.UI;

namespace BOTF3D.Galaxy
{

    public class FleetChildFields : MonoBehaviour
    {
        [Header("GameObjects")]
        public GameObject DropLine;
        public GameObject FleetNameGO;
        public GameObject InsigniaGO;
        public GameObject InsigniaUnknownGO;
        // Romulan/Klingon cloak arc (CloakingController, §8 II.3) - background + label shown while
        // this fleet's FleetData.IsCloakActive is true, toggled by FleetController.UpdateCloakVisual
        // alongside the Insignia grayscale swap. Assign the CloakBackground GO in the Editor (same
        // pattern as InsigniaGO above).
        public GameObject CloakBackground;

        [Header("Text")]
        public TextMeshProUGUI FleetName;
        public TextMeshProUGUI text; // pre and pending a need.
    }

}

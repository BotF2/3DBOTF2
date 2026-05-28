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

        [Header("Text")]
        public TextMeshProUGUI FleetName;
        public TextMeshProUGUI text; // pre and pending a need.
    }

}

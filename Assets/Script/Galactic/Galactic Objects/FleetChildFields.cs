using TMPro;
using UnityEngine;
//using UnityEngine.UI;

namespace Assets.Core
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

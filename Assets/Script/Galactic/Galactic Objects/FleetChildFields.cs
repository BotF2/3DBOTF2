using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Core
{

    public class FleetChildFields : MonoBehaviour
    {
        [Header("GOs")]
        public GameObject DestinationLine;
        public GameObject DropLine;
        public GameObject FleetNameGO;
        public RectTransform InsigniaHolder;

        [Header("Text")]
        public TextMeshProUGUI FleetName;
        public TextMeshProUGUI text;
        public TextMeshProUGUI motext;
        public TextMeshProUGUI evenmotext;

        [Header("Image")]
        public Image Insignia;
        public Image InsigniaUnknown;
    }

}

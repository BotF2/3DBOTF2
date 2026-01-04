// Ignore Spelling: Sys

using TMPro;
using UnityEngine;

namespace Assets.Core
{
    public class StarSysChildFields : MonoBehaviour
    {
        [Header("Text")]
        public TextMeshProUGUI SysName;
        public TextMeshProUGUI SysDescription;

        [Header("GameObjects")]
        public GameObject OwnerInsigniaGO;
        public GameObject StarSpriteGO;

    }
}

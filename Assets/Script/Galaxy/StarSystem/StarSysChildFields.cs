// Ignore Spelling: Sys

using TMPro;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Galaxy
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

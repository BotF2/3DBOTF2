using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Combat
{
    public class AnimationObjectPair : MonoBehaviour
    {
        public GameObject[] _objectPair = new GameObject[2];
        public AnimationObjectPair(GameObject child, GameObject parent)
        {
            this._objectPair = new GameObject[] { child, parent };
        }
    }
}
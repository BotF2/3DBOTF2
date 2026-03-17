using UnityEngine;

namespace BOTF3D.Core
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
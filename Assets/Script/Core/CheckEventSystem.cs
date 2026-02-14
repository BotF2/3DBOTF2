using UnityEngine;

namespace BOTF3D.Core
{

    public class CheckEventSystem : MonoBehaviour
    {
        public GameObject eventSystemGameObject;

        void Update()
        {
            if (eventSystemGameObject != null && !eventSystemGameObject.activeInHierarchy)
            {
                Debug.LogWarning("EventSystem is active!");
                eventSystemGameObject.SetActive(true);
            }
        }
    }
}

using UnityEngine;

namespace BOTF3D.Combat
{
    public class VFXController : MonoBehaviour
    {
        [SerializeField] private float duration = 2f;
        [SerializeField] private bool destroyOnFinish = true;

        private void Start()
        {
            if (destroyOnFinish)
            {
                Destroy(gameObject, duration);
            }
        }
    }
}
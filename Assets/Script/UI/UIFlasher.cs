using System.Collections;
using UnityEngine;

namespace Assets.UI
{
    public class UIFlasher : MonoBehaviour
    {
        [SerializeField] private GameObject powerOverloadImage;
        [SerializeField] private float interval = 0.5f;
        [SerializeField] private int flashes = 3;

        private Coroutine flashRoutine;

        private void OnEnable()
        {
            flashRoutine = StartCoroutine(Flash());
        }

        private void OnDisable()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            //// Ensure final state
            //gameObject.SetActive(false);
        }

        private IEnumerator Flash()
        {
            for (int i = 0; i < flashes; i++)
            {
                powerOverloadImage.SetActive(true);
                yield return new WaitForSeconds(interval);
                powerOverloadImage.SetActive(false);
                yield return new WaitForSeconds(interval);
            }
        }
    }
}


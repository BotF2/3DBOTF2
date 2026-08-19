using System.Collections;
using TMPro;
using UnityEngine;

namespace BOTF3D.UI
{
    /// <summary>
    /// Alternates a TextMeshProUGUI between its original color and an alternate color while
    /// flashing - used to reinforce an urgent/attention-worthy status (e.g. ColdWar relations)
    /// without changing the text content.
    /// </summary>
    public class TextColorFlasher : MonoBehaviour
    {
        [SerializeField] private float interval = 0.5f;

        private TextMeshProUGUI text;
        private Color baseColor;
        private Color flashColor;
        private Coroutine flashRoutine;
        private bool baseColorCaptured;

        /// <summary>
        /// Starts (or keeps running) the flash. baseColor is captured only the first time this is
        /// ever called on a given instance - the text's color at that moment is its true original
        /// color; every call after that could be mid-flash (already showing alternateColor), so
        /// re-capturing on every call would corrupt the "normal" side of the flash.
        /// </summary>
        public void StartFlashing(TextMeshProUGUI targetText, Color alternateColor)
        {
            text = targetText;
            if (!baseColorCaptured)
            {
                baseColor = targetText.color;
                baseColorCaptured = true;
            }
            flashColor = alternateColor;
            if (flashRoutine == null && isActiveAndEnabled)
                flashRoutine = StartCoroutine(Flash());
        }

        public void StopFlashing()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }
            if (text != null && baseColorCaptured)
                text.color = baseColor;
        }

        private void OnEnable()
        {
            if (text != null && flashRoutine == null)
                flashRoutine = StartCoroutine(Flash());
        }

        private void OnDisable()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }
        }

        private IEnumerator Flash()
        {
            bool showAlternate = false;
            while (true)
            {
                text.color = showAlternate ? flashColor : baseColor;
                showAlternate = !showAlternate;
                yield return new WaitForSeconds(interval);
            }
        }
    }
}

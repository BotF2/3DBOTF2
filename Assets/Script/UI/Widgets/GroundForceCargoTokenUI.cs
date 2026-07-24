using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace BOTF3D.UI
{
    /// <summary>
    /// The single draggable "ground force" token shown in CargoDeployMenuUIController.GroundForceSlot.
    /// Structurally identical to PopulationCargoTokenUI (represents the system's available GroundForces
    /// count as a pool) but drops onto a CargoDropSlot load a ground-force unit instead of population.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class GroundForceCargoTokenUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float ReappearDelay = 0.15f;

        public TextMeshProUGUI countText;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Transform originalParent;
        private Vector2 originalAnchoredPosition;
        private int availableGroundForces;
        private bool transferSucceeded;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void SetAvailable(int groundForces)
        {
            availableGroundForces = groundForces;
            if (countText != null)
                countText.text = groundForces.ToString();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (availableGroundForces <= 0) return;

            originalParent = transform.parent;
            originalAnchoredPosition = rectTransform.anchoredPosition;
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
            transform.SetParent(transform.root, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (originalParent == null) return;
            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (originalParent == null) return;

            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = originalAnchoredPosition;
            canvasGroup.blocksRaycasts = true;
            originalParent = null;

            if (transferSucceeded)
            {
                transferSucceeded = false;
                canvasGroup.alpha = 0f;
                StopAllCoroutines();
                StartCoroutine(ReappearAfterDelay());
            }
            else
            {
                canvasGroup.alpha = 1f;
            }
        }

        /// <summary>Called by CargoDropSlot.OnDrop (which fires before OnEndDrag) when the drag ended in a successful load.</summary>
        public void NotifyTransferSucceeded()
        {
            transferSucceeded = true;
        }

        private IEnumerator ReappearAfterDelay()
        {
            yield return new WaitForSecondsRealtime(ReappearDelay);
            canvasGroup.alpha = 1f;
        }
    }
}

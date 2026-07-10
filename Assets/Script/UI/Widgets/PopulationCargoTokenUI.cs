using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace BOTF3D.UI
{
    /// <summary>
    /// The single draggable "population" token shown in CargoDeployMenuUIController.PopulationSlot.
    /// Represents the system's available Population as a pool rather than one token per unit (unlike
    /// GroundForces, Population is a scalar, not a List&lt;GameObject&gt;) — dragging it onto a
    /// CargoDropSlot moves 1 unit into that transport's cargo hold. Always snaps back to its slot;
    /// it is a control, not something that gets consumed or reparented permanently.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PopulationCargoTokenUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float ReappearDelay = 0.15f;

        public TextMeshProUGUI countText;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Transform originalParent;
        private Vector2 originalAnchoredPosition;
        private int availablePopulation;
        private bool transferSucceeded;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void SetAvailable(int population)
        {
            availablePopulation = population;
            if (countText != null)
                countText.text = population.ToString();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (availablePopulation <= 0) return;

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
                // Confirms the transfer visually: snap back invisible, then quickly reappear,
                // rather than just instantly being visible again as if nothing happened.
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

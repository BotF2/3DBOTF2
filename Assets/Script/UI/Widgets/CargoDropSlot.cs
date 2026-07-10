using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BOTF3D.Combat;

namespace BOTF3D.UI
{
    /// <summary>
    /// Drop target attached to each CargoTransportRowUI. Accepts a dragged PopulationCargoTokenUI or
    /// GroundForceCargoTokenUI and forwards the transfer to CargoDeployMenuUIController, which owns
    /// the actual data mutation. Also highlights its background while a token is dragged over it, so
    /// the player can see which row will receive the drop.
    /// </summary>
    public class CargoDropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [HideInInspector] public ShipController OwnerTransport;

        [Header("Drop Highlight")]
        public Image background;
        public Color normalColor = new Color(1f, 1f, 1f, 0.1f);
        public Color highlightColor = new Color(1f, 0.85f, 0.3f, 0.35f);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsDraggingToken(eventData))
                SetHighlighted(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlighted(false);
        }

        public void OnDrop(PointerEventData eventData)
        {
            SetHighlighted(false);
            if (OwnerTransport == null || eventData.pointerDrag == null) return;

            var popToken = eventData.pointerDrag.GetComponent<PopulationCargoTokenUI>();
            if (popToken != null)
            {
                bool loaded = CargoDeployMenuUIController.Instance != null &&
                              CargoDeployMenuUIController.Instance.TryLoadPopulationOnto(OwnerTransport);

                // OnDrop fires before OnEndDrag, so the token can use this to play its "moved" feedback
                // once it repositions itself back at PopulationSlot.
                if (loaded)
                    popToken.NotifyTransferSucceeded();
                return;
            }

            var forceToken = eventData.pointerDrag.GetComponent<GroundForceCargoTokenUI>();
            if (forceToken != null)
            {
                bool loaded = CargoDeployMenuUIController.Instance != null &&
                              CargoDeployMenuUIController.Instance.TryLoadGroundForceOnto(OwnerTransport);
                if (loaded)
                    forceToken.NotifyTransferSucceeded();
            }
        }

        private bool IsDraggingToken(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return false;
            return eventData.pointerDrag.GetComponent<PopulationCargoTokenUI>() != null ||
                   eventData.pointerDrag.GetComponent<GroundForceCargoTokenUI>() != null;
        }

        private void SetHighlighted(bool on)
        {
            if (background != null)
                background.color = on ? highlightColor : normalColor;
        }
    }
}

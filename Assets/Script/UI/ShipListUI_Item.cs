using Assets.GamePlay;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.UI
{
    public class ShipListUI_Item : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ShipController ShipController; // Reference to underlying ship
        public FleetController CurrentFleet; // Who currently owns the ship UI
        public StarSysController CurrentStarSyst; // Which star system currently owns the ship UI

        // CRITICAL: Store the intended slot parent (set externally when placing in slots)
        public Transform IntendedSlotParent { get; set; }

        private Transform originalParent;
        private int originalSiblingIndex; // Track position in original parent
        private Canvas parentCanvas; // The root canvas for coordinate conversion
        private CanvasGroup canvasGroup; // For visual feedback during drag
        private RectTransform rectTransform;
        private bool wasDragged = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // Get the root canvas for coordinate conversion
            parentCanvas = GetComponentInParent<Canvas>();

            // Add CanvasGroup if not present (for drag visual feedback)
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            Debug.Log($"ShipListUI_Item Awake: {name}, parent={transform.parent?.name}");
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"OnBeginDrag PRE-CAPTURE: {name}, current parent={transform.parent?.name}, IntendedSlotParent={IntendedSlotParent?.name}");

            // Use IntendedSlotParent if available, otherwise fall back to current parent
            originalParent = IntendedSlotParent ?? transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            wasDragged = false;

            Debug.Log($"OnBeginDrag POST-CAPTURE: {ShipController?.ShipData?.ShipName} from originalParent={originalParent?.name}, siblingIndex={originalSiblingIndex}");

            // Make semi-transparent during drag
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.6f;
                canvasGroup.blocksRaycasts = false; // Allow raycasts to pass through to detect drop targets
            }

            // Find ShipDeployPanel and use it as drag parent
            var deployMenu = ShipDeployMenuUIController.Instance;
            if (deployMenu != null && deployMenu.ShipDeployPanel != null)
            {
                // Move to ShipDeployPanel temporarily so it can be dragged freely
                transform.SetParent(deployMenu.ShipDeployPanel.transform, true); // worldPositionStays=true
                transform.SetAsLastSibling(); // Ensure it's on top visually
                Debug.Log($"OnBeginDrag: Moved to ShipDeployPanel, new parent={transform.parent?.name}");
            }
            else
            {
                Debug.LogError($"OnBeginDrag: Cannot find ShipDeployPanel! deployMenu={deployMenu != null}, panel={deployMenu?.ShipDeployPanel != null}");
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rectTransform != null)
            {
                // Simply follow the mouse cursor
                rectTransform.position = eventData.position;
            }
            wasDragged = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log($"OnEndDrag START: {ShipController?.ShipData?.ShipName}, wasDragged={wasDragged}, current parent={transform.parent?.name}");

            // Restore visual state
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1.0f;
                canvasGroup.blocksRaycasts = true;
            }

            // Determine which slot we're dropped into
            Transform newParent = DetermineDropTarget(eventData);

            Debug.Log($"OnEndDrag: newParent={newParent?.name}, originalParent={originalParent?.name}, same={newParent == originalParent}");

            if (newParent != null)
            {
                Debug.Log($"OnEndDrag: Dropping into slot {newParent.name}");
                // Reparent UI
                transform.SetParent(newParent, false);

                // Update the IntendedSlotParent to the new slot
                IntendedSlotParent = newParent;

                // Update ownership based on which slot
                if (newParent != originalParent)
                {
                    UpdateOwnershipFromSlot(newParent);
                }
            }
            else
            {
                Debug.Log($"OnEndDrag: Invalid drop, returning to original parent '{originalParent?.name}'");
                // Return to original parent if invalid drop
                if (originalParent != null)
                {
                    transform.SetParent(originalParent, false);
                    transform.SetSiblingIndex(originalSiblingIndex); // Restore original position
                    Debug.Log($"OnEndDrag: Successfully returned to {transform.parent?.name}");
                }
                else
                {
                    Debug.LogWarning($"OnEndDrag: originalParent is null! Current parent: {transform.parent?.name}");
                }
            }

            wasDragged = false;
            Debug.Log($"OnEndDrag COMPLETE: final parent={transform.parent?.name}");
        }

        private Transform DetermineDropTarget(PointerEventData eventData)
        {
            var deployMenu = ShipDeployMenuUIController.Instance;
            if (deployMenu == null)
            {
                Debug.LogWarning("DetermineDropTarget: deployMenu is null");
                return null;
            }

            if (deployMenu.TopSlot != null && RectTransformUtility.RectangleContainsScreenPoint(
                deployMenu.TopSlot.GetComponent<RectTransform>(),
                eventData.position,
                eventData.pressEventCamera))
            {
                Debug.Log($"DetermineDropTarget: TopSlot");
                return deployMenu.TopSlot.transform;
            }
            else if (deployMenu.BottomSlot != null && RectTransformUtility.RectangleContainsScreenPoint(
                deployMenu.BottomSlot.GetComponent<RectTransform>(),
                eventData.position,
                eventData.pressEventCamera))
            {
                Debug.Log($"DetermineDropTarget: BottomSlot");
                return deployMenu.BottomSlot.transform;
            }

            Debug.Log($"DetermineDropTarget: NONE (invalid drop area)");
            return null;
        }

        private void UpdateOwnershipFromSlot(Transform slotParent)
        {
            var deployMenu = ShipDeployMenuUIController.Instance;
            if (deployMenu == null || ShipController == null) return;

            Debug.Log($"UpdateOwnershipFromSlot: Ship={ShipController.ShipData.ShipName}, Slot={slotParent.name}");

            // Remove from previous owner
            RemoveFromCurrentOwner();

            // Determine new owner based on slot
            if (slotParent == deployMenu.TopSlot.transform)
            {
                if (deployMenu.TopFleet != null)
                {
                    AddToFleet(deployMenu.TopFleet);
                }
                else if (deployMenu.TopStarSyst != null)
                {
                    AddToStarSystem(deployMenu.TopStarSyst);
                }
            }
            else if (slotParent == deployMenu.BottomSlot.transform)
            {
                if (deployMenu.BottomFleet != null)
                {
                    AddToFleet(deployMenu.BottomFleet);
                }
                else if (deployMenu.BottomStarSyst != null)
                {
                    AddToStarSystem(deployMenu.BottomStarSyst);
                }
            }
        }

        private void RemoveFromCurrentOwner()
        {
            if (CurrentFleet != null)
            {
                Debug.Log($"RemoveFromCurrentOwner: Removing {ShipController.ShipData.ShipName} from fleet {CurrentFleet.name}");
                CurrentFleet.FleetData.ShipsList.Remove(ShipController);
                ShipController.ShipData.CurrentFleetController = null;
            }
            else if (CurrentStarSyst != null)
            {
                Debug.Log($"RemoveFromCurrentOwner: Removing {ShipController.ShipData.ShipName} from system {CurrentStarSyst.name}");
                CurrentStarSyst.StarSysData.ShipsList.Remove(ShipController);
                ShipController.ShipData.CurrentStarSysController = null;
            }
        }

        private void AddToFleet(FleetController fleet)
        {
            Debug.Log($"AddToFleet: '{ShipController?.ShipData?.ShipName}' to fleet '{fleet?.name}'");

            CurrentFleet = fleet;
            CurrentStarSyst = null;

            if (!fleet.FleetData.ShipsList.Contains(ShipController))
            {
                fleet.FleetData.ShipsList.Add(ShipController);
            }

            ShipController.ShipData.CurrentFleetController = fleet;
            ShipController.ShipData.CurrentStarSysController = null;

            if (ShipController != null && fleet != null && fleet.gameObject != null)
            {
                ShipController.transform.SetParent(fleet.transform, false);
            }

            try { fleet.UpdateMaxWarp(); } catch { }
        }

        private void AddToStarSystem(StarSysController starSys)
        {
            Debug.Log($"AddToStarSystem: '{ShipController?.ShipData?.ShipName}' to system '{starSys?.name}'");

            CurrentStarSyst = starSys;
            CurrentFleet = null;

            if (!starSys.StarSysData.ShipsList.Contains(ShipController))
            {
                starSys.StarSysData.ShipsList.Add(ShipController);
            }

            ShipController.ShipData.CurrentStarSysController = starSys;
            ShipController.ShipData.CurrentFleetController = null;

            if (ShipController != null && starSys != null && starSys.gameObject != null)
            {
                ShipController.transform.SetParent(starSys.transform, false);
            }
        }
    }
}

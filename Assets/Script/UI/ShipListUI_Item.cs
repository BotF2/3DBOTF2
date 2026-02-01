using Assets.Core;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShipListUI_Item : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ShipController ShipController; // Reference to underlying ship
    public FleetController CurrentFleet; // Who currently owns the ship UI
    public StarSysController CurrentStarSyst; // Which star system currently owns the ship UI

    private Transform originalParent;
    private Canvas canvas;
    private bool wasDragged = false;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        wasDragged = false;
        Debug.Log($"BeginDrag: {ShipController?.ShipData?.ShipName} from {originalParent?.name}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
        wasDragged = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"EndDrag: {ShipController?.ShipData?.ShipName}, wasDragged={wasDragged}");

        // Determine which slot we're dropped into
        Transform newParent = DetermineDropTarget(eventData);

        if (newParent != null && newParent != originalParent)
        {
            Debug.Log($"EndDrag: Dropping into {newParent.name}");
            // Reparent UI
            transform.SetParent(newParent, false);

            // Immediately update ownership based on which slot
            UpdateOwnershipFromSlot(newParent);
        }
        else
        {
            Debug.Log($"EndDrag: Returning to original parent {originalParent?.name}");
            // Return to original parent if invalid drop
            transform.SetParent(originalParent, false);
        }

        wasDragged = false;
    }

    private Transform DetermineDropTarget(PointerEventData eventData)
    {
        // Check if we're over TopSlot or BottomSlot
        var deployMenu = ShipDeployMenuUIController.Instance;
        if (deployMenu == null) return null;

        if (RectTransformUtility.RectangleContainsScreenPoint(
            deployMenu.TopSlot.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera))
        {
            return deployMenu.TopSlot.transform;
        }
        else if (RectTransformUtility.RectangleContainsScreenPoint(
            deployMenu.BottomSlot.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera))
        {
            return deployMenu.BottomSlot.transform;
        }

        return null;
    }

    private void UpdateOwnershipFromSlot(Transform slotParent)
    {
        var deployMenu = ShipDeployMenuUIController.Instance;
        if (deployMenu == null || ShipController == null) return;

        Debug.Log($"UpdateOwnershipFromSlot: Ship={ShipController.ShipData.ShipName}, Slot={slotParent.name}");
        Debug.Log($"  - TopSlot: {deployMenu.TopSlot.transform.name}, BottomSlot: {deployMenu.BottomSlot.transform.name}");

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
        Debug.Log($"AddToFleet START: Moving ship '{ShipController?.ShipData?.ShipName}' to fleet '{fleet?.name}'");
        Debug.Log($"  - Fleet has ShipListUIParent: {fleet.FleetData?.ShipListUIParent != null}");
        Debug.Log($"  - ShipController parent before: {ShipController?.transform?.parent?.name}");

        CurrentFleet = fleet;
        CurrentStarSyst = null;

        if (!fleet.FleetData.ShipsList.Contains(ShipController))
        {
            fleet.FleetData.ShipsList.Add(ShipController);
            Debug.Log($"  - Ship '{ShipController.ShipData.ShipName}' added to fleet '{fleet.name}'. Fleet now has {fleet.FleetData.ShipsList.Count} ships");
        }
        else
        {
            Debug.Log($"  - Ship '{ShipController.ShipData.ShipName}' already in fleet '{fleet.name}'");
        }

        ShipController.ShipData.CurrentFleetController = fleet;
        ShipController.ShipData.CurrentStarSysController = null;

        // CRITICAL: Reparent the actual 3D ShipController GameObject to the fleet in scene hierarchy
        if (ShipController != null && ShipController.gameObject != null && fleet != null)
        {
            // First, verify the fleet exists in the hierarchy
            if (fleet.gameObject == null)
            {
                Debug.LogError($"  - ERROR: Fleet GameObject is null!");
            }
            else
            {
                ShipController.transform.SetParent(fleet.transform, false);
                Debug.Log($"  - Ship GameObject '{ShipController.name}' reparented to fleet '{fleet.name}' in scene hierarchy");
                Debug.Log($"  - ShipController parent after: {ShipController.transform.parent?.name}");
            }
        }
        else
        {
            Debug.LogError($"  - ERROR: Failed to reparent ship GameObject: ShipController={ShipController != null}, ShipController.gameObject={ShipController?.gameObject != null}, fleet={fleet != null}");
        }

        // Update fleet max warp if fleet has that method
        try
        {
            fleet.UpdateMaxWarp();
            Debug.Log($"  - UpdateMaxWarp called for fleet '{fleet.name}'");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"  - UpdateMaxWarp failed for fleet '{fleet.name}': {ex.Message}");
        }

        Debug.Log($"AddToFleet END: Ship {ShipController.ShipData.ShipName} moved to fleet {fleet.FleetData.Name}");
    }

    private void AddToStarSystem(StarSysController starSys)
    {
        Debug.Log($"AddToStarSystem START: Moving ship '{ShipController?.ShipData?.ShipName}' to system '{starSys?.name}'");

        CurrentStarSyst = starSys;
        CurrentFleet = null;

        if (!starSys.StarSysData.ShipsList.Contains(ShipController))
        {
            starSys.StarSysData.ShipsList.Add(ShipController);
            Debug.Log($"  - Ship added. System now has {starSys.StarSysData.ShipsList.Count} ships");
        }

        ShipController.ShipData.CurrentStarSysController = starSys;
        ShipController.ShipData.CurrentFleetController = null;

        // CRITICAL: Reparent the actual 3D ShipController GameObject
        if (ShipController != null && ShipController.gameObject != null && starSys != null)
        {
            ShipController.transform.SetParent(starSys.transform, false);
            Debug.Log($"  - Ship GameObject '{ShipController.name}' reparented to star system '{starSys.name}' in scene hierarchy");
        }
        else
        {
            Debug.LogError($"  - ERROR: Failed to reparent ship GameObject");
        }

        Debug.Log($"AddToStarSystem END: Ship {ShipController.ShipData.ShipName} moved to system {starSys.StarSysData.SysName}");
    }
}

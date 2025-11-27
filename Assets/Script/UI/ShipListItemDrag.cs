using Assets.Core;
using UnityEngine;
using UnityEngine.EventSystems;


public class ShipListItemDrag : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    public Transform originalParent;
    //private FleetController oldFleet;
    //private StarSysController oldStarSys;

    public ShipType ShipType;
    public Sprite ShipSprite;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {

        //var theDragedScript = eventData.pointerDrag.GetComponent<ShipListItemDrag>();
        //switch (eventData.pointerDrag.name) //!! need to update to look for key word like "Scout" etc
        //{
        //    case "ItemScout":
        //        theDragedScript.ShipType = ShipType.Scout;
        //        break;
        //    case "ItemDestroyer":
        //        theDragedScript.ShipType = ShipType.Destroyer;
        //        break;
        //    case "ItemCruiser":
        //        theDragedScript.ShipType = ShipType.Cruiser;
        //        break;
        //    case "ItemLtCruiser":
        //        theDragedScript.ShipType = ShipType.LtCruiser;
        //        break;
        //    case "ItemHvyCruiser":
        //        theDragedScript.ShipType = ShipType.HvyCruiser;
        //        break;
        //    case "ItemTransport":
        //        theDragedScript.ShipType = ShipType.Transport;
        //        break;
        //    default:
        //        break;
        //}
        originalParent = transform.parent;
        canvasGroup.interactable = true;
        canvasGroup.alpha = 0.6f; // make it transparent
        canvasGroup.blocksRaycasts = false; // see item slots below
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();// down list = top layer to be seen
        Debug.Log("onBeginDrag");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // follow the mouse cursor every frame
        rectTransform.anchoredPosition += eventData.delta / rectTransform.lossyScale;
        Debug.Log("onDraging");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        if (eventData.pointerEnter != null)
        {
            var shipUI = eventData.pointerDrag.GetComponent<ShipListUI_Item>();
            if (shipUI == null) return;
            //if (shipUI.CurrentFleet != null)
            //{
            //    oldFleet = shipUI.CurrentFleet;
            //}
            //else if (shipUI.CurrentStarSyst != null)
            //{
            //    oldStarSys = shipUI.CurrentStarSyst;
            //}
            if (eventData.pointerEnter.name == "TopSlot") // ship UI GO dropped on this slot go, not the ship controller go
            {
                if (GalaxyMenuUIController.Instance.FleetLookingForShipDeploy != null)
                {
                    if (shipUI.CurrentStarSyst != null)
                    {
                        shipUI.CurrentStarSyst.RemoveFromShipList(shipUI.ShipController);
                        shipUI.CurrentStarSyst = null;
                    }
                    else if (shipUI.CurrentFleet != null)
                    {
                        shipUI.CurrentFleet.RemoveFromShipList(shipUI.ShipController);
                        shipUI.CurrentFleet = null;
                    }
                    shipUI.CurrentFleet = GalaxyMenuUIController.Instance.FleetLookingForShipDeploy;

                    //RemoveFromOldList(shipUI.ShipController);// the ship UI GO was dropped here so lets remove the ship controller from the old parent fleet or star system
                    shipUI.CurrentFleet.AddToShipList(shipUI.ShipController); // and add the ship controller to the new fleet here or star system in the else

                }
                else if (GalaxyMenuUIController.Instance.StarSystLookingForShipDeploy != null)
                {
                    if (shipUI.CurrentStarSyst != null)
                    {
                        shipUI.CurrentStarSyst.RemoveFromShipList(shipUI.ShipController);
                        shipUI.CurrentStarSyst = null;
                    }
                    else if (shipUI.CurrentFleet != null)
                    {
                        shipUI.CurrentFleet.RemoveFromShipList(shipUI.ShipController);
                        shipUI.CurrentFleet = null;
                    }
                    shipUI.CurrentStarSyst = GalaxyMenuUIController.Instance.StarSystLookingForShipDeploy;
                    //RemoveFromOldList(shipUI.ShipController);
                    shipUI.CurrentStarSyst.AddToShipList(shipUI.ShipController);
                }
            }
            else if (eventData.pointerEnter.name == "BottomSlot") // ship UI GO dropped on this slot go, not the ship controller go
            {
                if (GalaxyMenuUIController.Instance.FleetConSelectedForShipDeploy != null)
                {
                    if (shipUI.CurrentStarSyst != null)
                    {
                        shipUI.CurrentStarSyst.RemoveFromShipList(shipUI.ShipController);
                        shipUI.CurrentStarSyst = null;
                    }
                    else if (shipUI.CurrentFleet != null)
                    {
                        shipUI.CurrentFleet.RemoveFromShipList(shipUI.ShipController);
                        shipUI.CurrentFleet = null;
                    }
                    //RemoveFromOldList(shipUI.ShipController);
                    shipUI.CurrentFleet = GalaxyMenuUIController.Instance.FleetConSelectedForShipDeploy;
                    shipUI.CurrentFleet.AddToShipList(shipUI.ShipController);
                }
                else if (GalaxyMenuUIController.Instance.StarSystConSelectedForShipDeploy != null)
                {
                    if (shipUI.CurrentStarSyst != null)
                    {
                        shipUI.CurrentStarSyst.RemoveFromShipList(shipUI.ShipController);
                        shipUI.CurrentStarSyst = null;
                    }
                    else if (shipUI.CurrentFleet != null)
                    {
                        shipUI.CurrentFleet.RemoveFromShipList(shipUI.ShipController);
                        shipUI.CurrentFleet = null;
                    }
                    shipUI.CurrentStarSyst = GalaxyMenuUIController.Instance.StarSystConSelectedForShipDeploy;
                    //RemoveFromOldList(shipUI.ShipController);// the ship UI GO was dropped here so lets remove the ship controller from the old parent fleet or star system
                    shipUI.CurrentStarSyst.AddToShipList(shipUI.ShipController);
                }
            }
            if (eventData.pointerEnter.tag == "TopShipDeploySlot")
            {
                transform.SetParent(eventData.pointerEnter.transform);
            }
            else if (eventData.pointerEnter.tag == "BottomShipDeploySlot")
            {
                transform.SetParent(eventData.pointerEnter.transform);// parent the ship UI GO under the slot, slots are child of ShipDeployPanel in CanvasGalaxy
            }
            //var theDragedScript = eventData.pointerDrag.GetComponent<ShipListItemDrag>();
            //switch (eventData.pointerDrag.name)
            //{
            //    case "ItemScout":
            //        theDragedScript.ShipType = ShipType.Scout;
            //        break;
            //    case "ItemDestroyer":
            //        theDragedScript.ShipType = ShipType.Destroyer;
            //        break;
            //    case "ItemCruiser":
            //        theDragedScript.ShipType = ShipType.Cruiser;
            //        break;
            //    case "ItemLtCruiser":
            //        theDragedScript.ShipType = ShipType.LtCruiser;
            //        break;
            //    case "ItemHvyCruiser":
            //        theDragedScript.ShipType = ShipType.HvyCruiser;
            //        break;
            //    case "ItemTransport":
            //        theDragedScript.ShipType = ShipType.Transport;
            //        break;
            //    default:
            //        break;
            //}
        }
        else
        {
            transform.SetParent(originalParent);
        }
        rectTransform.anchoredPosition = Vector2.zero;
        Debug.Log("onEndDrag");
    }

    //private void RemoveFromOldList(ShipController shipController)
    //{
    //    if (shipController.ShipData.CurrentFleetController != null)
    //    {
    //        shipController.ShipData.CurrentFleetController.RemoveFromShipList(shipController, shipController.ShipData.CurrentFleetController);
    //    }
    //    else if (shipController.ShipData.CurrentStarSysController != null)
    //    {
    //        shipController.ShipData.CurrentStarSysController.RemoveFromShipList(shipController, shipController.ShipData.CurrentStarSysController);
    //    }
    //}

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("OnPointerDown");
        //Can we do something here later?
    }
}

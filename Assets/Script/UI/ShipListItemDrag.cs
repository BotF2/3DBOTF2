using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Core;
using System;


public class ShipListItemDrag : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    public Transform originalParent;
    private FleetController oldFleet;
    private StarSysController oldStarSys;

    public ShipType ShipType;
    public Sprite ShipSprite;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        
        var theDragedScript = eventData.pointerDrag.GetComponent<ShipListItemDrag>();
        switch (eventData.pointerDrag.name)
        {
            case "ItemScout":
                theDragedScript.ShipType = ShipType.Scout;
                    break;
            case "ItemDestroyer":
                theDragedScript.ShipType = ShipType.Destroyer;
                break;
            case "ItemCruiser":
                theDragedScript.ShipType = ShipType.Cruiser;
                break;
            case "ItemLtCruiser":
                theDragedScript.ShipType = ShipType.LtCruiser;
                break;
            case "ItemHvyCruiser":
                theDragedScript.ShipType = ShipType.HvyCruiser;
                break;
            case "ItemTransport":
                theDragedScript.ShipType = ShipType.Transport;
                break;
            default:
                break;
        }
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
            var shipUI = eventData.pointerDrag.GetComponent<ShipUiItem>();
            if (shipUI == null) return;
            if (shipUI.CurrentFleet != null)
            {
                oldFleet = shipUI.CurrentFleet;
            }
            else if (shipUI.CurrentStarSys != null)
            {
                oldStarSys = shipUI.CurrentStarSys;
            }
            if (eventData.pointerEnter.name == "TopSlot")
            {
                if (GalaxyMenuUIController.Instance.FleetLookingForShipDeploy != null)
                {
                    shipUI.CurrentFleet = GalaxyMenuUIController.Instance.FleetLookingForShipDeploy;
                    shipUI.CurrentStarSys = null;
                    RemoveFromOldList(shipUI.ShipController);
                    shipUI.CurrentFleet.AddToShipList(shipUI.ShipController);

                }
                else if (GalaxyMenuUIController.Instance.StarSysLookingForShipDeploy != null)
                {
                    shipUI.CurrentStarSys = GalaxyMenuUIController.Instance.StarSysLookingForShipDeploy;
                    shipUI.CurrentFleet = null;
                    RemoveFromOldList(shipUI.ShipController);
                    shipUI.CurrentStarSys.StarSysData.AddToShipList(shipUI.ShipController);
                }
            }
            else if (eventData.pointerEnter.name == "BottomSlot")
            {
                if (GalaxyMenuUIController.Instance.FleetConSelectedForShipDeploy != null)
                {
                    shipUI.CurrentFleet = GalaxyMenuUIController.Instance.FleetConSelectedForShipDeploy;
                    shipUI.CurrentStarSys = null;
                    RemoveFromOldList(shipUI.ShipController);
                    shipUI.CurrentFleet.AddToShipList(shipUI.ShipController);
                }
                else if (GalaxyMenuUIController.Instance.StarSysConSelectedForShipDeploy != null)
                {
                    shipUI.CurrentStarSys = GalaxyMenuUIController.Instance.StarSysConSelectedForShipDeploy;
                    shipUI.CurrentFleet = null;
                    RemoveFromOldList(shipUI.ShipController);
                    shipUI.CurrentStarSys.StarSysData.AddToShipList(shipUI.ShipController);
                }
            }
            if (eventData.pointerEnter.tag == "TopShipDeploySlot")
            {
                transform.SetParent(eventData.pointerEnter.transform);
            }
            else if (eventData.pointerEnter.tag == "BottomShipDeploySlot")
            {
                transform.SetParent(eventData.pointerEnter.transform);
            }
            var theDragedScript = eventData.pointerDrag.GetComponent<ShipListItemDrag>();
            switch (eventData.pointerDrag.name)
            {
                case "ItemScout":
                    theDragedScript.ShipType = ShipType.Scout;
                    break;
                case "ItemDestroyer":
                    theDragedScript.ShipType = ShipType.Destroyer;
                    break;
                case "ItemCruiser":
                    theDragedScript.ShipType = ShipType.Cruiser;
                    break;
                case "ItemLtCruiser":
                    theDragedScript.ShipType = ShipType.LtCruiser;
                    break;
                case "ItemHvyCruiser":
                    theDragedScript.ShipType = ShipType.HvyCruiser;
                    break;
                case "ItemTransport":
                    theDragedScript.ShipType = ShipType.Transport;
                    break;
                default:
                    break;
            }
        }
        else
        {
            transform.SetParent(originalParent);
        }
        rectTransform.anchoredPosition = Vector2.zero;
        Debug.Log("onEndDrag");
    }

    private void RemoveFromOldList(ShipController shipController)
    {
        if (oldFleet != null)
        {
            oldFleet.RemoveFromShipList(shipController);
        }
        else if (oldStarSys != null)
        {
            oldStarSys.RemoveFromShipList(shipController);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("OnPointerDown");
        //Can we do something here later?
    }
}

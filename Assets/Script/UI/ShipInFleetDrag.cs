using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Core;


public class ShipInFleetDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    public Transform originalParent;
    
    public FleetController FleetController;
    public ShipType ShipType;
    public Sprite ShipSprite;
    public int BuildDuration;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        var theDragedScript = eventData.pointerDrag.GetComponent<ShipInFleetDrag>();
        switch (eventData.pointerDrag.name)
        {
            case "ItemScout":
                theDragedScript.ShipType = ShipType.Scout;
                    break;
            case "ItemFactory":
                theDragedScript.ShipType = ShipType.Destroyer;
                break;
            case "ItemShipyard":
                theDragedScript.ShipType = ShipType.Cruiser;
                break;
            case "ItemShieldGenerator":
                theDragedScript.ShipType = ShipType.LtCruiser;
                break;
            case "ItemOrbitalBattery":
                theDragedScript.ShipType = ShipType.HvyCruiser;
                break;
            case "ItemResearchCenter":
                theDragedScript.ShipType = ShipType.Transport;
                break;
            default:
                break;
        }
        originalParent = transform.parent;
        canvasGroup.blocksRaycasts = false; // allow drag
        transform.SetParent(transform.root); // down list to top layer to be seen
        transform.SetAsLastSibling();
        Debug.Log("onBeginDrag");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // follow the mouse cursor
        rectTransform.anchoredPosition += eventData.delta / rectTransform.lossyScale;
        Debug.Log("onDraging");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        if (eventData.pointerEnter != null && eventData.pointerEnter.CompareTag("ShipBuildSlot"))
        {
            transform.SetParent(eventData.pointerEnter.transform);
            var theDragedScript = eventData.pointerDrag.GetComponent<ShipInFleetDrag>();
            switch (eventData.pointerDrag.name)
            {
                case "ItemPowerPlant":
                case "ItemPowerPlant Variant(Clone)":
                    theDragedScript.ShipType = ShipType.Scout;
                    break;
                case "ItemFactory":
                case "ItemFactory Variant(Clone)":
                    theDragedScript.ShipType = ShipType.Destroyer;
                    break;
                case "ItemShipyard":
                case "ItemShipyard Variant(Clone)":
                    theDragedScript.ShipType = ShipType.Cruiser;
                    break;
                case "ItemShieldGenerator":
                case "ItemShieldGenerator Variant(Clone)":
                    theDragedScript.ShipType = ShipType.LtCruiser;
                    break;
                case "ItemOrbitalBattery":
                case "ItemOrbitalBattery Variant(Clone)":
                    theDragedScript.ShipType = ShipType.HvyCruiser;
                    break;
                case "ItemResearchCenter":
                case "ItemResearchCenter Variant(Clone)":
                    theDragedScript.ShipType = ShipType.Transport;
                    break;
                default:
                    break;
            }
            switch (theDragedScript.ShipType)
            {
                case ShipType.Scout:
                    StarSysManager.Instance.NewImageInEmptyShipBuildableInventory(ShipType.Scout);                //StarSysManager.Instance.scoutBluePrintPrefab);
                    break;
                case ShipType.Destroyer:
                    StarSysManager.Instance.NewImageInEmptyShipBuildableInventory(ShipType.Destroyer);
                    break;
                case ShipType.Cruiser:
                    StarSysManager.Instance.NewImageInEmptyShipBuildableInventory(ShipType.Cruiser);
                    break;
                case ShipType.LtCruiser:
                    StarSysManager.Instance.NewImageInEmptyShipBuildableInventory(ShipType.LtCruiser);
                    break;
                case ShipType.HvyCruiser:
                    StarSysManager.Instance.NewImageInEmptyShipBuildableInventory(ShipType.HvyCruiser);
                    break;
                case ShipType.Transport:
                    StarSysManager.Instance.NewImageInEmptyShipBuildableInventory(ShipType.Transport);
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

}

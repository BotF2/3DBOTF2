using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Core;


public class ShipBuildDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    public Transform originalParent;

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
        var theDragedScript = eventData.pointerDrag.GetComponent<ShipBuildDrag>();
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
        canvasGroup.blocksRaycasts = false; // see slot below
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
            var theDragedScript = eventData.pointerDrag.GetComponent<ShipBuildDrag>();
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
            switch (theDragedScript.ShipType)
            {
                case ShipType.Scout:
                    StarSysManager.Instance.NewImageInShipInventory(ShipType.Scout); 
                    break;
                case ShipType.Destroyer:
                    StarSysManager.Instance.NewImageInShipInventory(ShipType.Destroyer);
                    break;
                case ShipType.Cruiser:
                    StarSysManager.Instance.NewImageInShipInventory(ShipType.Cruiser);
                    break;
                case ShipType.LtCruiser:
                    StarSysManager.Instance.NewImageInShipInventory(ShipType.LtCruiser);
                    break;
                case ShipType.HvyCruiser:
                    StarSysManager.Instance.NewImageInShipInventory(ShipType.HvyCruiser);
                    break;
                case ShipType.Transport:
                    StarSysManager.Instance.NewImageInShipInventory(ShipType.Transport);
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

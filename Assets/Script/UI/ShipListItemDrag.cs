using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Core;


public class ShipListItemDrag : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    public Transform originalParent;

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

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("OnPointerDown");
        //Can we do something here later?
    }
}

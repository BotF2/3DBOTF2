using BOTF3D.Core;
using BOTF3D.GamePlay;
using BOTF3D.UI;
using UnityEngine;
using UnityEngine.EventSystems;


public class ShipBuildDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    public Transform originalParent;

    public ShipType ShipType;
    public Sprite ShipSprite;
    public int BuildDuration;

    public StarSysController StarSysController { get; internal set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        this.StarSysController = StarSysMenuUIController.Instance.ActiveStarSysController;

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

        // ✅ Validate StarSysController
        if (StarSysController == null)
        {
            Debug.LogError("ShipBuildDrag.OnEndDrag: StarSysController is NULL!");
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = Vector2.zero;
            return;
        }

        if (eventData.pointerEnter != null && eventData.pointerEnter.CompareTag("ShipBuildSlot"))
        {
            // ✅ CRITICAL: Parent to the ACTUAL ship build queue!
            if (StarSysController.ShipListGridLayoutGroup != null)
            {
                transform.SetParent(StarSysController.ShipListGridLayoutGroup.transform);
                Debug.Log($"  ✅ Added '{ShipType}' to ShipListGridLayoutGroup");
            }
            else
            {
                Debug.LogError("  ❌ ShipListGridLayoutGroup is NULL on StarSysController!");
                transform.SetParent(originalParent);
                rectTransform.anchoredPosition = Vector2.zero;
                return;
            }

            var theDraggedScript = eventData.pointerDrag.GetComponent<ShipBuildDrag>();

            // ✅ Create visual icon in inventory
            if (StarSysManager.Instance != null)
            {
                StarSysManager.Instance.NewImageInShipInventory(theDraggedScript.ShipType);
            }

            // ✅ CRITICAL: Manually trigger queue update
            StarSysController.GridShipQueueUpdate();
        }
        else
        {
            transform.SetParent(originalParent);
        }

        rectTransform.anchoredPosition = Vector2.zero;
        Debug.Log("ShipBuildDrag: onEndDrag");
    }

}

using BOTF3D.Core;

using BOTF3D.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;




public class ShipBuildDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
        public void Initialize() { }
        public void UpdateState() { }
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
        // ✅ Don't overwrite if already set! Only set if null
        if (this.StarSysController == null)
        {
            this.StarSysController = StarSysMenuUIController.Instance?.ActiveStarSysController;

            if (this.StarSysController == null)
            {
                Debug.LogError("❌ ShipBuildDrag.OnBeginDrag: Cannot get StarSysController! Both references are null.");
            }
        }

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
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        Debug.Log($"ShipBuildDrag.OnBeginDrag: {ShipType}, StarSysController={(StarSysController != null ? StarSysController.name : "NULL")}");
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
            // Enforce max 5 queued items (count only actual cloned ShipBuildDrag children)
            int queued = 0;
            foreach (var t in StarSysController.sysShipBuildQueueList)
                if (t != null && t.GetComponent<ShipBuildDrag>() != null) queued++;
            if (queued >= 5)
            {
                Debug.Log("ShipBuildDrag: queue full (max 5)");
                transform.SetParent(originalParent);
                rectTransform.anchoredPosition = Vector2.zero;
                return;
            }

            // ✅ CRITICAL: Instantiate CLONE instead of moving original
            GameObject clone = Instantiate(gameObject);

            // ✅ Copy all references to the clone
            ShipBuildDrag clonedScript = clone.GetComponent<ShipBuildDrag>();
            if (clonedScript != null)
            {
                clonedScript.StarSysController = this.StarSysController;
                clonedScript.ShipType = this.ShipType;
                clonedScript.ShipSprite = this.ShipSprite;
                clonedScript.BuildDuration = this.BuildDuration;
                clonedScript.originalParent = this.StarSysController.ShipListGridLayoutGroup.transform;

                Debug.Log($"✅ Cloned ship build item: {ShipType}, StarSysController assigned: {(clonedScript.StarSysController != null)}");
            }
            else
            {
                Debug.LogError("❌ Failed to get ShipBuildDrag component on clone!");
                Destroy(clone);
                transform.SetParent(originalParent);
                rectTransform.anchoredPosition = Vector2.zero;
                return;
            }

            // ✅ CRITICAL: Parent CLONE to the ship build queue
            if (StarSysController.ShipListGridLayoutGroup != null)
            {
                clone.transform.SetParent(StarSysController.ShipListGridLayoutGroup.transform, false);
                Debug.Log($"✅ Added '{ShipType}' clone to ShipListGridLayoutGroup");
            }
            else
            {
                Debug.LogError("❌ ShipListGridLayoutGroup is NULL on StarSysController!");
                Destroy(clone);
                transform.SetParent(originalParent);
                rectTransform.anchoredPosition = Vector2.zero;
                return;
            }

            // ✅ Return ORIGINAL to palette
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = Vector2.zero;

            // ✅ CRITICAL: Manually trigger queue update
            StarSysController.GridShipQueueUpdate();
        }
        else
        {
            // Not dropped on valid slot - return to palette
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        Debug.Log("ShipBuildDrag: onEndDrag complete");
    }

}

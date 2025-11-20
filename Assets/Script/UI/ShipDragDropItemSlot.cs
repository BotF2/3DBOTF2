using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attached to the Ship Drag Drop Item Slot, top and bottom UI view areas, GameObject to handle drop events
/// for the shiplistUI_shipnamehere_(clone) GameObjects being dragged and dropped into the slots.
/// </summary>

[System.Serializable]
public class ShipDragDropItemSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("OnDrop -> slot: " + gameObject.name);

        if (eventData == null || eventData.pointerDrag == null)
            return;

        var dragged = eventData.pointerDrag;
        // Parent the dragged GameObject to this slot (use false so local coordinates are reset)
        dragged.transform.SetParent(this.transform, false);

        // Reset RectTransform so it snaps into the slot
        var rt = dragged.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        // Restore canvas group so it receives ray-casts again
        var cg = dragged.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = true;
            cg.alpha = 1f;
            cg.interactable = true;
        }

        // Optionally ensure the dragged item is top child
        dragged.transform.SetAsLastSibling();
    }
}

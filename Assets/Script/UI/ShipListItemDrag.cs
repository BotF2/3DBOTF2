using UnityEngine;
using UnityEngine.EventSystems;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;


/// <summary>
/// Enables drag-and-drop behavior for a ship list item within a Unity UI, allowing the item to be moved and
/// repositioned by the user.
/// </summary>
/// <remarks>Implements Unity's drag event interfaces to support interactive item dragging in UI lists. When a
/// drag is initiated, the item is moved to the top-level canvas to ensure correct rendering order. If the item is not
/// dropped onto a valid target, it returns to its original position. Requires a CanvasGroup component on the same
/// GameObject. This component is typically used in conjunction with drop targets that handle item placement.</remarks>

public class ShipListItemDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private Transform originalParent;
    private Vector2 originalAnchoredPosition;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        rootCanvas = eventData.pointerDrag.GetComponentInParent<Canvas>().rootCanvas;
        originalParent = transform.parent;
        originalAnchoredPosition = GetComponent<RectTransform>().anchoredPosition;

        transform.SetParent(rootCanvas.transform, true); // move to top canvas
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // If still parented to canvas, it wasn't dropped on a slot
        if (transform.parent == rootCanvas.transform)
        {
            SnapBack();
        }
    }

    private void SnapBack()
    {
        transform.SetParent(originalParent, false);

        var rt = GetComponent<RectTransform>();
        rt.anchoredPosition = originalAnchoredPosition;
        rt.localScale = Vector3.one;
    }
}


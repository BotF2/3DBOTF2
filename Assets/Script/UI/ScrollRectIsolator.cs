using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// Prevents scroll events from propagating past this ScrollRect to any ancestor ScrollRect.
    /// Attach to the same GameObject as a ScrollRect that must not share scroll input with a parent.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectIsolator : MonoBehaviour, IScrollHandler
    {
        private ScrollRect _scrollRect;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        public void OnScroll(PointerEventData eventData)
        {
            // Forward to our own ScrollRect so it handles the scroll normally,
            // then mark the event used so it does not bubble to parent ScrollRects.
            _scrollRect.OnScroll(eventData);
            eventData.Use();
        }
    }
}

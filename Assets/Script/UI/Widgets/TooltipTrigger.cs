using UnityEngine;
using UnityEngine.EventSystems;

namespace BOTF3D.UI
{
    /// <summary>
    /// Add to any UI element to give it a tooltip.
    /// Set the content string in the Inspector, or call SetContent() at runtime.
    /// Requires TooltipSystem to be present in the scene.
    /// </summary>
    [AddComponentMenu("BOTF3D/UI/Tooltip Trigger")]
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [TextArea(2, 5)]
        public string content;

        public void SetContent(string text) => content = text;

        public void OnPointerEnter(PointerEventData _) => TooltipSystem.Instance?.Show(content);
        public void OnPointerExit(PointerEventData _)  => TooltipSystem.Instance?.Hide();

        private void OnDisable() => TooltipSystem.Instance?.Hide();
    }
}

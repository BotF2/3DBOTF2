using TMPro;
using UnityEngine;

namespace BOTF3D.UI
{
    /// <summary>
    /// Singleton tooltip panel.
    ///
    /// followCursor = true  — panel repositions to track the mouse each frame.
    ///                        Use for item-list tooltips, ship cards, etc.
    /// followCursor = false — panel stays at its anchored Inspector position and only
    ///                        shows/hides. Use for fixed info-bars like the turn phase
    ///                        strip above the overlay panel.
    ///
    /// Place this script on a Panel GO inside the persistent canvas.
    /// Add TooltipTrigger to any UI element to connect it to this system.
    /// </summary>
    public class TooltipSystem : MonoBehaviour
    {
        public static TooltipSystem Instance { get; private set; }

        [SerializeField] private RectTransform tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;

        [Header("Cursor Following")]
        [Tooltip("When true the panel repositions to follow the mouse. " +
                 "When false the panel appears at its anchored Inspector position.")]
        [SerializeField] private bool followCursor = true;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 cursorOffset = new Vector2(14f, -14f);

        private RectTransform _canvasRect;
        private bool _visible;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
            if (tooltipPanel != null)
                tooltipPanel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_visible && followCursor)
                RepositionToMouse();
        }

        public void Show(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || tooltipPanel == null) return;
            if (tooltipText != null) tooltipText.text = text;
            tooltipPanel.gameObject.SetActive(true);
            _visible = true;
            if (followCursor)
                RepositionToMouse();
        }

        public void Hide()
        {
            if (tooltipPanel != null)
                tooltipPanel.gameObject.SetActive(false);
            _visible = false;
        }

        private void RepositionToMouse()
        {
            if (tooltipPanel == null || rootCanvas == null) return;
            if (_canvasRect == null)
                _canvasRect = rootCanvas.GetComponent<RectTransform>();

            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : rootCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, Input.mousePosition, cam, out var local);

            tooltipPanel.localPosition = local + cursorOffset;
        }
    }
}

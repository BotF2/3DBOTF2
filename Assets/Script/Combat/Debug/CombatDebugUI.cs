using BOTF3D.Core;
using BOTF3D.UI;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.Combat.Debugging
{
    /// <summary>
    /// In-game debug overlay for combat testing.
    /// Press F1 to toggle, provides combat state info and test controls.
    /// </summary>
    public class CombatDebugUI : MonoBehaviour
    {
        [Header("Settings")]
        public KeyCode ToggleKey = KeyCode.F1;
        public bool ShowOnStart = false;

        [Header("UI Elements")]
        private Canvas debugCanvas;
        private GameObject debugPanel;
        private TextMeshProUGUI debugText;
        private bool isVisible = false;

        private CombatController combatController;
        private TurnBasedCombatResolver turnResolver;

        private void Start()
        {
            CreateDebugUI();
            isVisible = ShowOnStart;
            debugPanel?.SetActive(isVisible);
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                ToggleVisibility();
            }

            if (isVisible)
            {
                UpdateDebugInfo();
            }
        }

        public void Initialize(CombatController controller)
        {
            combatController = controller;
            turnResolver = controller?.TurnResolver;
            GameLogger.Log(GameLogger.LogCategory.Combat, "🐛 CombatDebugUI initialized");
        }

        private void ToggleVisibility()
        {
            isVisible = !isVisible;
            debugPanel?.SetActive(isVisible);
            GameLogger.Log(GameLogger.LogCategory.Combat, $"🐛 Combat Debug UI: {(isVisible ? "SHOWN" : "HIDDEN")}");
        }

        private void CreateDebugUI()
        {
            // Create canvas
            GameObject canvasObj = new GameObject("CombatDebugCanvas");
            canvasObj.transform.SetParent(transform);
            debugCanvas = canvasObj.AddComponent<Canvas>();
            debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            debugCanvas.sortingOrder = 9999; // Always on top

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Create debug panel
            debugPanel = new GameObject("DebugPanel");
            debugPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = debugPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(500, 400);

            Image panelBg = debugPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.8f);

            // Create text element
            GameObject textObj = new GameObject("DebugText");
            textObj.transform.SetParent(debugPanel.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            debugText = textObj.AddComponent<TextMeshProUGUI>();
            debugText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            debugText.fontSize = 16;
            debugText.color = Color.white;
            debugText.alignment = TextAlignmentOptions.TopLeft;
            debugText.enableWordWrapping = true;

            // Create button panel
            CreateButtonPanel();
        }

        private void CreateButtonPanel()
        {
            GameObject buttonPanel = new GameObject("ButtonPanel");
            buttonPanel.transform.SetParent(debugPanel.transform, false);

            RectTransform buttonPanelRect = buttonPanel.AddComponent<RectTransform>();
            buttonPanelRect.anchorMin = new Vector2(0, 0);
            buttonPanelRect.anchorMax = new Vector2(1, 0);
            buttonPanelRect.pivot = new Vector2(0.5f, 0);
            buttonPanelRect.anchoredPosition = Vector2.zero;
            buttonPanelRect.sizeDelta = new Vector2(0, 100);

            // Add buttons
            CreateButton(buttonPanel.transform, "Skip Turn", new Vector2(-180, 10), SkipToNextTurn);
            CreateButton(buttonPanel.transform, "Side 1 Win", new Vector2(-60, 10), ForceSideOneWin);
            CreateButton(buttonPanel.transform, "Side 2 Win", new Vector2(60, 10), ForceSideTwoWin);
            CreateButton(buttonPanel.transform, "End Combat", new Vector2(180, 10), EndCombat);
        }

        private void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject($"Btn_{label}");
            buttonObj.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(100, 30);

            Image buttonImg = buttonObj.AddComponent<Image>();
            buttonImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImg;
            button.onClick.AddListener(onClick);

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            buttonText.text = label;
            buttonText.fontSize = 14;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
        }

        private void UpdateDebugInfo()
        {
            if (debugText == null) return;

            // Find combat controller if not set
            if (combatController == null)
            {
                combatController = CombatUIManager.Instance?.CurrentCombatController;
                if (combatController == null)
                {
                    debugText.text = "⚠️ No active combat controller";
                    return;
                }
                turnResolver = combatController.TurnResolver;
            }

            var combatData = combatController.CombatData;
            if (combatData == null)
            {
                debugText.text = "⚠️ No combat data";
                return;
            }

            // Build debug info
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== COMBAT DEBUG (F1 to toggle) ===");
            sb.AppendLine();

            // Turn info
            if (turnResolver != null)
            {
                sb.AppendLine($"Turn: {turnResolver.CurrentTurn}");
                sb.AppendLine($"Phase: {turnResolver.CurrentPhase}");
                sb.AppendLine();
            }

            // Orders
            sb.AppendLine($"Side 1 Order: {combatData.SideOneOrder}");
            sb.AppendLine($"Side 2 Order: {combatData.SideTwoOrder}");
            sb.AppendLine();

            // Ship counts
            int s1Alive = combatData.SideOneShipCons.Count(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy);
            int s2Alive = combatData.SideTwoShipCons.Count(s => s != null && !s.ShipData.Distroyed && s.gameObject.activeInHierarchy);
            sb.AppendLine($"Side 1 Ships: {s1Alive} / {combatData.SideOneShipCons.Count}");
            sb.AppendLine($"Side 2 Ships: {s2Alive} / {combatData.SideTwoShipCons.Count}");
            sb.AppendLine();

            // HP totals
            int s1HP = GetTotalHP(combatData.SideOneShipCons);
            int s2HP = GetTotalHP(combatData.SideTwoShipCons);
            sb.AppendLine($"Side 1 HP: {s1HP}");
            sb.AppendLine($"Side 2 HP: {s2HP}");
            sb.AppendLine();

            // Last turn result
            if (turnResolver?.LastTurnResult != null)
            {
                var result = turnResolver.LastTurnResult;
                sb.AppendLine("Last Turn:");
                sb.AppendLine($"  S1 Damage: {result.SideOneDamageDealt}");
                sb.AppendLine($"  S2 Damage: {result.SideTwoDamageDealt}");
            }

            // Multipliers
            if (combatData.SideOneOrder != CombatOrders.None && combatData.SideTwoOrder != CombatOrders.None)
            {
                float s1Mult = CombatOrderHelper.GetOrderMultiplier(combatData.SideOneOrder, combatData.SideTwoOrder);
                float s2Mult = CombatOrderHelper.GetOrderMultiplier(combatData.SideTwoOrder, combatData.SideOneOrder);
                sb.AppendLine();
                sb.AppendLine($"S1 Multiplier: {s1Mult:F2}x");
                sb.AppendLine($"S2 Multiplier: {s2Mult:F2}x");
            }

            debugText.text = sb.ToString();
        }

        private int GetTotalHP(System.Collections.Generic.List<ShipController> ships)
        {
            return ships.Where(s => s != null && !s.ShipData.Distroyed)
                        .Sum(s => s.ShipData.ShieldHealth + s.ShipData.HullHealth);
        }

        // ===== DEBUG COMMANDS =====

        private void SkipToNextTurn()
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, "🐛 DEBUG: Skip to next turn");

            if (turnResolver != null && combatController != null)
            {
                // Force current phase to end
                combatController.StopAllCoroutines();
                combatController.isMoving = false;

                // Start next turn
                turnResolver.BeginOrderSelection();
            }
        }

        private void ForceSideOneWin()
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, "🐛 DEBUG: Force Side 1 victory");

            if (combatController?.CombatData != null)
            {
                // Destroy all Side 2 ships
                foreach (var ship in combatController.CombatData.SideTwoShipCons)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        ship.ShipData.ShieldHealth = 0;
                        ship.ShipData.HullHealth = 0;
                        ship.ShipData.Distroyed = true;
                        ship.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void ForceSideTwoWin()
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, "🐛 DEBUG: Force Side 2 victory");

            if (combatController?.CombatData != null)
            {
                // Destroy all Side 1 ships
                foreach (var ship in combatController.CombatData.SideOneShipCons)
                {
                    if (ship != null && !ship.ShipData.Distroyed)
                    {
                        ship.ShipData.ShieldHealth = 0;
                        ship.ShipData.HullHealth = 0;
                        ship.ShipData.Distroyed = true;
                        ship.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void EndCombat()
        {
            GameLogger.Log(GameLogger.LogCategory.Combat, "🐛 DEBUG: End combat immediately");

            if (combatController != null)
            {
                combatController.EndCombat();
            }
        }
    }
}

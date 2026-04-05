
using BOTF3D.Combat;
using BOTF3D.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    ///} <summary>
    /// Persistent manager that controls the CombatUICanvas in CombatScene
    /// Lives in GalaxyScene and updates for each new combat
    /// </summary>
    public class CombatUIManager : MonoBehaviour
    {
        public static CombatUIManager Instance { get; private set; }

        // ✅ References to current combat
        public CombatController CurrentCombatController;
        private GameObject currentCombatUICanvas;
        private GameObject currentCombat3DCanvas;
        private GameObject currentGameOverCanvas;

        // ✅ Cached UI references (found dynamically each combat)
        private GameObject panelCombatMenu;
        private GameObject panelShipCombat;
        private GameObject panelCombatOver;
        private TextMeshProUGUI timerText;
        private Toggle engage, rush, retreat, formation, targetTransports;

        // ✅ Combat state
        private float remainingTime = 10f;
        private bool isTimerRunning = false;
        private CombatOrders currentOrder = CombatOrders.Engage;
        public CivEnum CivEnumLocalPlayer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate CombatUIManager - destroying duplicate");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ CombatUIManager initialized (persistent)");
        }

        private void Update()
        {
            if (isTimerRunning)
            {
                remainingTime -= Time.deltaTime;
                if (remainingTime > 0f)
                {
                    if (timerText != null)
                    {
                        timerText.text = Mathf.FloorToInt(remainingTime).ToString("00");
                    }
                }
                else
                {
                    isTimerRunning = false;
                    remainingTime = 0f;
                    if (timerText != null) timerText.text = "00";
                    EnterShipCombatPhase();
                }
            }
        }

        /// <summary>
        /// Called by CombatManager when a new combat starts for the local player
        /// </summary>
        public void SetupForCombat(CombatController combatController, GameObject combatUICanvas, GameObject combat3DCanva, GameObject gameOverCanvas)
        {
            Debug.Log($"🎮 CombatUIManager: Setting up for combat {combatController.CombatID}");

            CurrentCombatController = combatController;
            currentCombatUICanvas = combatUICanvas;
            currentCombat3DCanvas = combat3DCanva;
            currentGameOverCanvas = gameOverCanvas;
            CivEnumLocalPlayer = GameController.Instance.GameData.LocalPlayerCivEnum;

            // ✅ Find and setup all UI elements
            FindAndSetupUI();

            // ✅ Start timer
            remainingTime = 10f;
            isTimerRunning = true;
        }

        /// <summary>
        /// Find all UI elements in the CombatUICanvas
        /// </summary>
        private void FindAndSetupUI()
        {
            if (currentCombatUICanvas == null)
            {
                Debug.LogError("❌ Cannot setup UI - CombatUICanvas is null!");
                return;
            }

            currentCombatUICanvas.SetActive(true);

            // Find panels
            RectTransform[] rectTransforms = currentCombatUICanvas.GetComponentsInChildren<RectTransform>(true);
            foreach (var rt in rectTransforms)
            {
                switch (rt.name)
                {
                    case "PanelCombat_Menu":
                        panelCombatMenu = rt.gameObject;
                        panelCombatMenu.SetActive(true);
                        break;
                }
            }
            RectTransform[] rectTransforms2 = currentCombat3DCanvas.GetComponentsInChildren<RectTransform>(true);
            foreach (var rt2 in rectTransforms2)
            {
                switch (rt2.name)
                {
                    case "PanelShipCombat":
                        panelShipCombat = rt2.gameObject;
                        panelShipCombat.SetActive(false);
                        break;
                }
            }
            RectTransform[] rectTransforms3 = currentGameOverCanvas.GetComponentsInChildren<RectTransform>(true);
            foreach (var rt3 in rectTransforms3)
            {
                switch (rt3.name)
                {
                    case "PanelCombatEnd":
                        panelCombatOver = rt3.gameObject;
                        panelCombatOver.SetActive(false);
                        break;
                }
            }

            // Find timer text
            TextMeshProUGUI[] tmps = currentCombatUICanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp.name == "Timer Text")
                {
                    timerText = tmp;
                    break;
                }
            }

            // Find and setup toggles
            Toggle[] toggles = currentCombatUICanvas.GetComponentsInChildren<Toggle>(true);
            foreach (var toggle in toggles)
            {
                toggle.onValueChanged.RemoveAllListeners();

                switch (toggle.name)
                {
                    case "Toggle_ENGAGE":
                        engage = toggle;
                        engage.onValueChanged.AddListener(OnToggleENGAGE);
                        break;
                    case "Toggle_RUSH":
                        rush = toggle;
                        rush.onValueChanged.AddListener(OnToggleRUSH);
                        break;
                    case "Toggle_RETREAT":
                        retreat = toggle;
                        retreat.onValueChanged.AddListener(OnToggleRETREAT);
                        break;
                    case "Toggle_FORMATION":
                        formation = toggle;
                        formation.onValueChanged.AddListener(OnToggleFORMATION);
                        break;
                    case "Toggle_TARGET_TRANSPORTS":
                        targetTransports = toggle;
                        targetTransports.onValueChanged.AddListener(OnToggleTARGET_TRANSPORTS);
                        break;
                }
            }

            // Find and setup buttons
            Button[] buttons = currentCombatUICanvas.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name == "ButtonEnterCombat")
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(EnterShipCombatPhase);
                }
            }

            Debug.Log("✅ Combat UI setup complete");
        }

        // ✅ Toggle callbacks
        private void OnToggleENGAGE(bool isOn)
        {
            if (isOn)
            {
                currentOrder = CombatOrders.Engage;
                Debug.Log("Order: Engage");
            }
        }

        private void OnToggleRUSH(bool isOn)
        {
            if (isOn)
            {
                currentOrder = CombatOrders.Rush;
                Debug.Log("Order: Rush");
            }
        }

        private void OnToggleRETREAT(bool isOn)
        {
            if (isOn)
            {
                currentOrder = CombatOrders.Retreat;
                Debug.Log("Order: Retreat");
            }
        }

        private void OnToggleFORMATION(bool isOn)
        {
            if (isOn)
            {
                currentOrder = CombatOrders.Formation;
                Debug.Log("Order: Formation");
            }
        }

        private void OnToggleTARGET_TRANSPORTS(bool isOn)
        {
            if (isOn)
            {
                currentOrder = CombatOrders.TargetTransports;
                Debug.Log("Order: Target Transports");
            }
        }

        /// <summary>
        /// Called when timer ends or button clicked
        /// </summary>
        private void EnterShipCombatPhase()
        {
            isTimerRunning = false;

            if (CurrentCombatController != null)
            {
                // ✅ Hide combat menu, show 3D combat view
                if (panelCombatMenu != null)
                {
                    panelCombatMenu.SetActive(false);
                    Debug.Log("✅ Combat menu closed");
                }

                if (panelShipCombat != null)
                {
                    panelShipCombat.SetActive(true);
                    Debug.Log("✅ Ship combat panel activated");

                }

                // Apply current order
                CurrentCombatController.SetShipOrders(currentOrder, CivEnumLocalPlayer);

                // Start combat animation
                CurrentCombatController.RunAnimation();

                Debug.Log($"✅ Combat phase started with order: {currentOrder}");
            }
            else
            {
                Debug.LogError("❌ Cannot start combat - controller is null!");
            }
        }

        /// <summary>
        /// Show combat over panel
        /// </summary>
        public void ShowCombatOverPanel()
        {
            if (panelCombatOver != null)
            {
                panelCombatOver.SetActive(true);
            }
        }

        /// <summary>
        /// Clean up when combat ends
        /// </summary>
        public void CleanupCombat()
        {
            Debug.Log("🧹 CombatUIManager: Cleaning up combat UI references");

            CurrentCombatController = null;
            currentCombatUICanvas = null;
            panelCombatMenu = null;
            panelShipCombat = null;
            panelCombatOver = null;
            timerText = null;
            engage = null;
            rush = null;
            retreat = null;
            formation = null;
            targetTransports = null;
            isTimerRunning = false;
        }
    }
}
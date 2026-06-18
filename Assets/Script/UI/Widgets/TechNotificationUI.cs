using BOTF3D.Core;

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    /// <summary>
    /// Shows notifications when tech level advances
    /// </summary>
    public class TechNotificationUI : MonoBehaviour
    {
        public static TechNotificationUI Instance { get; private set; }

        [Header("UI References")]
        public GameObject notificationPanel;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI messageText;
        public Button closeButton;

        [Header("Settings")]
        public float autoCloseDuration = 3f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Hide initially
            if (notificationPanel != null)
                notificationPanel.SetActive(false);

            // Wire close button
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseNotification);
        }

        private void OnEnable()
        {
            // Subscribe to tech advancement event
            if (TechManager.Instance != null)
            {
                TechManager.Instance.OnTechAdvanced += OnTechAdvanced; // ✅ Use event, not private method
            }
        }

        private void OnDisable()
        {
            // Unsubscribe
            if (TechManager.Instance != null)
            {
                TechManager.Instance.OnTechAdvanced -= OnTechAdvanced;
            }
        }

        private void OnTechAdvanced(CivController civ, TechLevel newLevel)
        {
            // Handle the event
            ShowTechAdvancement(civ, newLevel);
        }

        /// <summary>
        /// Show notification when tech level advances
        /// </summary>
        public void ShowTechAdvancement(CivController civ, TechLevel newLevel)
        {
            // Show notification UI
            Debug.Log($"📢 Showing tech notification: {civ.CivData.CivShortName} → {newLevel}");
            // TODO: Show UI panel with tech advancement message
        }

        public void CloseNotification()
        {
            if (notificationPanel != null)
                notificationPanel.SetActive(false);

            CancelInvoke(nameof(CloseNotification));
        }
    }
}

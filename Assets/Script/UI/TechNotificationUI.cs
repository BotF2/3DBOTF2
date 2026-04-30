using BOTF3D.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            // Subscribe to tech advancement events
            if (TechManager.Instance != null)
            {
                TechManager.Instance.OnTechLevelAdvanced += ShowTechAdvancementNotification;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe
            if (TechManager.Instance != null)
            {
                TechManager.Instance.OnTechLevelAdvanced -= ShowTechAdvancementNotification;
            }
        }

        /// <summary>
        /// Show notification when tech level advances
        /// </summary>
        private void ShowTechAdvancementNotification(CivEnum civEnum, TechLevel oldLevel, TechLevel newLevel)
        {
            // Only show for local player
            if (!GameController.Instance.AreWeLocalPlayer(civEnum))
                return;

            var civData = CivManager.Instance?.GetCivDataByCivEnum(civEnum);
            if (civData == null) return;

            string civName = civData.CivShortName;

            if (titleText != null)
                titleText.text = "TECHNOLOGY ERA";

            if (messageText != null)
                messageText.text = $"{newLevel}"; //{civName} available

            if (notificationPanel != null)
                notificationPanel.SetActive(true);

            // Auto-close after duration
            if (autoCloseDuration > 0)
                Invoke(nameof(CloseNotification), autoCloseDuration);

            Debug.Log($"🔔 Showing tech advancement notification: {civName} → {newLevel}");
        }

        public void CloseNotification()
        {
            if (notificationPanel != null)
                notificationPanel.SetActive(false);

            CancelInvoke(nameof(CloseNotification));
        }
    }
}

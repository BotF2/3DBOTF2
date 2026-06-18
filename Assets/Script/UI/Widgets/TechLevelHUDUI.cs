using TMPro;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Civilization;

namespace BOTF3D.UI
{
    /// <summary>
    /// Attach to GalaxyScene/MainGalaxyMenuRibbon/TechLevel.
    /// Auto-discovers the TechLeveEnum TMP child; no Inspector wiring needed.
    /// Updates on every turn advance and whenever the local player gains a tech level.
    /// </summary>
    public class TechLevelHUDUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI techLevelText;

        private bool _subscribed;
        private bool _initialRefreshDone;

        private void Awake()
        {
            if (techLevelText == null)
            {
                Transform t = transform.Find("TechLevelData");
                if (t != null) techLevelText = t.GetComponent<TextMeshProUGUI>();
            }
        }

        private void OnEnable()
        {
            IntelligenceManager.OnProjectResolved += OnIntelProjectResolved;
            _initialRefreshDone = false;
            TrySubscribe();
            Refresh();
        }

        private void Update()
        {
            if (!_initialRefreshDone)
                Refresh();
        }

        private void OnDisable()
        {
            IntelligenceManager.OnProjectResolved -= OnIntelProjectResolved;
            if (_subscribed)
            {
                if (TimeManager.Instance != null)
                    TimeManager.Instance.OnTurnAdvanced -= Refresh;
                if (TechManager.Instance != null)
                    TechManager.Instance.OnTechAdvanced -= OnTechAdvanced;
                _subscribed = false;
            }
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (TimeManager.Instance != null && TechManager.Instance != null)
            {
                TimeManager.Instance.OnTurnAdvanced += Refresh;
                TechManager.Instance.OnTechAdvanced += OnTechAdvanced;
                _subscribed = true;
            }
        }

        private void OnTechAdvanced(CivController civ, TechLevel newLevel)
        {
            if (GameController.Instance?.AreWeLocalPlayer(civ.CivData.CivEnum) == true)
                Refresh();
        }

        private void OnIntelProjectResolved(SecretActionsEnum action, CivEnum initiator, CivEnum target,
            bool succeeded, bool discovered, int techGain)
        {
            if (succeeded && GameController.Instance?.AreWeLocalPlayer(initiator) == true)
                Refresh();
        }

        public void Refresh()
        {
            TrySubscribe();
            if (techLevelText == null) return;

            CivController localCiv = CivManager.Instance?.LocalPlayerCivController;
            if (localCiv == null) return;

            techLevelText.text = $"{localCiv.CivData.TechRating:F1} ({localCiv.CivData.CurrentTechLevel})";
            _initialRefreshDone = true;
        }
    }
}

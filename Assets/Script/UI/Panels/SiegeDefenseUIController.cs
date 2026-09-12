using BOTF3D.Core;
using BOTF3D.Galaxy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// Defender-side panel shown to the human player whose system is under siege (System Invasion
    /// Phase 1, §4). Opened each InterTurn by TurnEventQueue when the local player is the defender
    /// and Phase B is underway or the system defenses have just been cleared. Gives the player a
    /// turn-by-turn readout of their defense: shields, ground troops, besieging fleet size, and
    /// landed enemy troops. No choices are available in Phase 1 — the panel is informational only.
    ///
    /// Wire-up (Unity Editor):
    ///   - Add this component to a persistent GameObject (PersistentScene recommended, same parent
    ///     as SiegeDecisionUIController).
    ///   - Set PanelRoot to the root of the defense-status panel UI (starts inactive).
    ///   - Assign SysNameLabel, AttackerLabel, ShieldText, SysTroopsText, EnemyFleetText,
    ///     LandedEnemyTroopsText, and DismissButton.
    /// </summary>
    public class SiegeDefenseUIController : MonoBehaviour
    {
        public static SiegeDefenseUIController Instance;

        [Header("Panel root — disabled until OpenPanel() activates it")]
        public GameObject PanelRoot;

        [Header("Labels")]
        [SerializeField] private TMP_Text sysNameLabel;
        [SerializeField] private TMP_Text attackerLabel;

        [Header("Live stats")]
        [SerializeField] private TMP_Text shieldText;
        [SerializeField] private TMP_Text sysTroopsText;
        [SerializeField] private TMP_Text enemyFleetText;
        [SerializeField] private TMP_Text landedEnemyTroopsText;

        [Header("Buttons")]
        [SerializeField] private Button dismissButton;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (PanelRoot != null) PanelRoot.SetActive(false);
            if (dismissButton != null)
                dismissButton.onClick.AddListener(ClosePanel);
        }

        /// <summary>
        /// Opens the panel for the given system. Called by TurnEventQueue (SiegeDefenseNotify drain).
        /// TurnEventQueue.NotifyDismissed is called by ClosePanel — do not call it separately.
        /// </summary>
        public void OpenPanel(StarSysController sysCon)
        {
            if (PanelRoot == null)
            {
                Debug.LogWarning("[SiegeDefense] PanelRoot not assigned — wire it in the Inspector.");
                TurnEventQueue.Instance?.NotifyDismissed();
                return;
            }

            PopulateStats(sysCon);
            PanelRoot.SetActive(true);
        }

        /// <summary>Called by StarSysManager after each Phase B tick so the panel updates mid-siege.</summary>
        public void RefreshStats(StarSysController sysCon)
        {
            if (PanelRoot == null || !PanelRoot.activeSelf) return;
            PopulateStats(sysCon);
        }

        private void PopulateStats(StarSysController sysCon)
        {
            var data = sysCon?.StarSysData;
            if (data == null) return;

            if (sysNameLabel != null)
                sysNameLabel.text = data.SysName;
            if (attackerLabel != null)
                attackerLabel.text = $"Attacker: {data.BesiegingCivEnum}";

            if (shieldText != null)
            {
                if (data.PhaseBShieldsDown)
                    shieldText.text = "Shields: Down";
                else if (data.PhaseBShieldMaxHP > 0)
                    shieldText.text = $"Shields: {data.PhaseBShieldHP:F0} / {data.PhaseBShieldMaxHP:F0}";
                else
                    shieldText.text = $"Shields: {data.ShieldGenerators?.Count ?? 0} generator(s) active";
            }

            if (sysTroopsText != null)
                sysTroopsText.text = $"Troops: {data.GroundForces?.Count ?? 0}";

            if (enemyFleetText != null)
            {
                int ships = 0;
                if (data.BesiegingFleet?.FleetData?.ShipsList != null)
                    foreach (var s in data.BesiegingFleet.FleetData.ShipsList)
                        if (s != null && !s.ShipData.Distroyed) ships++;
                enemyFleetText.text = $"Enemy ships: {ships}";
            }

            if (landedEnemyTroopsText != null)
            {
                if (data.PhaseBTroopsLanded && data.PhaseBAttackerTroopHP > 0)
                    landedEnemyTroopsText.text = $"Enemy troops landed: {data.PhaseBAttackerTroopHP:F0} HP";
                else
                    landedEnemyTroopsText.text = "Enemy troops landed: none";
            }
        }

        private void ClosePanel()
        {
            if (PanelRoot != null) PanelRoot.SetActive(false);
            TurnEventQueue.Instance?.NotifyDismissed();
        }
    }
}

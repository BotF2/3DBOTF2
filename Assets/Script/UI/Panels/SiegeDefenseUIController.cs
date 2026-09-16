using BOTF3D.Core;
using BOTF3D.Galaxy;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
    ///     LandedEnemyTroopsText, PowerOutputText, CountdownText, and DismissButton.
    ///   - This panel has no Close button by design — DismissButton is the only way to close it
    ///     (informational status only, no decision to make here). There is no closeButton field;
    ///     don't wire a separate "CloseButton" object to anything on this controller.
    ///   - CountdownText mirrors the attacker's 20s entry-gate timer (SiegeDecisionUIController) for
    ///     the defender's awareness only — it drives no action here and is a separate client-local
    ///     timer, not networked/synced with the attacker's own countdown.
    ///   - PowerOutputText shows the number of power plants still standing (not power output) —
    ///     ticks down as Target Power/Total Destruction destroy them.
    ///   - Progress sprites (shield/power/facilities/population/troop/landed-troop/owner-insignia
    ///     Images) are optional — leave unassigned until the step sprites exist; PhaseBProgressUI
    ///     no-ops on a null Image. Facilities and Population both track the same
    ///     PhaseBInfrastructureHP pool and should gray in lockstep.
    /// </summary>
    public class SiegeDefenseUIController : MonoBehaviour
    {
        public static SiegeDefenseUIController Instance;

        [Header("Panel root — disabled until OpenPanel() activates it")]
        public GameObject PanelRoot;

        [Header("Labels")]
        [SerializeField] private TMP_Text sysNameLabel;
        [SerializeField] private TMP_Text attackerLabel;

        [Header("Entry-gate countdown — cosmetic mirror of SiegeDecisionUIController's 20s gate")]
        // Purely informational for the defender — no action depends on it, hidden once AssaultMode
        // is chosen or the panel closes. Not networked/synced with the attacker's own timer.
        [SerializeField] private TMP_Text countdownText;
        private const float GateTimeoutSeconds = 20f;
        private float remainingTime;
        private bool isTimerRunning;

        [Header("Live stats")]
        [SerializeField] private TMP_Text shieldText;
        [SerializeField] private TMP_Text sysTroopsText;
        [SerializeField] private TMP_Text enemyFleetText;
        [SerializeField] private TMP_Text landedEnemyTroopsText;
        // Power plants remaining — plain count, not output; see PopulateStats.
        [SerializeField] private TMP_Text powerOutputText;

        [Header("Progress sprites — normal color on open, gray toward Depleted as each pool drains")]
        [SerializeField] private Image shieldProgressImage;
        [SerializeField] private Image powerProgressImage;
        // Split 2026-09-14 (mirrors SiegeDecisionUIController) — Factories/Research Centers and
        // Population each get their own icon, both fed by the same PhaseBInfrastructureHP/MaxHP
        // pool so they gray together — Total Destruction only.
        [FormerlySerializedAs("infrastructureProgressImage")]
        [SerializeField] private Image facilitiesProgressImage;
        [SerializeField] private Image populationProgressImage;
        [SerializeField] private Image troopProgressImage;
        [SerializeField] private Image landedTroopProgressImage;
        [SerializeField] private Image ownerInsigniaImage;

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

        private void Update()
        {
            if (!isTimerRunning) return;

            remainingTime -= Time.unscaledDeltaTime;
            if (remainingTime > 0f)
            {
                if (countdownText != null)
                    countdownText.text = Mathf.CeilToInt(remainingTime).ToString();
                return;
            }

            StopGateTimer();
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

            // Entry-gate countdown — cosmetic only here (see field comment); runs while the
            // attacker's own decision is still pending, hidden once a mode is chosen.
            bool modeChosen = sysCon?.StarSysData != null && sysCon.StarSysData.AssaultMode != AssaultMode.None;
            if (!modeChosen)
            {
                remainingTime = GateTimeoutSeconds;
                isTimerRunning = true;
                if (countdownText != null)
                {
                    countdownText.gameObject.SetActive(true);
                    countdownText.text = Mathf.CeilToInt(remainingTime).ToString();
                }
            }
            else
            {
                StopGateTimer();
            }

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

            // Power plants remaining — plain count, not output; updates as Target Power/Total
            // Destruction destroy them (data.PowerPlants shrinks live in StarSysManager).
            if (powerOutputText != null)
                powerOutputText.text = $"Power Plants: {data.PowerPlants?.Count ?? 0}";

            UpdateProgressSprites(data);
        }

        /// <summary>Same grayscale-progress rules as SiegeDecisionUIController.UpdateProgressSprites — see there.</summary>
        private void UpdateProgressSprites(StarSysData data)
        {
            PhaseBProgressUI.SetProgress(shieldProgressImage, data.PhaseBShieldHP, data.PhaseBShieldMaxHP);
            PhaseBProgressUI.SetProgress(troopProgressImage, data.PhaseBTroopHP, data.PhaseBTroopMaxHP);

            bool powerTargeted = data.AssaultMode == AssaultMode.TargetPower || data.AssaultMode == AssaultMode.TotalDestruction;
            if (powerTargeted)
                PhaseBProgressUI.SetProgress(powerProgressImage, data.PhaseBPowerPlantHP, data.PhaseBPowerPlantMaxHP);
            else
                PhaseBProgressUI.SetNormal(powerProgressImage);

            if (data.AssaultMode == AssaultMode.TotalDestruction)
            {
                PhaseBProgressUI.SetProgress(facilitiesProgressImage, data.PhaseBInfrastructureHP, data.PhaseBInfrastructureMaxHP);
                PhaseBProgressUI.SetProgress(populationProgressImage, data.PhaseBInfrastructureHP, data.PhaseBInfrastructureMaxHP);
            }
            else
            {
                PhaseBProgressUI.SetNormal(facilitiesProgressImage);
                PhaseBProgressUI.SetNormal(populationProgressImage);
            }

            if (data.PhaseBTroopsLanded)
                PhaseBProgressUI.SetProgress(landedTroopProgressImage, data.PhaseBAttackerTroopHP, data.PhaseBAttackerTroopMaxHP);
            else
                PhaseBProgressUI.SetNormal(landedTroopProgressImage);

            if (ownerInsigniaImage != null)
            {
                var sprite = data.CurrentCivController?.CivData?.InsigniaSprite;
                if (sprite != null) ownerInsigniaImage.sprite = sprite;
            }
        }

        private void ClosePanel()
        {
            StopGateTimer();
            if (PanelRoot != null) PanelRoot.SetActive(false);
            TurnEventQueue.Instance?.NotifyDismissed();
        }

        /// <summary>Stops the cosmetic entry-gate countdown — called whenever the panel closes.</summary>
        private void StopGateTimer()
        {
            isTimerRunning = false;
            if (countdownText != null)
                countdownText.gameObject.SetActive(false);
        }
    }
}

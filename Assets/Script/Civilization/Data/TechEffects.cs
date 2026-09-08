using System.Collections.Generic;

namespace BOTF3D.Civilization
{
    /// <summary>
    /// Accumulated, civ-wide gameplay effects produced by <see cref="BOTF3D.Core.TechManager"/>.
    /// ApplyTechEffect(civ, techDef) (TechTree_Phase2_Design.md §6, §8 II.3) - not a parallel
    /// multiplier system, this IS the "reuses existing multiplier plumbing" the design doc asks for:
    /// every consuming system (ShipDataInitializer, FleetController, IntelligenceManager,
    /// DiplomacyController, StarSysManager, TurnBasedCombatResolver, Torpedo, csFogVisibilityAgent,
    /// TranswarpHubController...) just reads a field here instead of re-deriving "has this civ
    /// researched X" from ResearchedTechIds itself.
    ///
    /// Convention for every "+stat%" multiplier field below (per §4's cumulative T1 1.00 -> T7 1.65
    /// curve): completing a tech that targets the field sets it to Max(current, TechDefSO.
    /// EffectMagnitude) rather than stacking multiplicatively - the CSV magnitude at a given tier is
    /// meant to already be the cumulative value once that tier is reached, not a per-tech increment.
    /// EffectMagnitude is still the II.1 placeholder default (1f) on every authored asset - the real
    /// curve is an §8 II.5 balance-pass task. Wiring here is correct regardless of what the numbers
    /// end up being.
    ///
    /// Plain data, no logic - lives beside CivData in Civilization.Data rather than _Core, since it's
    /// written from TechManager (Core) but read from every application layer (Combat/Galaxy/UI), and
    /// _Core must never import application namespaces (CLAUDE.md's Core Layer Dependency Rule).
    /// </summary>
    public class TechEffects
    {
        // ── Propulsion (Branch A) ───────────────────────────────────────────────────────────────
        // T1 Warp Core Stabilization - without this, FleetController.MoveToDesitinationGO applies a
        // 25% speed penalty once a fleet has spent more than one turn in transit (§4's Tier-1 downside).
        public bool PersistentWarpSpeed;
        // T2 Warp Field Optimization + T7 Deep Space Rangefinding capstone - both raise the same
        // sector-move-speed multiplier FleetController.MoveToDesitinationGO applies to CurrentWarpFactor.
        public float WarpSpeedMultiplier = 1f;
        // T3 Warp Field Overlap - FleetController.UpdateMaxWarp averages a fleet's ships' maxWarpFactor
        // instead of capping to the slowest hull once this is true.
        public bool WarpFieldOverlap;
        // T4 Wormhole Quantum Slipstream - no wormhole galaxy-map object exists yet (TechTree_Phase2_
        // Design.md §4 Branch A Tier-4 note); flag only, nothing consumes it today.
        public bool WormholeStabilizer;
        // T5 Transwarp Access - lets a non-Borg civ enter/use a Borg Transwarp Hub (TranswarpHubController).
        public bool AccessTranswarpHub;
        // T6 Warp Beacon Network - static fast lane between two owned systems; flag only for now (no
        // lane-pathing system exists - see TranswarpHubController's own note on scope).
        public bool WarpLaneNetwork;

        // ── Tactical (Branch B) ─────────────────────────────────────────────────────────────────
        // T1 Polarized Hull Plating + T3 Ablative Hull Materials - ShipDataInitializer multiplies
        // HullMaxHealth by this after ShipStatCalculator.Calculate.
        public float HullMultiplier = 1f;
        // T2 Deflector Shields & Harmonics + T7 Quantum Shield Tuning capstone - same, for ShieldMaxHealth.
        public float ShieldMultiplier = 1f;
        // T4 Quantum Capacitors - CombatOrderHelper/TurnBasedCombatResolver auto-triggers a temporary
        // +shield-or-+weapon burst during combat once true, no player action required (§4 Tactical
        // Tier-4 correction: automatic, not player-activated).
        public bool CombatBurst;
        // T5 Regenerative Shield Matrices - ships regen a small amount of shield health each combat turn.
        public bool ShieldRegenMidCombat;
        // T6 Metaphasic Shielding - no environmental/exotic damage source exists yet anywhere in the
        // codebase to resist; flag only, same "nothing to hook into yet" category as WormholeStabilizer.
        public bool EnvironmentalResist;

        // ── Ordnance (Branch C) ─────────────────────────────────────────────────────────────────
        // T1 Beam Weapon Calibration + T7 Directed Energy Overcharge capstone - ShipDataInitializer
        // multiplies BeamDamage/TorpedoDamage by this after ShipStatCalculator.Calculate.
        public float WeaponDamageMultiplier = 1f;
        // T3 Fire Control Solutions - additive to CombatDamageRandomizer's hit variance floor (accuracy)
        // and to critical-hit odds; consumed wherever that roll happens.
        public float AccuracyBonus = 0f;
        // T2/T4/T5/T6 Photon/Plasma/Quantum/Transphasic torpedo unlocks - no separate torpedo-class
        // projectile system exists (single TorpedoDamage stat only), so these are modeled as flags
        // Torpedo.cs reads directly rather than a new ordnance-class object: Plasma adds a damage-over-
        // time tick after impact, Quantum adds flat bonus damage, Transphasic adds a % chance to bypass
        // shields and hit hull directly. Photon (T2) is the baseline unlock with no extra mechanic.
        public bool PlasmaTorpedoes;
        public bool QuantumTorpedoes;
        public bool TransphasicTorpedoes;

        // ── Science (Branch D) ──────────────────────────────────────────────────────────────────
        // Every Branch D tech additionally advances fog sight range one stage (§4's throughline) -
        // TechManager.GetFogSightRangeMultiplier now reads this instead of raw banked TechPoints.
        public int BranchDHighestTierResearched = 0;
        // T3 Anomaly Detection - no anomaly galaxy-map object exists yet; flag only (§4 Branch D
        // Tier-3 note - same "blocked on shipping" category as WormholeStabilizer).
        public bool AnomalyDetection;
        // T4 Terraforming Technology - StarSysController.TerraformSystem gates on this instead of the
        // flat CivData.TechPoints >= 300 stand-in.
        public bool TerraformingTech;
        // T5 Structural Integrity Fields - station (OrbitalBattery) HP multiplier, applied alongside
        // HullMultiplier in ShipDataInitializer for that ShipType only.
        public float StationHPMultiplier = 1f;
        // T6 Xenobiological Engineering - additive facility-cap bonus fraction; T7 High-Density Energy
        // Storage capstone - additive facility power-buffer fraction. Both read by StarSysData's
        // facility-cap/power calculations.
        public float FacilityCapBonus = 0f;
        public float FacilityPowerBuffer = 0f;

        // ── Intelligence (Branch E) ─────────────────────────────────────────────────────────────
        // T1 Subspace Echo Decoys / T2 Quantum Masking Algorithms - no fake-signature or sub-light-
        // movement-detection system exists to hook into yet; flags only.
        public bool Decoy;
        public bool MaskMovement;
        // T3/T4 Deep Cover Networks / Full Surveillance Net - drive IntelligenceManager's per-turn
        // auto-refresh of LastSeenStarSysController/fleets against every contact (the "intel panel"
        // itself is still a stub - see IntelligenceManager.InstantiateIntelligenceUIGameObject - so this
        // is the real backend behavior, not yet paired with a UI to display it).
        public bool IntelPanelPartial;
        public bool IntelPanelFull;
        // T5 Counter-Espionage Doctrine - subtracted from an enemy's chance to discover a covert op
        // targeting this civ (IntelligenceManager.CalculateDiscoveryChance).
        public float SabotageResist = 0f;
        // T6 Tachyon Detection Grid (shared, all civs) - lets this civ see through a Tier-4/5-grade
        // GalaxyMapCloak within sensor range; explicitly does not defeat a Tier-7 "unseen again" upgrade
        // (CloakingController.IsFleetCloakedFromViewer / csFogVisibilityAgent).
        public bool CloakDetection;
        // T7 Strategic Intelligence Mastery capstone - additive bonus to this civ's own covert-op
        // success chance (IntelligenceManager.GetCivSuccessModifier), on top of any Branch-F bonus below.
        public float IntelDashboardBonus = 0f;
        // Accumulated across every Intelligence-branch/Branch-F success-chance tech (Romulan Tal Shiar/
        // Cardassian Obsidian Order/capstone above) - summed into GetCivSuccessModifier's return value.
        public float IntelSuccessBonus = 0f;

        // ── Branch F: Federation ────────────────────────────────────────────────────────────────
        // T2 Diplomatic Outreach Doctrine - multiplies DiplomacyController's majorFactor drift scaling
        // and ApplyGestureGain's point-gain multiplier for Federation specifically (§5a).
        public float DiplomaticOutreachMultiplier = 1f;
        // T3 Minor-Civ Alliance Discount - additive to minorReceptivity in the same drift formula.
        public float MinorCivAllianceDiscount = 0f;
        // T4 Federation Science Exchange - additive TechPoints/turn bonus, applied in
        // TechManager.ProcessResearchForAllCivs alongside the Research Center income calc.
        public float FederationScienceBonus = 0f;
        // T5 Positronic Neural Network - flag only, no specific backend numeric target identified in
        // TechTree_Phase2_Design.md §5a; reserved for a future hook.
        public bool PositronicNeuralNetwork;

        // ── Branch F: Klingon ───────────────────────────────────────────────────────────────────
        // T2 Disruptor Overload Arrays + T6 Disruptor Subsystem Cripple - chance per hit (checked in
        // CombatOrderHelper's order-damage step) to additionally disable a random subsystem, following
        // the same order-based damage-modifier pattern Rush/Flanking already use.
        public float SubsystemCrippleChance = 0f;
        // T3 Great Houses Fleet Coordination - flag only, no specific backend numeric target identified.
        public bool GreatHousesFleetCoordination;
        // T5 Battle Cloak / T7 Adaptive Battle Cloak Refinement - see the shared GalaxyMapCloak/
        // CloakDetection pair below (§5b's cloak arc).
        public bool GalaxyMapCloak;
        // Tier-7 cloak upgrade (Klingon Adaptive Battle Cloak Refinement / Romulan Near-Perfect Cloak) -
        // defeats Tachyon Detection Grid outright, "unseen again" per §5b beat 4.
        public bool CloakDefeatsDetection;

        // ── Branch F: Romulan ───────────────────────────────────────────────────────────────────
        // T2 Tal Shiar Intelligence Matrix - see IntelSuccessBonus above (§5a's concrete landing spot).
        // T3 Adaptive Cloak Harmonics - flag only, precursor shield/sensor tuning, no numeric target yet.
        public bool AdaptiveCloakHarmonics;
        // T4 Basic Cloaking Field - sets GalaxyMapCloak (shared field above, §5b beat 1).
        // T5 Warbird Ambush Doctrine - first-strike bonus damage when decloaking to attack; consumed by
        // TurnBasedCombatResolver's opening-volley step for a cloaked Romulan fleet's first shot.
        public float WarbirdAmbushBonus = 0f;

        // ── Branch F: Borg ──────────────────────────────────────────────────────────────────────
        // T1/T4 Nanite Regeneration Matrix I/II - additive fractional bonus to StarSysManager.
        // ProcessRepairs' RepairHullPerTurn for Borg ships.
        public float NaniteRegenBonus = 0f;
        // T2/T3 Adaptive Shield Modulation I/II - flag/placeholder, folded into ShieldMultiplier via the
        // same shared-hook Max-semantics (no distinct backend target beyond the generic shield stat).
        // T5 Transwarp Hub Network - this civ (Borg only) can build/operate hub travel; see
        // TranswarpHubController. T7 Assimilation Protocols - chance to convert a kill into a captured
        // Borg asset instead of a wreck, applied in the post-combat resolution step.
        public bool TranswarpHubNetwork;
        public float AssimilationChance = 0f;

        // ── Branch F: Cardassian ────────────────────────────────────────────────────────────────
        // T4 Obsidian Order Surveillance Net - see IntelSuccessBonus above (§5a's concrete landing spot,
        // same GetCivSuccessModifier target as Romulan Tal Shiar).
        // T2/T3/T5/T7 - Logistics Optimization / Interrogation Algorithm Suites / Occupation Efficiency
        // Doctrine / Central Authority Infrastructure: no distinct backend numeric target identified in
        // §5a beyond the shared facility-speed plumbing; folded into FacilityOutputMultiplier below.
        public float FacilityOutputMultiplier = 1f;

        // ── Branch F: Terran Empire (Mirror) ────────────────────────────────────────────────────
        // T1 Agonizer Discipline Regimen - additive bonus stacked on top of TechManager.
        // GetFactorySpeedMultiplier for Terran specifically (§5a's concrete landing spot).
        public float FactorySpeedBonus = 0f;
        // T2/T4 Fear-Driven Command Protocols I/II - combat-morale bonus; consumed the same way as
        // Klingon's SubsystemCrippleChance (order-based damage-modifier step) but as a flat damage bonus
        // instead of a disable chance.
        public float CombatMoraleBonus = 0f;

        // ── Branch F: Dominion ──────────────────────────────────────────────────────────────────
        // T5 Changeling Infiltration Units - unlocks SecretActionsEnum.Infiltration as an action this
        // civ can initiate via IntelligenceManager.CreateIntelProject (§5a's "new verb" tech).
        public bool ChangelingInfiltration;

        // Every TechDefSO.Id already applied - guards ApplyTechEffect against double-applying a flag/
        // bonus if CompleteResearch is ever somehow invoked twice for the same tech (defensive only;
        // CompleteResearch already adds to ResearchedTechIds first, but flags/bonuses here have no
        // natural idempotency of their own the way a HashSet.Add does).
        public HashSet<string> AppliedTechIds = new();
    }
}

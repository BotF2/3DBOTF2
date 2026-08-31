namespace BOTF3D.Core
{
    /// <summary>
    /// One member per distinct backend mechanism a TechDefSO can plug into, dispatched from
    /// TechManager.ApplyTechEffect(civ, techDef) once that method exists (TechTree_Phase2_Design.md
    /// §6, §8 II.3 - not implemented yet, this enum is II.1's data-model piece only).
    ///
    /// Shared-branch members (TechFieldEnum.Propulsion/Tactical/Ordnance/Science/Intelligence) are
    /// named after the literal EffectHook column in TechTree_CommonBranches.csv, and are
    /// deliberately REUSED across more than one tier where that CSV reuses the same token (e.g.
    /// HullMultiplier at Tactical T1 and T3, OrdnanceUnlock at Ordnance T2/T4/T5/T6) - those techs
    /// are meant to invoke the same backend formula at a different magnitude/tier, per §3's "shared
    /// techs are balanced automatically by being singular assets" rule. Which specific magnitude or
    /// ordnance class a given reuse applies is resolved from the owning TechDefSO's own Id/Tier at
    /// ApplyTechEffect time, once that method is written - not encoded in this enum.
    ///
    /// FactionUnique members are one-per-tech (TechTree_FactionUnique.csv has no single-token
    /// EffectHook column, only free-text BackendTieIn notes) even where §5a documents two civs'
    /// techs landing on the same underlying call (e.g. Romulan TalShiarIntelligenceMatrix and
    /// Cardassian ObsidianOrderSurveillanceNet both target IntelligenceManager.GetCivSuccessModifier)
    /// - collapsing those into one shared member is an II.3 implementation choice, not a data-model
    /// one, so each civ's tech keeps its own identity here.
    /// </summary>
    public enum TechEffectHook
    {
        // ── Shared branches (TechTree_CommonBranches.csv) ──────────────────────────────────────
        // Propulsion
        PersistentWarpSpeed,
        WarpSpeedMultiplier,
        WarpSpeedAverage,
        WormholeStabilizer,
        AccessTranswarpHub,
        WarpLane,
        WarpSpeedMultiplier_Capstone,
        // Tactical
        HullMultiplier,
        ShieldMultiplier,
        CombatBurst,
        ShieldRegenMidCombat,
        EnvironmentalResist,
        ShieldMultiplier_Capstone,
        // Ordnance
        WeaponDamageMultiplier,
        OrdnanceUnlock,
        AccuracyBonus,
        WeaponDamageMultiplier_Capstone,
        // Science
        SightRangeStage_1,
        SightRangeStage_2,
        SightRangeStage_3,
        SightRangeStage_4,
        SightRangeStage_5,
        SightRangeStage_6_FacilityCap,
        SightRangeStage_7_Capstone,
        // Intelligence
        Decoy,
        MaskMovement,
        IntelPanelReveal_Partial,
        IntelPanelReveal_Full,
        SabotageResist,
        CloakDetection,
        IntelDashboard_Capstone,

        // ── Faction-unique, Branch F (TechTree_FactionUnique.csv) ──────────────────────────────
        // Federation
        FederationCharter,
        FirstContactProtocols,
        DiplomaticOutreachDoctrine,
        MinorCivAllianceDiscount,
        FederationScienceExchange,
        PositronicNeuralNetwork,
        FederationCharterMastery,
        // Klingon
        WarriorsCreed,
        IonizedHullPlating,
        DisruptorOverloadArrays,
        GreatHousesFleetCoordination,
        BattleCloak,
        DisruptorSubsystemCripple,
        AdaptiveBattleCloakRefinement,
        // Romulan
        CultureOfSecrecy,
        CloakFieldTheory,
        TalShiarIntelligenceMatrix,
        AdaptiveCloakHarmonics,
        BasicCloakingField,
        WarbirdAmbushDoctrine,
        NearPerfectCloak,
        // Borg
        TheCollective,
        NaniteRegenerationMatrixI,
        AdaptiveShieldModulationI,
        AdaptiveShieldModulationII,
        NaniteRegenerationMatrixII,
        TranswarpHubNetwork,
        AssimilationProtocols,
        // Cardassian
        ObedientSociety,
        ReinforcedDuraniumHulls,
        CardassianLogisticsOptimization,
        InterrogationAlgorithmSuites,
        ObsidianOrderSurveillanceNet,
        OccupationEfficiencyDoctrine,
        CentralAuthorityInfrastructure,
        // Terran Empire (Mirror)
        ImperialAmbition,
        AgonizerDisciplineRegimen,
        FearDrivenCommandProtocolsI,
        ImperialPhaserOvercharge,
        FearDrivenCommandProtocolsII,
        TerranEliteStrikeTeams,
        FlagshipDominationSystems,
        // Dominion
        FoundersDesign,
        KetracelWhiteOptimization,
        VortaCommandAlgorithms,
        PolaronBeamEnhancement,
        GammaQuadrantSupplyLattice,
        ChangelingInfiltrationUnits,
        CloningAccelerationChambers,
    }
}

using BOTF3D.Core;
using BOTF3D.UI;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.Civilization
{
    public class DiplomacyController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        private DiplomacyData diplomacyData; // holds civOne and two, diplomacy enum...
        public DiplomacyData DiplomacyData { get { return diplomacyData; } set { diplomacyData = value; } }
        private static string declareWar = "The A declares war on the B.";
        private static string requestSomething = "The A request X from the B.";
        private static string demandSomething = "The A demand X from the B.";
        private static string offerSomething = "The A offers the B X.";
        private static string demandStopInterferance = "The A demand that the B stop X.";

        private List<string> diplomaticTransmissions = new List<string> { declareWar, requestSomething, demandSomething, offerSomething, demandStopInterferance };
        public List<string> DiplomaticTransmissions { get { return diplomaticTransmissions; } set { diplomaticTransmissions = value; } }
        public List<DiplomaticEventEnum> DiplomaticEvents = new List<DiplomaticEventEnum>
        { DiplomaticEventEnum.War, DiplomaticEventEnum.DiscoveredSabotage, DiplomaticEventEnum.DiscoveredDisinformation, DiplomaticEventEnum.DiscoveredIntellectualTheft,
        DiplomaticEventEnum.Trade, DiplomaticEventEnum.ShareTech, DiplomaticEventEnum.GiveAid};
        public GameObject DiplomacyUIGameObject;

        // MonoBehaviour should not rely on parameterized constructors. Use Init(...) after AddComponent/Instantiate.
        public void Init(DiplomacyData data)
        {
            DiplomacyData = data;
        }

        public void DoAIDiplomacy()
        {
            if (GameController.Instance.AreWeLocalPlayer(this.DiplomacyData.CivEnumSideOne) || GameController.Instance.AreWeLocalPlayer(this.DiplomacyData.CivEnumSideTwo))
            {
                if (DiplomacyUIGameObject != null)
                    DiplomacyUIGameObject.SetActive(true);
                //ToDo: AI civ diplomacy actions for on or both civs that are AI.
            }
        }
        public void UpdateDiplomacyControllerData(DiplomacyData diplomacyData)
        {
            this.DiplomacyData = diplomacyData;
            ChangedDiplomacyStatus(this.DiplomacyData.DiplomacyPointsOfCivs);
        }
        public void AddDiplomaticPoints(int points)
        {
            this.DiplomacyData.DiplomacyPointsOfCivs += points;
            ChangedDiplomacyStatus(this.DiplomacyData.DiplomacyPointsOfCivs);
        }
        public void SubtractDiplomaticPoints(int points)
        {
            this.DiplomacyData.DiplomacyPointsOfCivs -= points;
            ChangedDiplomacyStatus(this.DiplomacyData.DiplomacyPointsOfCivs);
        }
        private void ChangedDiplomacyStatus(int currentStatusPoints)
        {
            if (currentStatusPoints < -20)
            {
                currentStatusPoints = -20;
            }

            if (currentStatusPoints >= (int)DiplomacyStatusEnum.Neutral && currentStatusPoints < (int)DiplomacyStatusEnum.Friendly)
            {
                this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Neutral;
            }
            else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Friendly && currentStatusPoints < (int)DiplomacyStatusEnum.Allied)
            {
                this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Friendly;
            }
            else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Allied && currentStatusPoints < (int)DiplomacyStatusEnum.Membership)
            {
                this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Allied;
            }
            else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Membership && ((int)this.DiplomacyData.CivEnumSideOne > 6 || (int)this.DiplomacyData.CivEnumSideTwo > 6))
            {
                // only minors AI civ can become member of a playable major race
                bool wasAlreadyMember = this.DiplomacyData.DiplomacyStatusEnumOfCivs == DiplomacyStatusEnum.Membership;
                this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Membership;
                if (!wasAlreadyMember)
                    AnnexMinorIntoMajor();
            }
            else if (currentStatusPoints >= (int)DiplomacyStatusEnum.UnFriendly && currentStatusPoints < (int)DiplomacyStatusEnum.Neutral)
            {
                this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.UnFriendly;
            }
            else if (currentStatusPoints >= (int)DiplomacyStatusEnum.Hostile && currentStatusPoints < (int)DiplomacyStatusEnum.UnFriendly)
            {
                this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.Hostile;
            }
            else if (currentStatusPoints >= (int)DiplomacyStatusEnum.ColdWar && currentStatusPoints < (int)DiplomacyStatusEnum.Hostile)
            {
                this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.ColdWar;
            }
            else if (currentStatusPoints >= (int)DiplomacyStatusEnum.War)
            {
                bool wasAlreadyAtWar = this.DiplomacyData.DiplomacyStatusEnumOfCivs == DiplomacyStatusEnum.War;
                this.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.War;
                if (!wasAlreadyAtWar)
                    TriggerWarModeForAIControlledSystems();
            }
        }

        /// <summary>
        /// When a pair first drops to War status - whether from the DeclareWar button or
        /// relations decaying there through sabotage/other gestures - any side of the pair
        /// that isn't the local player is switched to War build mode across all its systems.
        /// The human player's own systems are left alone; they choose War mode manually.
        /// </summary>
        private void TriggerWarModeForAIControlledSystems()
        {
            SetWarModeIfAIControlled(DiplomacyData.CivEnumSideOne);
            SetWarModeIfAIControlled(DiplomacyData.CivEnumSideTwo);
        }

        private static void SetWarModeIfAIControlled(CivEnum civEnum)
        {
            if (GameController.Instance.AreWeLocalPlayer(civEnum)) return;

            CivController civ = CivManager.Instance.GetCivControllerByCivEnum(civEnum);
            if (civ?.CivData?.StarSysWeOwn == null) return;

            foreach (var sysCon in civ.CivData.StarSysWeOwn)
            {
                if (sysCon?.StarSysData != null)
                    sysCon.StarSysData.AIBuildMode = AIBuildMode.War;
            }
        }

        /// <summary>
        /// Explicit declaration of war: forces this pair straight to War status (skipping
        /// the usual gradual point decay) and ends any standing cooperation pact. The Borg
        /// don't use diplomacy at all - they're never "at peace" to declare war from.
        /// </summary>
        public void DeclareWar(DiplomacyController diplomacyController)
        {
            if (DiplomacyData.CivEnumSideOne == CivEnum.BORG || DiplomacyData.CivEnumSideTwo == CivEnum.BORG)
            {
                Debug.Log("[Diplomacy] DeclareWar rejected: the Borg do not engage in diplomacy.");
                return;
            }

            DiplomacyData.CooperationPactActive = false;
            DiplomacyData.DiplomacyPointsOfCivs = (int)DiplomacyStatusEnum.War;
            ChangedDiplomacyStatus(DiplomacyData.DiplomacyPointsOfCivs);
        }
        /// <summary>
        /// Full immediate annexation: whichever side of this pair is a minor race (CivEnum > 6)
        /// hands every system it owns to the major civ. Diplomacy pairs always have exactly one
        /// major side (DiplomacyManager only creates a pair when one side is a playable major civ).
        /// </summary>
        private void AnnexMinorIntoMajor()
        {
            bool sideOneIsMinor = (int)DiplomacyData.CivEnumSideOne > 6;
            CivEnum minorEnum = sideOneIsMinor ? DiplomacyData.CivEnumSideOne : DiplomacyData.CivEnumSideTwo;
            CivEnum majorEnum = sideOneIsMinor ? DiplomacyData.CivEnumSideTwo : DiplomacyData.CivEnumSideOne;
            CivManager.Instance.AnnexMinorCiv(majorEnum, minorEnum);
            DiplomacyData.CooperationPactActive = false; // pact fulfilled - stop passive drift on the now-defunct minor civ
        }

        // Base point gains before DiplomaticAptitude scaling — bigger commitments swing relations more.
        private const int TradePointGain = 5;
        private const int EngagementPointGain = 3;
        private const int TechPointGain = 8;
        private const int AidPointGain = 6;
        private const int AlliancePointGain = 10;

        public void ProposeTrade(DiplomacyController diplomacyController)
        {
            ApplyDiplomaticGesture(TradePointGain);
        }
        public void Engagement(DiplomacyController dilomacyCon)
        {
            ApplyDiplomaticGesture(EngagementPointGain);
        }
        public void ProposeTech(DiplomacyController dilomacyCon)
        {
            ApplyDiplomaticGesture(TechPointGain);
        }
        public void SendAid(DiplomacyController diplomacyController)
        {
            ApplyDiplomaticGesture(AidPointGain);
        }
        public void OfferAlliance(DiplomacyController diplomacyController)
        {
            // Can't leap straight to Allied from a cold relationship - build up through Friendly first.
            if (DiplomacyData.DiplomacyStatusEnumOfCivs < DiplomacyStatusEnum.Friendly)
            {
                Debug.Log($"[Diplomacy] Alliance offer rejected: {DiplomacyData.CivEnumSideOne} and " +
                    $"{DiplomacyData.CivEnumSideTwo} aren't Friendly yet.");
                return;
            }

            // Minor-major pairs (exactly one side is a minor race, CivEnum > 6): Alliance opens a
            // standing cooperation pact - tech/trade/military ties that build trust passively every
            // turn (see TickCooperationPactDrift) toward Membership/annexation, rather than a single
            // point gesture. Major-major pairs keep the existing one-off gesture behavior; reaching
            // Allied there represents a mutual-defense treaty, with no annexation path (already
            // gated by AnnexMinorIntoMajor requiring a minor side).
            bool sideOneMinor = (int)DiplomacyData.CivEnumSideOne > 6;
            bool sideTwoMinor = (int)DiplomacyData.CivEnumSideTwo > 6;
            if (sideOneMinor != sideTwoMinor && !DiplomacyData.CooperationPactActive)
            {
                DiplomacyData.CooperationPactActive = true;
                Debug.Log($"[Diplomacy] Cooperation pact opened between {DiplomacyData.CivEnumSideOne} and " +
                    $"{DiplomacyData.CivEnumSideTwo} - trust will now build over time toward Federation membership.");
            }

            ApplyDiplomaticGesture(AlliancePointGain);
        }

        /// <summary>
        /// Passive per-turn trust growth for an active minor-major cooperation pact. Scaled by the
        /// major civ's DiplomaticAptitude (how eagerly it courts closer ties) and the minor civ's own
        /// Xenophobia/DiplomaticAptitude (how receptive it is to integrating with outsiders) - a very
        /// xenophobic minor race warms up much more slowly than a Vulcan-like curious/cooperative one.
        /// Always non-negative; only sabotage or (future) random events should push trust backward.
        /// </summary>
        private const float CooperationBaseDrift = 2f;
        public void TickCooperationPactDrift(CivData majorCiv, CivData minorCiv)
        {
            float majorFactor = 1f + majorCiv.DiplomaticAptitude * 0.25f;               // ~0.5x-1.5x
            float minorReceptivity = ((int)minorCiv.Xenophobia + minorCiv.DiplomaticAptitude) / 2f; // -2..+2
            float minorFactor = 1f + minorReceptivity * 0.35f;                          // ~0.3x-1.7x

            int drift = Mathf.Max(1, Mathf.RoundToInt(CooperationBaseDrift * majorFactor * minorFactor));
            AddDiplomaticPoints(drift);
        }

        /// <summary>
        /// Scales a diplomacy-menu gesture's point gain by the proposing civ's DiplomaticAptitude
        /// (derived from Warlike/Xenophobia/Ruthless/Greedy), so Federation-like civs get noticeably
        /// more goodwill per gesture than Romulan/Cardassian/Dominion-like civs.
        /// </summary>
        private void ApplyDiplomaticGesture(int basePoints)
        {
            // The Borg do not negotiate - they have no diplomacy, only assimilation via combat.
            if (DiplomacyData.CivEnumSideOne == CivEnum.BORG || DiplomacyData.CivEnumSideTwo == CivEnum.BORG)
            {
                Debug.Log("[Diplomacy] Gesture rejected: the Borg do not engage in diplomacy.");
                return;
            }

            CivController proposer = CivManager.Instance.GetCivControllerByCivEnum(DiplomacyData.CivEnumSideOne);
            if (proposer?.CivData == null) return;

            float multiplier = 1f + proposer.CivData.DiplomaticAptitude * 0.2f; // ~0.6x-1.4x
            int gain = Mathf.Max(1, Mathf.RoundToInt(basePoints * multiplier));
            AddDiplomaticPoints(gain);
        }
        public void GatherIntel(DiplomacyController diplomacyController)
        {
            IntelligenceManager.Instance.CreateIntelProject(
                SecretActionsEnum.GatherIntelligence,
                diplomacyController.DiplomacyData.CivEnumSideOne,
                diplomacyController.DiplomacyData.CivEnumSideTwo, out _);
        }
        public void Theft(DiplomacyController diplomacyController)
        {
            IntelligenceManager.Instance.CreateIntelProject(
                SecretActionsEnum.IntellectualTheft,
                diplomacyController.DiplomacyData.CivEnumSideOne,
                diplomacyController.DiplomacyData.CivEnumSideTwo, out _);
        }
        public void Disinformation(DiplomacyController diplomacyController)
        {
            IntelligenceManager.Instance.CreateIntelProject(
                SecretActionsEnum.Disinformation,
                diplomacyController.DiplomacyData.CivEnumSideOne,
                diplomacyController.DiplomacyData.CivEnumSideTwo, out _);
        }
        public void Sabatoge(DiplomacyController diplomacyController)
        {
            IntelligenceManager.Instance.CreateIntelProject(
                SecretActionsEnum.Sabotage,
                diplomacyController.DiplomacyData.CivEnumSideOne,
                diplomacyController.DiplomacyData.CivEnumSideTwo, out _);
        }
        public void SystemRecon(DiplomacyController diplomacyController)
        {
            IntelligenceManager.Instance.CreateIntelProject(
                SecretActionsEnum.SystemRecon,
                diplomacyController.DiplomacyData.CivEnumSideOne,
                diplomacyController.DiplomacyData.CivEnumSideTwo, out _);
        }
        public void Combat(DiplomacyController diplomacyController)
        {
            // ToDo: include orbital batteries and shields in combat, see ValidCombatCheck()
            if (diplomacyController.DiplomacyData.CombatIntiated != true && ValidCombatCheck(diplomacyController.DiplomacyData))
            {
                diplomacyController.DiplomacyData.CombatIntiated = true;

                // ✅ CRITICAL: Close diplomacy menu AND clear the open menu tracking
                // This prevents the system from trying to re-open the wrong menu
                GalaxyMenuUIController.Instance.CloseMenu(Menu.DiplomacyMenu);
                GalaxyMenuUIController.Instance.CloseMenu(Menu.ADiplomacyMenu); // Also close the individual diplomacy view

                // ✅ Force close ALL menus to prevent UI conflicts
                GalaxyMenuUIController.Instance.CloseAllMenus();

                Debug.Log($"✅ Diplomacy closed, requesting combat scene...");

                // SceneController.LoadCombatScene is not itself networked - it's a purely local scene
                // load. FleetControllerCivOne is always a real, networked fleet (see ValidCombatCheck),
                // so route the request through it: RequestStartCombat relays to the server if needed
                // and the server broadcasts the result to every client via RpcStartCombat, so both
                // combatants' clients load the Combat scene together instead of just whichever one
                // clicked the button.
                diplomacyController.DiplomacyData.FleetControllerCivOne.RequestStartCombat(
                    diplomacyController.DiplomacyData.FleetContollerCivTwo,
                    diplomacyController.DiplomacyData.StarSysController
                );
            }
            //*******load combat menu for local player and do AI civs
        }

        private bool ValidCombatCheck(DiplomacyData diplomacyData)
        {
            // FleetControllerCivOne/CivTwo are sometimes a placeholder "empty" fleet controller
            // (see FleetManager.InsatiateEmptyFleetController) standing in for a defender that's
            // actually system-docked rather than fleeted - its FleetData is never assigned, so it must
            // be null-checked here too, not just the FleetController reference itself.
            bool sideOneHasShips = diplomacyData.FleetControllerCivOne != null &&
                diplomacyData.FleetControllerCivOne.FleetData != null &&
                diplomacyData.FleetControllerCivOne.FleetData.ShipsList.Count > 0;
            bool sideTwoHasShips = diplomacyData.FleetContollerCivTwo != null &&
                diplomacyData.FleetContollerCivTwo.FleetData != null &&
                diplomacyData.FleetContollerCivTwo.FleetData.ShipsList.Count > 0;
            bool systemHasShips = diplomacyData.StarSysController != null &&
                diplomacyData.StarSysController.StarSysData != null &&
                diplomacyData.StarSysController.StarSysData.ShipsList.Count > 0;

            return (sideOneHasShips && sideTwoHasShips) ||
                   (sideOneHasShips && systemHasShips) ||
                   (sideTwoHasShips && systemHasShips);
        }

        internal void ResolveFleetToStrangGalacticEncounter(DiplomacyController diplomacyController)
        {
            GalaxyMenuUIController.Instance.OpenMenu(Menu.ADiplomacyMenu, this.DiplomacyUIGameObject);
        }

        internal void ResolveUninhabitedSystem(CivController realCivController, StarSysController uninhabitedSysCon)
        {
            // UI for uninhabited system management
            if (GameController.Instance.AreWeLocalPlayer(realCivController.CivData.CivEnum))
                uninhabitedSysCon.DoHabitalbeSystemUI(realCivController);
            else
            {
                // do AI uninhabited system management
            }
        }
        public void CleanupDestroyedUIs()
        {
            foreach (var diplomacyCon in DiplomacyManager.Instance.DiplomacyControllers)
            {
                if (diplomacyCon.DiplomacyUIGameObject == null)
                    continue;

                if (!diplomacyCon.DiplomacyUIGameObject.activeInHierarchy)
                {
                    diplomacyCon.DiplomacyUIGameObject = null;
                }
            }
        }
    }
}

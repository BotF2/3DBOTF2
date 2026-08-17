using BOTF3D.Core;
using BOTF3D.UI;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Galaxy;
using BOTF3D.Audio;
using Mirror;



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
            }

            // AI responses are server-authoritative only - there's no LocalHumanPlayerController to
            // relay an AI civ's response through (see FleetController.CmdSetEncounterResponse's
            // authorization check). Every client still runs DoAIDiplomacy (to open its own local UI
            // above if it's playing against this AI), but only the server applies/broadcasts the
            // actual Fight/Withdraw decision, via the [Server] entry point that skips the Cmd relay.
            if (!NetworkServer.active) return;

            // CivData.PlayedByAI defaults to true and is never actually set false for the local
            // human player's own civ anywhere in the codebase, so it can't be trusted alone to tell
            // "this side is AI" from "this side is the human". AreWeLocalPlayer is the same signal
            // already used above to decide whether to show the UI, so use it here too to exclude
            // whichever side is actually the human.
            //
            // Sides must also be checked independently, not as an either/or: the previous
            // `!aiIsSideOne &&` short-circuit meant side two's response was only ever forced when
            // side one was NOT AI-controlled. Since PlayedByAI is always true, aiIsSideOne was always
            // true too, so side two (the actual other party in the encounter - human or AI) never
            // got a forced response at all. Whenever the human's civ landed in "side one" (purely by
            // CivEnum ordering), that human's own side got silently auto-resolved on encounter
            // creation while the real opposing civ (side two) stayed Undecided forever - Withdraw
            // could never fully resolve no matter what the human clicked, and the fleet stayed
            // frozen (see FleetController.IsAwaitingEncounterResolution).
            bool aiIsSideOne = this.DiplomacyData.CivOne != null && this.DiplomacyData.CivOne.CivData.PlayedByAI &&
                !GameController.Instance.AreWeLocalPlayer(this.DiplomacyData.CivOne.CivData.CivEnum);
            bool aiIsSideTwo = this.DiplomacyData.CivTwo != null && this.DiplomacyData.CivTwo.CivData.PlayedByAI &&
                !GameController.Instance.AreWeLocalPlayer(this.DiplomacyData.CivTwo.CivData.CivEnum);
            if (!aiIsSideOne && !aiIsSideTwo) return;

            if (aiIsSideOne) ServerForceResponse(true, DefaultResponseForCurrentStatus());
            if (aiIsSideTwo) ServerForceResponse(false, DefaultResponseForCurrentStatus());
        }

        // Status-based default: fight if relations are already Hostile or worse, otherwise withdraw
        // rather than escalate. Used both for AI civs' own decisions (DoAIDiplomacy) and to fill in
        // for a side that never responded in time (see UnresponsiveSideTimeoutSeconds below).
        private DiplomacyData.EncounterResponse DefaultResponseForCurrentStatus()
        {
            return this.DiplomacyData.DiplomacyStatusEnumOfCivs <= DiplomacyStatusEnum.Hostile
                ? DiplomacyData.EncounterResponse.Fight
                : DiplomacyData.EncounterResponse.Withdraw;
        }

        // Applies an authoritative response to one side of this encounter, server-side. Shared by
        // DoAIDiplomacy (AI civ's own decision) and the unresponsive-side timeout (Update below).
        private void ServerForceResponse(bool isSideOne, DiplomacyData.EncounterResponse response)
        {
            FleetController targetFleet = isSideOne ? this.DiplomacyData.FleetControllerCivOne : this.DiplomacyData.FleetContollerCivTwo;
            FleetController otherFleet = isSideOne ? this.DiplomacyData.FleetContollerCivTwo : this.DiplomacyData.FleetControllerCivOne;

            if (targetFleet != null)
            {
                targetFleet.ServerSetEncounterResponse(isSideOne, response, otherFleet, this.DiplomacyData.StarSysController);
            }
            else if (otherFleet != null)
            {
                // This side is the star system's own defenders, not a moving fleet -
                // StarSysController has no NetworkBehaviour of its own to carry the ClientRpc. Relay
                // through the other side's real fleet instead: RpcSetEncounterResponse only needs
                // *a* networked FleetController to broadcast through, since the response applies to
                // isSideOne/isSideTwo, not to whichever object carried the call. Pass
                // otherFleetCon:null + StarSysController (instead of otherFleet again) so the
                // client-side lookup in RpcSetEncounterResponse resolves "the other side" back to
                // the system's owning civ, not to otherFleet's own civ.
                //
                // Without this fallback, targetFleet==null used to just return here, leaving that
                // side permanently Undecided - TryResolveEncounter can never reach its Withdraw
                // branch (which requires BOTH sides), so a fleet that met an AI-owned system and
                // got Undecided back from the defenders would stay frozen forever with no error.
                otherFleet.ServerSetEncounterResponse(isSideOne, response, null, this.DiplomacyData.StarSysController);
            }
        }

        // How long a Fight/Withdraw decision waits on an unresponsive side (human who never clicks,
        // or - defensively - any other silent failure) before the game forces the same status-based
        // default an AI civ would pick, rather than freezing the involved fleet(s) forever.
        private const float UnresponsiveSideTimeoutSeconds = 60f;

        // Server-only: forces a default response onto whichever side is still Undecided once the
        // encounter has been open longer than UnresponsiveSideTimeoutSeconds. Covers the case where
        // a human player simply never opens or answers the Diplomacy popup (e.g. AFK) - AI civs
        // already respond immediately via DoAIDiplomacy, so in practice this only ever fires for a
        // human side, but it isn't restricted to that in case of some other stall.
        private void Update()
        {
            if (!NetworkServer.active) return;
            if (this.DiplomacyData == null || this.DiplomacyData.EncounterResolved) return;
            if (this.DiplomacyData.ResponseSideOne != DiplomacyData.EncounterResponse.Undecided &&
                this.DiplomacyData.ResponseSideTwo != DiplomacyData.EncounterResponse.Undecided) return;

            // Only a genuine Fight/Withdraw encounter actually freezes a fleet (see
            // FleetController.IsAwaitingEncounterResolution / ServerIncrementPendingEncounters).
            // DiplomacyController instances also back plain UI browsing - a player revisiting a
            // known system/fleet via ResolveDiplomacyForClickSystemWeKnow, or an uninhabited-system
            // contact - which also flow through OpenDiplomacyUI's Undecided/EncounterStartRealTime
            // reset but never pause anything. Without this check, idly leaving one of those popups
            // open past the timeout would force an unwanted Fight/Withdraw and could even trigger
            // combat.
            bool sideOneAwaiting = this.DiplomacyData.FleetControllerCivOne != null && this.DiplomacyData.FleetControllerCivOne.IsAwaitingEncounterResolution;
            bool sideTwoAwaiting = this.DiplomacyData.FleetContollerCivTwo != null && this.DiplomacyData.FleetContollerCivTwo.IsAwaitingEncounterResolution;
            if (!sideOneAwaiting && !sideTwoAwaiting) return;

            if (Time.realtimeSinceStartup - this.DiplomacyData.EncounterStartRealTime < UnresponsiveSideTimeoutSeconds) return;

            DiplomacyData.EncounterResponse fallback = DefaultResponseForCurrentStatus();
            if (this.DiplomacyData.ResponseSideOne == DiplomacyData.EncounterResponse.Undecided)
                ServerForceResponse(true, fallback);
            if (this.DiplomacyData.ResponseSideTwo == DiplomacyData.EncounterResponse.Undecided)
                ServerForceResponse(false, fallback);
        }

        // Called by a Fight/Withdraw UI click (isSideOne = true if the clicking player is CivOne in
        // this DiplomacyData), or applied locally when a network broadcast carries the authoritative
        // decision (relayToNetwork = false - see FleetController.RpcSetEncounterResponse). Relaying
        // is what actually reaches the server and comes back around via that Rpc; TryResolveEncounter
        // only runs once the broadcast is applied, not on the optimistic local click, so the fleets'
        // pending-encounter counters (server-authoritative SyncVars) are never decremented twice.
        public void SetResponse(bool isSideOne, DiplomacyData.EncounterResponse response, bool relayToNetwork = true)
        {
            if (isSideOne) this.DiplomacyData.ResponseSideOne = response;
            else this.DiplomacyData.ResponseSideTwo = response;

            if (relayToNetwork)
            {
                FleetController callerFleet = isSideOne ? this.DiplomacyData.FleetControllerCivOne : this.DiplomacyData.FleetContollerCivTwo;
                FleetController otherFleet = isSideOne ? this.DiplomacyData.FleetContollerCivTwo : this.DiplomacyData.FleetControllerCivOne;
                callerFleet?.RequestSetEncounterResponse(isSideOne, response, otherFleet, this.DiplomacyData.StarSysController);
            }
            else
            {
                TryResolveEncounter();
            }
        }

        // Called when the player dismisses the Diplomacy panel via the generic close/X button
        // instead of an explicit Fight/Withdraw/etc. choice (see GalaxyMenuUIController.
        // CloseCurrentMenu). Without this, a closed-without-deciding encounter left the local
        // player's side Undecided, and the only thing that would ever unstick the fleet(s)
        // involved (FleetController.IsAwaitingEncounterResolution gates FixedUpdate movement
        // entirely) was DiplomacyController.Update()'s 60-real-time-second unresponsive-side
        // timeout - technically not a permanent freeze, but indistinguishable from one to a
        // player who closes the panel and immediately queues a new order the same turn.
        // Mirrors WithdrawButton's isSideOne resolution (DiplomacyMenuUIController) exactly.
        // No-ops if the encounter is already resolved or this side already has an explicit
        // answer, so it can't override a Fight choice made just before closing.
        public void CloseWithoutDeciding()
        {
            if (this.DiplomacyData == null || this.DiplomacyData.EncounterResolved) return;

            bool isSideOne = this.DiplomacyData.CivOne != null &&
                GameController.Instance.AreWeLocalPlayer(this.DiplomacyData.CivOne.CivData.CivEnum);
            bool isSideTwo = !isSideOne && this.DiplomacyData.CivTwo != null &&
                GameController.Instance.AreWeLocalPlayer(this.DiplomacyData.CivTwo.CivData.CivEnum);
            if (!isSideOne && !isSideTwo) return; // local player isn't a party to this encounter

            DiplomacyData.EncounterResponse currentResponse = isSideOne
                ? this.DiplomacyData.ResponseSideOne
                : this.DiplomacyData.ResponseSideTwo;
            if (currentResponse != DiplomacyData.EncounterResponse.Undecided) return;

            SetResponse(isSideOne, DiplomacyData.EncounterResponse.Withdraw);
        }

        // Either side choosing Fight forces combat; both choosing Withdraw releases both fleets to
        // continue their prior movement. Any Undecided response leaves the encounter paused.
        public void TryResolveEncounter()
        {
            if (this.DiplomacyData.EncounterResolved) return;

            if (this.DiplomacyData.ResponseSideOne == DiplomacyData.EncounterResponse.Fight ||
                this.DiplomacyData.ResponseSideTwo == DiplomacyData.EncounterResponse.Fight)
            {
                this.DiplomacyData.EncounterResolved = true;

                // Close the encounter dialog unconditionally, on every peer, the moment Fight is
                // decided - independent of Combat()'s own ValidCombatCheck() gate below. That gate
                // gave inconsistent results per-peer (e.g. Player 2's local view of the encounter's
                // FleetController/StarSysController ship-list SyncVars isn't guaranteed to already
                // match the host's the instant this runs), so on whichever peer it failed, Combat()
                // never reached its own CloseMenu/CloseAllMenus calls and Menu.ADiplomacyMenu stayed
                // the tracked "open" menu for the rest of combat - reappearing, still showing the
                // stale pre-combat encounter view, the moment GalaxyScene's root objects were
                // reactivated after combat ended.
                GalaxyMenuUIController.Instance.CloseMenu(Menu.DiplomacyMenu);
                GalaxyMenuUIController.Instance.CloseMenu(Menu.ADiplomacyMenu);
                GalaxyMenuUIController.Instance.CloseAllMenus();

                Combat(this);
                return;
            }

            if (this.DiplomacyData.ResponseSideOne == DiplomacyData.EncounterResponse.Withdraw &&
                this.DiplomacyData.ResponseSideTwo == DiplomacyData.EncounterResponse.Withdraw)
            {
                this.DiplomacyData.EncounterResolved = true;

                GalaxyMenuUIController.Instance.CloseMenu(Menu.DiplomacyMenu);
                GalaxyMenuUIController.Instance.CloseMenu(Menu.ADiplomacyMenu);

                this.DiplomacyData.FleetControllerCivOne?.ServerDecrementPendingEncounters();
                this.DiplomacyData.FleetContollerCivTwo?.ServerDecrementPendingEncounters();
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

        /// <summary>
        /// UI-facing toggle for DiplomacyData.AutoImproveRelations - ready to wire to a Toggle's
        /// OnValueChanged on the Diplomacy card once one exists there (see
        /// DiplomacyManager.ProcessAutoImproveRelations for what it actually does).
        /// </summary>
        public void SetAutoImproveRelations(bool enabled)
        {
            DiplomacyData.AutoImproveRelations = enabled;
            Debug.Log($"[Diplomacy] Auto-improve relations with {DiplomacyData.CivEnumSideTwo} " +
                (enabled ? "enabled." : "disabled."));
        }
        private void ChangedDiplomacyStatus(int currentStatusPoints)
        {
            DiplomacyStatusEnum previousStatus = this.DiplomacyData.DiplomacyStatusEnumOfCivs;

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

            if (this.DiplomacyData.DiplomacyStatusEnumOfCivs != previousStatus)
            {
                GameEvents.DiplomacyChanged(DiplomacyData.CivEnumSideOne, DiplomacyData.CivEnumSideTwo,
                    this.DiplomacyData.DiplomacyStatusEnumOfCivs);
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
            SendProposal(NegotiationPloysEnum.OfferTrade, "Trade");
        }
        public void Engagement(DiplomacyController dilomacyCon)
        {
            SendProposal(NegotiationPloysEnum.OfferCulturalExchange, "Cultural Exchange");
        }
        public void ProposeTech(DiplomacyController dilomacyCon)
        {
            SendProposal(NegotiationPloysEnum.OfferTech, "Tech Bargain");
        }
        public void SendAid(DiplomacyController diplomacyController)
        {
            SendProposal(NegotiationPloysEnum.OfferAid, "Aid");
        }
        public void OfferAlliance(DiplomacyController diplomacyController)
        {
            // Minor-major pairs (exactly one side is a minor race, CivEnum > 6): Alliance opens a
            // standing cooperation pact - tech/trade/military ties that build trust passively every
            // turn (see TickCooperationPactDrift) toward Membership/annexation. That's already the
            // "this takes time" mechanic for minor-major, so it keeps its instant
            // ApplyDiplomaticGesture kick-off and bypasses the proposal system below entirely.
            bool sideOneMinor = (int)DiplomacyData.CivEnumSideOne > 6;
            bool sideTwoMinor = (int)DiplomacyData.CivEnumSideTwo > 6;
            if (sideOneMinor != sideTwoMinor)
            {
                // The Borg do not negotiate, and can't leap straight to Allied from a cold
                // relationship - build up through Friendly first. CreateDiplomacyProject checks
                // both of these too, but this branch never reaches it, so it needs its own checks.
                if (DiplomacyData.CivEnumSideOne == CivEnum.BORG || DiplomacyData.CivEnumSideTwo == CivEnum.BORG)
                {
                    Debug.Log("[Diplomacy] Alliance offer rejected: the Borg do not engage in diplomacy.");
                    return;
                }
                if (DiplomacyData.DiplomacyStatusEnumOfCivs < DiplomacyStatusEnum.Friendly)
                {
                    Debug.Log($"[Diplomacy] Alliance offer rejected: {DiplomacyData.CivEnumSideOne} and " +
                        $"{DiplomacyData.CivEnumSideTwo} aren't Friendly yet.");
                    return;
                }

                if (!DiplomacyData.CooperationPactActive)
                {
                    DiplomacyData.CooperationPactActive = true;
                    Debug.Log($"[Diplomacy] Cooperation pact opened between {DiplomacyData.CivEnumSideOne} and " +
                        $"{DiplomacyData.CivEnumSideTwo} - trust will now build over time toward Federation membership.");
                }

                ApplyDiplomaticGesture(AlliancePointGain);
                return;
            }

            // Major-major pairs: a mutual-defense treaty is the biggest ask in the game, so it's a
            // real proposal the other civ can accept or reject over a few turns instead of an
            // instant guaranteed point gain - see DiplomacyManager.CreateDiplomacyProject, which
            // covers the Borg-exclusion and Friendly-gate checks for this path.
            SendProposal(NegotiationPloysEnum.OfferAlliance, "Alliance");
        }

        /// <summary>
        /// Shared "fire a DiplomacyProject and log the outcome" helper for every proposal-driven
        /// gesture button above. CivEnumSideOne is this codebase's established "proposer" convention
        /// (see ApplyDiplomaticGesture below and DiplomacyManager.ProcessAIDiplomacyForAllCivs).
        /// </summary>
        private void SendProposal(NegotiationPloysEnum proposalType, string proposalLabel)
        {
            if (DiplomacyManager.Instance.CreateDiplomacyProject(proposalType,
                DiplomacyData.CivEnumSideOne, DiplomacyData.CivEnumSideTwo, out string failReason))
            {
                // Clear the previous proposal's result so it doesn't linger under a new "PENDING"
                // line, and refresh the card immediately - otherwise the button click gives no
                // visible feedback at all until the next turn tick or resolve event.
                DiplomacyData.LastProposalOutcome = null;
                Debug.Log($"[Diplomacy] {proposalLabel} proposal sent: {DiplomacyData.CivEnumSideOne} → {DiplomacyData.CivEnumSideTwo}.");
                DiplomacyMenuUIController.Instance?.RefreshActiveProposalDisplay(this);
            }
            else
            {
                Debug.Log($"[Diplomacy] {proposalLabel} proposal could not be sent: {failReason}");
            }
        }

        /// <summary>
        /// Applies the standing effect of a DiplomacyProject the target civ accepted - called by
        /// DiplomacyManager.ResolveDiplomacyProject once the proposal's turn timer runs out. Kept
        /// separate from OfferAlliance/etc. so it can be extended to other proposal types later
        /// without touching the button-facing methods.
        /// </summary>
        public void ApplyAcceptedProposal(NegotiationPloysEnum proposalType)
        {
            switch (proposalType)
            {
                case NegotiationPloysEnum.OfferAlliance:
                    ApplyDiplomaticGesture(AlliancePointGain);
                    break;
                case NegotiationPloysEnum.OfferTrade:
                    ApplyDiplomaticGesture(TradePointGain);
                    break;
                case NegotiationPloysEnum.OfferTech:
                    ApplyDiplomaticGesture(TechPointGain);
                    break;
                case NegotiationPloysEnum.OfferAid:
                    ApplyDiplomaticGesture(AidPointGain);
                    break;
                case NegotiationPloysEnum.OfferCulturalExchange:
                    ApplyDiplomaticGesture(EngagementPointGain);
                    break;
            }
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
                // load. RequestStartCombat must be called on a real, network-spawned FleetController;
                // ValidCombatCheck guarantees at least one of CivOne/CivTwo is real, but which one
                // depends on which side sorted first by CivEnum (see InstantiateDiplomacyController) -
                // e.g. a system-defender encounter (Borg, or any fleet-vs-system fight) only ever has
                // one real fleet, and it can land on either side. Pick whichever side is actually real.
                FleetController callerFleet = diplomacyController.DiplomacyData.FleetControllerCivOne != null
                    ? diplomacyController.DiplomacyData.FleetControllerCivOne
                    : diplomacyController.DiplomacyData.FleetContollerCivTwo;
                FleetController otherFleet = callerFleet == diplomacyController.DiplomacyData.FleetControllerCivOne
                    ? diplomacyController.DiplomacyData.FleetContollerCivTwo
                    : diplomacyController.DiplomacyData.FleetControllerCivOne;

                // RequestStartCombat relays to the server if needed and the server broadcasts the
                // result to every client via RpcStartCombat, so both combatants' clients load the
                // Combat scene together instead of just whichever one clicked the button.
                if (callerFleet != null)
                {
                    callerFleet.RequestStartCombat(
                        otherFleet,
                        diplomacyController.DiplomacyData.StarSysController
                    );
                }
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

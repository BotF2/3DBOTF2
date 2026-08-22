using BOTF3D.Combat;

using BOTF3D.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.Civilization
{
    public class DiplomacyManager : MonoBehaviour, IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        public static DiplomacyManager Instance;
        public List<DiplomacyController> DiplomacyControllers { get; private set; } = new List<DiplomacyController>();
        [SerializeField]
        private GameObject diplomacyUIPrefab;
        [SerializeField]
        private GameObject diplomacControllerPrefab;

        private void Awake()
        {
            ServiceLocator.Register<DiplomacyManager>(this);
            if (Instance != null) { Destroy(gameObject); }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        public void Start()
        {
            // galaxyEventCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;

        }

        // Fires after every DiplomacyProject resolves: (proposalType, proposer, target, accepted).
        // Mirrors IntelligenceManager.OnProjectResolved so UI can hook it the same way.
        public static event System.Action<NegotiationPloysEnum, CivEnum, CivEnum, bool> OnDiplomacyProjectResolved;

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                // TickDiplomacyProjects registered first (mirrors IntelligenceManager's
                // TickIntelProjects-before-ProcessAIIntelForAllCivs order): ages proposals already
                // pending before any new one gets created this turn, so a project an AI civ starts
                // this turn always waits at least one full OnTurnAdvanced cycle before resolving,
                // instead of ticking its 1-turn duration down to 0 in the same call it was created.
                TimeManager.Instance.OnTurnAdvanced += TickDiplomacyProjects;
                TimeManager.Instance.OnTurnAdvanced += ProcessAIDiplomacyForAllCivs;
                TimeManager.Instance.OnTurnAdvanced += ProcessAutoImproveRelations;
                TimeManager.Instance.OnTurnAdvanced += ProcessCooperationPactDrift;
            }
            GameEvents.OnCombatEnded += HandleCombatEnded;
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTurnAdvanced -= TickDiplomacyProjects;
                TimeManager.Instance.OnTurnAdvanced -= ProcessAIDiplomacyForAllCivs;
                TimeManager.Instance.OnTurnAdvanced -= ProcessAutoImproveRelations;
                TimeManager.Instance.OnTurnAdvanced -= ProcessCooperationPactDrift;
            }
            GameEvents.OnCombatEnded -= HandleCombatEnded;
        }

        // Base magnitude of the direct point hit a losing civ takes against the civ that just beat
        // it in combat - separate from (and on top of) whatever bilateral effect already exists for
        // that pair (e.g. a pair already at War stays at War; this just nudges points further).
        private const int CombatLossRippleMagnitude = 10;

        // A ripple to a third-party civ is this fraction of the original bilateral delta - allies
        // and rivals feel an echo of what happened, not the full impact.
        private const float RippleFactor = 0.4f;

        // Ripples are first-order only: applying a ripple can itself push some other pair across a
        // status-tier boundary (e.g. into War, which fires its own ripple call - see
        // DiplomacyController.ChangedDiplomacyStatus), so this guards against that cascading into a
        // second, third, ... wave of ripples from the same originating event.
        private bool _ripplingInProgress;

        /// <summary>
        /// Propagates a bilateral diplomacy change between `actor` and `target` out to third-party
        /// civs that have an opinion about `target`: civs Allied (or in an active Cooperation Pact)
        /// with `target` shift their own relationship with `actor` the same direction; civs Hostile-
        /// or-worse with `target` shift the opposite direction. Only reaches third parties `actor`
        /// already has a contact record with (ReturnADiplomacyController != null) - a civ can't form
        /// an opinion about someone it's never met.
        /// </summary>
        public void ApplyDiplomaticRipple(CivEnum actor, CivEnum target, int bilateralDelta, DiplomaticEventEnum eventType)
        {
            if (bilateralDelta == 0) return;
            if (actor == CivEnum.BORG || target == CivEnum.BORG) return; // the Borg do not engage in diplomacy
            if (_ripplingInProgress) return;
            if (DiplomacyControllers == null || DiplomacyControllers.Count == 0) return;

            int rippleMagnitude = Mathf.RoundToInt(Mathf.Abs(bilateralDelta) * RippleFactor);
            if (rippleMagnitude == 0) return;
            int sign = bilateralDelta > 0 ? 1 : -1;

            _ripplingInProgress = true;
            try
            {
                // Snapshot first - AddDiplomaticPoints below can trigger annexation
                // (DiplomacyController.AnnexMinorIntoMajor), which mutates DiplomacyControllers.
                var snapshot = new List<DiplomacyController>(DiplomacyControllers);
                foreach (var pair in snapshot)
                {
                    if (pair?.DiplomacyData == null) continue;
                    CivEnum sideA = pair.DiplomacyData.CivEnumSideOne;
                    CivEnum sideB = pair.DiplomacyData.CivEnumSideTwo;
                    if (sideA != target && sideB != target) continue;

                    CivEnum thirdParty = sideA == target ? sideB : sideA;
                    if (thirdParty == actor || thirdParty == target || thirdParty == CivEnum.BORG) continue;

                    DiplomacyStatusEnum thirdToTarget = pair.DiplomacyData.DiplomacyStatusEnumOfCivs;
                    int signedRipple;
                    if (thirdToTarget >= DiplomacyStatusEnum.Allied || pair.DiplomacyData.CooperationPactActive)
                        signedRipple = sign * rippleMagnitude; // an ally of the target feels what the target feels
                    else if (thirdToTarget <= DiplomacyStatusEnum.Hostile)
                        signedRipple = -sign * rippleMagnitude; // a rival of the target feels the opposite
                    else
                        continue; // neutral-ish third civs don't have a strong enough opinion to react

                    DiplomacyController actorThirdPair = ReturnADiplomacyController(actor, thirdParty);
                    if (actorThirdPair == null) continue; // no contact record - can't have an opinion

                    actorThirdPair.AddDiplomaticPoints(signedRipple);
                    GameEvents.DiplomaticRipple(actor, thirdParty, target, signedRipple, eventType);
                }
            }
            finally
            {
                _ripplingInProgress = false;
            }
        }

        /// <summary>
        /// Every resolved combat nudges the loser's standing with the winner down a little further
        /// (on top of whatever status they were already at) and ripples that out to both sides'
        /// allies/rivals. Combat always implies an existing contact record between winner and loser,
        /// but the null-check is kept for safety.
        /// </summary>
        private void HandleCombatEnded(CivEnum winner, CivEnum loser)
        {
            DiplomacyController pair = ReturnADiplomacyController(winner, loser);
            if (pair == null) return;

            pair.SubtractDiplomaticPoints(CombatLossRippleMagnitude);
            ApplyDiplomaticRipple(winner, loser, -CombatLossRippleMagnitude, DiplomaticEventEnum.Combat);
        }

        /// <summary>
        /// Human-player counterpart to ProcessAIDiplomacyForAllCivs: for every pair with
        /// AutoImproveRelations opted in (see DiplomacyData) and no proposal already pending,
        /// automatically sends the same tiered goodwill gesture AI civs pick for themselves. Unlike
        /// the AI tick this has no per-turn chance roll - it's an explicit player opt-in, so it fires
        /// deterministically every turn a slot is free rather than "sometimes", which would read as
        /// broken to a player who just turned it on. CivEnumSideOne must be the human's own civ here
        /// (true whenever this was toggled from that civ's own open Diplomacy card - see
        /// OpenDiplomacyUI's CivEnumSideOne-normalizes-to-local-player behavior).
        /// </summary>
        private void ProcessAutoImproveRelations()
        {
            foreach (var diploCon in DiplomacyControllers)
            {
                if (diploCon?.DiplomacyData == null || !diploCon.DiplomacyData.AutoImproveRelations) continue;

                CivEnum proposerEnum = diploCon.DiplomacyData.CivEnumSideOne;
                if (proposerEnum == CivEnum.BORG) continue;
                if (diploCon.DiplomacyData.ActiveProjects.Exists(p => !p.IsComplete)) continue;

                DiplomacyStatusEnum status = diploCon.DiplomacyData.DiplomacyStatusEnumOfCivs;
                if (status >= DiplomacyStatusEnum.Friendly)
                    diploCon.OfferAlliance(diploCon);
                else if (status >= DiplomacyStatusEnum.Neutral)
                    diploCon.SendAid(diploCon);
                else
                    diploCon.ProposeTrade(diploCon);
            }
        }

        // ─── Project creation ────────────────────────────────────────────────

        /// <summary>
        /// Starts a multi-turn diplomatic proposal from proposerCiv to targetCiv. Returns false if
        /// the proposal cannot start (no contact record, duplicate proposal of the same type already
        /// pending, Borg involved, or - for Alliance - relations aren't Friendly yet). Mirrors
        /// IntelligenceManager.CreateIntelProject's shape.
        /// </summary>
        public bool CreateDiplomacyProject(NegotiationPloysEnum proposalType, CivEnum proposerCiv, CivEnum targetCiv,
            out string failReason)
        {
            failReason = string.Empty;

            if (proposerCiv == CivEnum.BORG || targetCiv == CivEnum.BORG)
            {
                failReason = "the Borg do not engage in diplomacy";
                return false;
            }

            DiplomacyController diploCon = ReturnADiplomacyController(proposerCiv, targetCiv);
            if (diploCon == null)
            {
                failReason = "no contact record with that civilization";
                GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                    $"DiplomacyProject: no contact record between {proposerCiv} and {targetCiv}");
                return false;
            }

            if (proposalType == NegotiationPloysEnum.OfferAlliance &&
                diploCon.DiplomacyData.DiplomacyStatusEnumOfCivs < DiplomacyStatusEnum.Friendly)
            {
                failReason = "relations aren't Friendly yet";
                return false;
            }

            // Only one proposal may be pending between a given civ pair at a time, regardless of
            // type - keeps the relationship from being flooded with simultaneous Trade/Tech/Aid/
            // Alliance offers all racing to resolve at once.
            foreach (var existing in diploCon.DiplomacyData.ActiveProjects)
            {
                if (!existing.IsComplete)
                {
                    failReason = "a proposal is already pending with this civilization";
                    return false;
                }
            }

            int turns = TurnsForProposal(proposalType);
            float chance = Mathf.Clamp01(CalculateAcceptanceChance(proposalType, diploCon.DiplomacyData, targetCiv));

            var project = new DiplomacyProject(proposalType, proposerCiv, targetCiv, turns, chance);
            diploCon.DiplomacyData.ActiveProjects.Add(project);

            GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                $"DiplomacyProject started: {proposalType} {proposerCiv} → {targetCiv} | {turns} turns | Acceptance {chance:P0}");
            return true;
        }

        // ─── Per-turn tick ───────────────────────────────────────────────────

        private void TickDiplomacyProjects()
        {
            foreach (var diploCon in DiplomacyControllers)
            {
                if (diploCon?.DiplomacyData?.ActiveProjects == null) continue;

                for (int i = diploCon.DiplomacyData.ActiveProjects.Count - 1; i >= 0; i--)
                {
                    var project = diploCon.DiplomacyData.ActiveProjects[i];
                    if (project.IsComplete) continue;

                    project.TurnsRemaining--;

                    if (project.TurnsRemaining <= 0)
                    {
                        ResolveDiplomacyProject(diploCon, project);
                        project.IsComplete = true;
                        diploCon.DiplomacyData.ActiveProjects.RemoveAt(i);
                    }
                }
            }
        }

        // ─── Resolution ──────────────────────────────────────────────────────

        private void ResolveDiplomacyProject(DiplomacyController diploCon, DiplomacyProject project)
        {
            bool accepted = UnityEngine.Random.value <= project.AcceptanceChance;

            if (accepted)
            {
                diploCon.ApplyAcceptedProposal(project.ProposalType);
                diploCon.DiplomacyData.LastProposalOutcome = $"✅ {project.ProposalType} ACCEPTED by {project.TargetCiv}";
                GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                    $"DiplomacyProject accepted: {project.ProposalType} {project.ProposerCiv} → {project.TargetCiv}");
            }
            else
            {
                diploCon.DiplomacyData.LastProposalOutcome = $"❌ {project.ProposalType} REJECTED by {project.TargetCiv}";
                GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                    $"DiplomacyProject rejected: {project.ProposalType} {project.ProposerCiv} → {project.TargetCiv}");
            }

            OnDiplomacyProjectResolved?.Invoke(project.ProposalType, project.ProposerCiv, project.TargetCiv, accepted);
        }

        // ─── Calculation helpers ─────────────────────────────────────────────

        private static int TurnsForProposal(NegotiationPloysEnum proposalType)
        {
            switch (proposalType)
            {
                case NegotiationPloysEnum.OfferAlliance:         return 3;
                case NegotiationPloysEnum.OfferTech:             return 2;
                case NegotiationPloysEnum.OfferAid:              return 1;
                case NegotiationPloysEnum.OfferTrade:            return 1;
                case NegotiationPloysEnum.OfferCulturalExchange: return 1;
                default:                                         return 1;
            }
        }

        private static float BaseAcceptanceChance(NegotiationPloysEnum proposalType)
        {
            switch (proposalType)
            {
                case NegotiationPloysEnum.OfferAlliance:         return 0.5f;
                case NegotiationPloysEnum.OfferTech:             return 0.75f;
                case NegotiationPloysEnum.OfferAid:              return 0.85f;
                case NegotiationPloysEnum.OfferTrade:            return 0.9f;
                case NegotiationPloysEnum.OfferCulturalExchange: return 0.9f;
                default:                                         return 0.6f;
            }
        }

        /// <summary>
        /// Base chance for the proposal type, adjusted by how open the responding (target) civ
        /// tends to be (its own DiplomaticAptitude - a warlike/xenophobic/ruthless/greedy civ is
        /// harder to win over) and by how far relations already sit above the gating threshold used
        /// for that proposal type - closer to Allied makes acceptance more likely.
        /// </summary>
        private static float CalculateAcceptanceChance(NegotiationPloysEnum proposalType, DiplomacyData data, CivEnum targetCiv)
        {
            float baseChance = BaseAcceptanceChance(proposalType);

            CivData targetData = CivManager.Instance?.GetCivDataByCivEnum(targetCiv);
            float targetOpenness = targetData != null ? targetData.DiplomaticAptitude * 0.15f : 0f; // roughly ±0.3

            float relationBonus = Mathf.Clamp01(
                (data.DiplomacyPointsOfCivs - (int)DiplomacyStatusEnum.Friendly) /
                (float)((int)DiplomacyStatusEnum.Allied - (int)DiplomacyStatusEnum.Friendly)) * 0.3f;

            return Mathf.Clamp01(baseChance + targetOpenness + relationBonus);
        }

        /// <summary>
        /// Per-turn trust drift for every minor-major pair with an active cooperation pact (see
        /// DiplomacyController.OfferAlliance / TickCooperationPactDrift). Runs independently of the
        /// AI decision tick above so a human-proposed pact with an AI minor race still grows even
        /// though the human doesn't go through ProcessAIDiplomacyForAllCivs.
        /// </summary>
        private void ProcessCooperationPactDrift()
        {
            foreach (var diploCon in DiplomacyControllers)
            {
                if (diploCon?.DiplomacyData == null || !diploCon.DiplomacyData.CooperationPactActive) continue;

                bool sideOneMinor = (int)diploCon.DiplomacyData.CivEnumSideOne > 6;
                CivEnum minorEnum = sideOneMinor ? diploCon.DiplomacyData.CivEnumSideOne : diploCon.DiplomacyData.CivEnumSideTwo;
                CivEnum majorEnum = sideOneMinor ? diploCon.DiplomacyData.CivEnumSideTwo : diploCon.DiplomacyData.CivEnumSideOne;

                CivController major = CivManager.Instance.GetCivControllerByCivEnum(majorEnum);
                CivController minor = CivManager.Instance.GetCivControllerByCivEnum(minorEnum);
                if (major?.CivData == null || minor?.CivData == null) continue;

                diploCon.TickCooperationPactDrift(major.CivData, minor.CivData);
            }
        }

        /// <summary>
        /// Minimal per-turn AI diplomacy tick: for every active pair where the AI seat is Side One
        /// (a human player acts through the UI instead), roll a chance to attempt one goodwill
        /// gesture this turn. Chance and target status are both scaled by DiplomaticAptitude, so
        /// Federation-like civs court relationships far more readily than Romulan/Cardassian/
        /// Dominion-like ones. ApplyDiplomaticGesture already rejects any pair involving the Borg.
        /// </summary>
        private void ProcessAIDiplomacyForAllCivs()
        {
            foreach (var diploCon in DiplomacyControllers)
            {
                if (diploCon?.DiplomacyData == null) continue;

                CivEnum aiCivEnum = diploCon.DiplomacyData.CivEnumSideOne;
                if (aiCivEnum == CivEnum.BORG) continue;
                if (GameController.Instance != null && GameController.Instance.AreWeLocalPlayer(aiCivEnum)) continue;

                CivController proposer = CivManager.Instance.GetCivControllerByCivEnum(aiCivEnum);
                if (proposer?.CivData == null || !proposer.CivData.PlayedByAI) continue;

                // Cooperative civs (DiplomaticAptitude near +2) act nearly every turn; hostile ones
                // (near -2) rarely bother. Range: ~10%-50% chance per turn.
                float actionChance = 0.3f + proposer.CivData.DiplomaticAptitude * 0.1f;
                if (UnityEngine.Random.value > actionChance) continue;

                DiplomacyStatusEnum status = diploCon.DiplomacyData.DiplomacyStatusEnumOfCivs;
                if (status >= DiplomacyStatusEnum.Friendly && proposer.CivData.DiplomaticAptitude > 0f)
                    diploCon.OfferAlliance(diploCon);
                else if (status >= DiplomacyStatusEnum.Neutral)
                    diploCon.SendAid(diploCon);
                else
                    diploCon.ProposeTrade(diploCon);
            }
        }

        public DiplomacyController InstantiateDiplomacyController(CivController civSideOne, FleetController fleetSideOne,
        CivController civSideTwo, FleetController fleetSideTwo, StarSysController sysCon)
        {
            List<ShipController> shipsToSeeInLocalPayerDiploUI = new List<ShipController>();
            DiplomacyData diplomacyData = new DiplomacyData(civSideOne.CivData.CivEnum, civSideTwo.CivData.CivEnum);
            DiplomacyController diplomacyCon = Instantiate(diplomacControllerPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity).GetComponent<DiplomacyController>();
            diplomacyCon.enabled = true;
            diplomacyCon.DiplomacyData = diplomacyData;

            if (!DiplomacyControllers.Contains(diplomacyCon))
                DiplomacyControllers.Add(diplomacyCon);
            diplomacyCon.gameObject.SetActive(true);
            diplomacyCon.gameObject.layer = 5;
            diplomacyCon.transform.SetParent(this.transform);
            if (civSideOne.CivData.CivEnum <= CivEnum.TERRAN || civSideTwo.CivData.CivEnum <= CivEnum.TERRAN) // diplomacy only when there is one or more major civ
            {
                // one or two is a major civ so minors do not have diplomacy with other minors
                diplomacyData.CivEnumSideOne = civSideOne.CivData.CivEnum;
                diplomacyData.CivOne = civSideOne;
                diplomacyData.CivEnumSideTwo = civSideTwo.CivData.CivEnum;
                diplomacyData.CivTwo = civSideTwo;
                if (CivManager.Instance.LocalPlayerCivController == civSideOne)
                {
                    if (fleetSideTwo.FleetData != null)
                    {
                        fleetSideTwo.FleetData.ShipsList.RemoveAll(item => item == null);
                        shipsToSeeInLocalPayerDiploUI = fleetSideTwo.FleetData.ShipsList;
                    }
                    else if (sysCon.StarSysData != null)
                    {
                        sysCon.StarSysData.ShipsList.RemoveAll(item => item == null);
                        shipsToSeeInLocalPayerDiploUI = sysCon.StarSysData.ShipsList;
                    }
                }
                else
                {
                    if (fleetSideOne.FleetData != null)
                    {
                        fleetSideOne.FleetData.ShipsList.RemoveAll(item => item == null);
                        shipsToSeeInLocalPayerDiploUI = fleetSideOne.FleetData.ShipsList;
                    }
                    else
                    {
                        sysCon.StarSysData.ShipsList.RemoveAll(item => item == null);
                        shipsToSeeInLocalPayerDiploUI = sysCon.StarSysData.ShipsList;
                    }
                }
                // Each side is either a real, network-spawned fleet or the disposable
                // placeholder from FleetManager.InsatiateEmptyFleetController (FleetData never
                // assigned, and it's Destroy()'d by the caller right after this method returns -
                // see ResolveEncounterOtherCivSystem). Whichever side is the placeholder actually
                // represents the star system's own docked defenders, so route it to
                // StarSysController instead of leaving a soon-to-be-destroyed reference behind.
                // Previously FleetControllerCivOne was assigned unconditionally and only side two
                // had this real/placeholder check, so any first-contact encounter where the local
                // player's civ sorted after the defender's (e.g. TERRAN or a minor race meeting the
                // Borg) left FleetControllerCivOne pointing at a destroyed placeholder and
                // StarSysController null, and DiplomacyController.Combat()'s ValidCombatCheck
                // silently failed forever, making the Combat button do nothing.
                if (fleetSideOne != null && fleetSideOne.FleetData != null)
                    diplomacyData.FleetControllerCivOne = fleetSideOne;
                else if (sysCon != null && sysCon.StarSysData != null)
                    diplomacyData.StarSysController = sysCon;

                if (fleetSideTwo != null && fleetSideTwo.FleetData != null)
                    diplomacyData.FleetContollerCivTwo = fleetSideTwo;
                else if (sysCon != null && sysCon.StarSysData != null)
                    diplomacyData.StarSysController = sysCon;
                diplomacyData.firstContact = true;
                diplomacyData.EncounterType = EncounterType.FirstContact;

                // Contact reveal would otherwise wait for the next OnTurnAdvanced pass through
                // StarSysAIManager.ProcessAllSystems, leaving the sprite/insignia hidden for up to
                // a full turn after the player actually made contact with the Borg system.
                if (sysCon != null && sysCon.StarSysData != null &&
                    sysCon.StarSysData.SystemType == GalaxyObjectType.UniComplex &&
                    StarSysManager.Instance != null)
                    StarSysManager.Instance.RefreshBorgConcealment(sysCon);
            }
            else if (sysCon.StarSysData.SystemType >= GalaxyObjectType.BlackHole) // resolve a non conventional encounter
            {
                diplomacyCon.ResolveFleetToStrangGalacticEncounter(diplomacyCon); // ToDo
            }
            diplomacyCon.DiplomacyData.DiplomacyStatusEnumOfCivs = CalculateDiplomaticStatusOnFirstContact(diplomacyCon);
            // A first encounter alone can never open relations worse than ColdWar - War only ever
            // comes from an explicit DeclareWar or from combat losses grinding relations down over
            // multiple turns, never as a same-frame consequence of simply meeting for the first
            // time. CalculateDiplomaticStatusOnFirstContact's switch never actually reaches War
            // today, but this makes that guarantee explicit rather than an accident of its case
            // boundaries.
            if (diplomacyCon.DiplomacyData.DiplomacyStatusEnumOfCivs < DiplomacyStatusEnum.ColdWar)
                diplomacyCon.DiplomacyData.DiplomacyStatusEnumOfCivs = DiplomacyStatusEnum.ColdWar;
            // Combat/Withdraw are only meaningful below Neutral relations (see
            // DiplomacyMenuUIController.IsEncounterGenuinelyPendingForLocalPlayer) - the trait-based
            // calculation above can occasionally land first contact at Neutral or better, in which
            // case there's no Fight-or-flee decision to make even on this very first meeting.
            diplomacyCon.DiplomacyData.EncounterResolved =
                diplomacyCon.DiplomacyData.DiplomacyStatusEnumOfCivs >= DiplomacyStatusEnum.Neutral;

            // At Neutral+ relations there's no Fight/Withdraw decision pending, and DoAIDiplomacy
            // below only ever force-resolves the AI's own side (never the local human's side), so
            // the mutual-Withdraw TryResolveEncounter path that normally releases a fleet never
            // fires here - release any real (non-placeholder) fleet immediately instead of leaving
            // it frozen forever awaiting a response nobody will give.
            if (diplomacyCon.DiplomacyData.EncounterResolved)
            {
                if (fleetSideOne != null && fleetSideOne.FleetData != null)
                    fleetSideOne.ServerDecrementPendingEncounters();
                if (fleetSideTwo != null && fleetSideTwo.FleetData != null)
                    fleetSideTwo.ServerDecrementPendingEncounters();
            }
            diplomacyCon.DiplomacyData.DiplomacyPointsOfCivs = (int)diplomacyCon.DiplomacyData.DiplomacyStatusEnumOfCivs;
            diplomacyCon.DiplomacyData.EncounterStartRealTime = Time.realtimeSinceStartup;
            // UI open is deferred to InterTurn via TurnEventQueue; the caller enqueues
            // ShowDiplomacyUIForController after this method returns.
            diplomacyCon.UIShipsForQueue = shipsToSeeInLocalPayerDiploUI;

            // First contact previously never got an AI auto-response - CheckForAIDiplomacy (the
            // only other caller of DoAIDiplomacy) is exclusively wired into the repeat-encounter
            // branches below. Without this, an AI-controlled side meeting a fleet for the first
            // time (e.g. an AI-owned system's defenders) never picked Fight/Withdraw, leaving the
            // fleet frozen forever waiting on a response nothing would ever produce.
            diplomacyCon.DoAIDiplomacy();

            return diplomacyCon;
        }
        // Called by TurnEventQueue.ShowAction closures at InterTurn to open the diplomacy panel
        // for a controller that was set up (and AI-resolved) at encounter time.
        private void ShowDiplomacyUIForController(DiplomacyController ctrl, List<ShipController> ships)
        {
            InstantiateDiplomacyUIGameObject(ctrl);
            GalaxyMenuUIController.Instance.HideNoContactUI();
            GalaxyMenuUIController.Instance.OpenMenu(Menu.ADiplomacyMenu, ctrl.gameObject);
            DiplomacyMenuUIController.Instance.SetUpDiplomacyUIElements(ctrl.DiplomacyUIGameObject, ctrl.gameObject, ships);
        }

        private void InstantiateDiplomacyUIGameObject(DiplomacyController diplomacyCon)
        {
            if (GameController.Instance.AreWeLocalPlayer(diplomacyCon.DiplomacyData.CivEnumSideOne) ||
                GameController.Instance.AreWeLocalPlayer(diplomacyCon.DiplomacyData.CivEnumSideTwo))
            {
                if (diplomacyCon.DiplomacyUIGameObject == null)
                {
                    GameObject uiGO = (GameObject)Instantiate(diplomacyUIPrefab, new Vector3(0, 0, 0),
                        Quaternion.identity);
                    uiGO.SetActive(true);
                    uiGO.layer = 5;
                    diplomacyCon.DiplomacyUIGameObject = uiGO;
                    diplomacyCon.DiplomacyUIGameObject.SetActive(true);
                    DiplomacyMenuUIController.Instance.ADiplomacyMenuView.SetActive(true);
                    uiGO.transform.SetParent(DiplomacyMenuUIController.Instance.ADiplomacyMenuView.transform, false);
                    if (DiplomacyMenuUIController.Instance != null && !DiplomacyMenuUIController.Instance.ListOfDiplomacyUiGos.Contains(uiGO))
                    {
                        DiplomacyMenuUIController.Instance.ListOfDiplomacyUiGos.Add(uiGO);
                    }
                }
            }
        }
        public bool FoundADiplomacyController(CivController civPartyOne, CivController civPartyTwo) //, GameObject hitGO)
        {
            bool found = false;
            //List<DiplomacyController> placeholderControllers = new List<DiplomacyController>();
            for (int i = 0; i < DiplomacyControllers.Count; i++)
            {
                if (DiplomacyControllers[i] != null)
                {
                    if (DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == civPartyOne.CivData.CivEnum && DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == civPartyTwo.CivData.CivEnum
                        || DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == civPartyOne.CivData.CivEnum && DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == civPartyTwo.CivData.CivEnum)
                    {
                        found = true;
                        break;
                    }
                }
            }
            return found;
        }
        public DiplomacyController ReturnADiplomacyController(CivEnum oneSide, CivEnum otherSide)
        {
            DiplomacyController diplomacyController = null;
            for (int i = 0; i < DiplomacyControllers.Count; i++)
            {
                if (DiplomacyControllers[i] != null && ((DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == oneSide && DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == otherSide)
                    || (DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == otherSide && DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == oneSide)))
                {
                    diplomacyController = DiplomacyControllers[i];
                    break;
                }
            }
            return diplomacyController;
        }
        public void OpenDiplomacyUI(CivController civPartyOne, CivController civPartyTwo, List<ShipController> shipList, FleetController fleetOne = null, FleetController fleetTwo = null, StarSysController sysCon = null)
        {
            DiplomacyController ourDiplomacyController = ReturnADiplomacyController(civPartyOne, civPartyTwo);
            if (ourDiplomacyController != null)
            {
                // ✅ Update participants in data to ensure Combat button works with current encounter
                // We assign them based on the party order passed in
                ourDiplomacyController.DiplomacyData.FleetControllerCivOne = fleetOne;
                ourDiplomacyController.DiplomacyData.FleetContollerCivTwo = fleetTwo;
                ourDiplomacyController.DiplomacyData.StarSysController = sysCon;

                // Fresh encounter decision - clear any Fight/Withdraw response left over from a
                // previous encounter between this civ pair (per-encounter, not a standing order).
                ourDiplomacyController.DiplomacyData.ResponseSideOne = DiplomacyData.EncounterResponse.Undecided;
                ourDiplomacyController.DiplomacyData.ResponseSideTwo = DiplomacyData.EncounterResponse.Undecided;

                // Combat/Withdraw are only meaningful while relations with this civ are below
                // Neutral (see DiplomacyMenuUIController.IsEncounterGenuinelyPendingForLocalPlayer).
                // At Neutral or better, reopening this card (fleet/system contact or manual browsing)
                // is never a Fight-or-flee decision, so mark it already resolved instead of pending -
                // the player can still escalate via the Declare War button, which re-arms this itself.
                ourDiplomacyController.DiplomacyData.EncounterResolved =
                    ourDiplomacyController.DiplomacyData.DiplomacyStatusEnumOfCivs >= DiplomacyStatusEnum.Neutral;
                // Bookkeeping only - no longer drives a timeout (resolution is now instant/action-driven,
                // see DiplomacyController.ServerImplicitlyWithdrawFleet / ServerForceOtherSideIfStillUndecided).
                ourDiplomacyController.DiplomacyData.EncounterStartRealTime = Time.realtimeSinceStartup;

                if (GameController.Instance.AreWeLocalPlayer(civPartyOne.CivData.CivEnum))
                {
                    ourDiplomacyController.DiplomacyData.CivEnumSideOne = civPartyOne.CivData.CivEnum; // local player civ
                    ourDiplomacyController.DiplomacyData.CivEnumSideTwo = civPartyTwo.CivData.CivEnum;
                }
                else if (GameController.Instance.AreWeLocalPlayer(civPartyTwo.CivData.CivEnum))
                {
                    ourDiplomacyController.DiplomacyData.CivEnumSideOne = civPartyTwo.CivData.CivEnum; // local player civ
                    ourDiplomacyController.DiplomacyData.CivEnumSideTwo = civPartyOne.CivData.CivEnum;
                }
                // ✅ Open via GalaxyMenuUIController to ensure other menus close correctly
                GalaxyMenuUIController.Instance.OpenMenu(Menu.ADiplomacyMenu, ourDiplomacyController.gameObject);

                DiplomacyMenuUIController.Instance.SetUpDiplomacyUIElements(ourDiplomacyController.DiplomacyUIGameObject,
                    ourDiplomacyController.gameObject, shipList);
            }
        }
        // Called server-side when a fleet is given new orders while IsAwaitingEncounterResolution is
        // true (see FleetController.ServerImplicitlyWithdrawFromPendingEncounters) - a fleet can be
        // party to more than one concurrent encounter (GalaxyEncounterQueue's simultaneous-convergence
        // batching), so every unresolved DiplomacyController this fleet is a party to gets resolved,
        // not just one.
        public void ServerImplicitlyWithdrawFleetFromPendingEncounters(FleetController fleet)
        {
            var snapshot = new List<DiplomacyController>(DiplomacyControllers);
            foreach (DiplomacyController diploCon in snapshot)
            {
                if (diploCon == null || diploCon.DiplomacyData == null || diploCon.DiplomacyData.EncounterResolved) continue;
                diploCon.ServerImplicitlyWithdrawFleet(fleet);
            }
        }
        public void CheckForAIDiplomacy(FleetController fleetCon1, FleetController fleetCon2)
        {
            CivController civPartyOne;
            CivController civPartyTwo;
            if (fleetCon1.FleetData.CivEnum < fleetCon2.FleetData.CivEnum)
            {
                civPartyOne = fleetCon1.FleetData.CivController;
                civPartyTwo = fleetCon2.FleetData.CivController;
            }
            else
            {
                civPartyOne = fleetCon2.FleetData.CivController;
                civPartyTwo = fleetCon1.FleetData.CivController;
            }
            DiplomacyController ourDiplomacyController = ReturnADiplomacyController(civPartyOne, civPartyTwo);
            if (ourDiplomacyController != null)
            {
                if (civPartyOne.CivData.PlayedByAI)
                    ourDiplomacyController.DoAIDiplomacy();
                else if (civPartyTwo.CivData.PlayedByAI)
                {
                    ourDiplomacyController.DoAIDiplomacy();
                }
            }

        }
        public void CheckForAIDiplomacy(FleetController fleetCon, StarSysController sysCon)
        {
            CivController civPartyOne;
            CivController civPartyTwo;
            if (fleetCon.FleetData.CivEnum < sysCon.StarSysData.CurrentOwnerCivEnum)
            {
                civPartyOne = fleetCon.FleetData.CivController;
                civPartyTwo = sysCon.StarSysData.CurrentCivController;
            }
            else
            {
                civPartyOne = sysCon.StarSysData.CurrentCivController;
                civPartyTwo = fleetCon.FleetData.CivController;
            }
            DiplomacyController ourDiplomacyController = ReturnADiplomacyController(civPartyOne, civPartyTwo);
            if (ourDiplomacyController != null)
            {
                if (civPartyOne.CivData.PlayedByAI)
                    ourDiplomacyController.DoAIDiplomacy();
                else if (civPartyTwo.CivData.PlayedByAI)
                {
                    ourDiplomacyController.DoAIDiplomacy();
                }
            }
        }
public DiplomacyController ReturnADiplomacyController(CivController civPartyOne, CivController civPartyTwo)
        {
            DiplomacyController diplomacyController = null;
            for (int i = 0; i < DiplomacyControllers.Count; i++)
            {
                if (DiplomacyControllers[i] != null && ((DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == civPartyOne.CivData.CivEnum &&
                    DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == civPartyTwo.CivData.CivEnum)
                    || (DiplomacyControllers[i].DiplomacyData.CivEnumSideOne == civPartyTwo.CivData.CivEnum && DiplomacyControllers[i].DiplomacyData.CivEnumSideTwo == civPartyOne.CivData.CivEnum)))
                {
                    diplomacyController = DiplomacyControllers[i];
                    break;
                }
            }
            return diplomacyController;
        }
        public DiplomacyStatusEnum CalculateDiplomaticStatusOnFirstContact(DiplomacyController ourDiploCon)
        {
            CivController civOne = CivManager.Instance.GetCivControllerByCivEnum(ourDiploCon.DiplomacyData.CivEnumSideOne);
            CivController civTwo = CivManager.Instance.GetCivControllerByCivEnum(ourDiploCon.DiplomacyData.CivEnumSideTwo);
            DiplomacyStatusEnum diplomacyStatus = DiplomacyStatusEnum.Neutral;
            int warLike = Math.Abs((int)civOne.CivData.Warlike - (int)civTwo.CivData.Warlike);
            int xenophobia = Math.Abs((int)civOne.CivData.Xenophobia - (int)civTwo.CivData.Xenophobia);
            int ruthless = Math.Abs((int)civOne.CivData.Ruthless - (int)civTwo.CivData.Ruthless);
            int greedy = Math.Abs((int)civOne.CivData.Greedy - (int)civTwo.CivData.Greedy);
            int degreesOfSparation = warLike + xenophobia + ruthless + greedy;
            switch (degreesOfSparation)
            {
                case 0:
                    diplomacyStatus = DiplomacyStatusEnum.Friendly;
                    break;
                case 1:
                case 2:
                case 3:
                case 4:
                    diplomacyStatus = DiplomacyStatusEnum.Neutral;
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                    diplomacyStatus = DiplomacyStatusEnum.UnFriendly;
                    break;
                case 9:
                case 10:
                case 11:
                case 12:
                    diplomacyStatus = DiplomacyStatusEnum.Hostile;
                    break;
                case 13:
                case 14:
                case 15:
                case 16:
                    diplomacyStatus = DiplomacyStatusEnum.ColdWar;
                    break;
                default:
                    diplomacyStatus = DiplomacyStatusEnum.Neutral;
                    break;
            }
            return diplomacyStatus;
        }

        public void FleetControllerVsOtherCivFleet(FleetController reportingPlayerFleet, FleetController otherFleet)
        { // already not one of our fleets
            reportingPlayerFleet.FleetData.ShipsList.RemoveAll(item => item == null);
            otherFleet.FleetData.ShipsList.RemoveAll(item => item == null);
            // FleetData.CivController can still be null here for a just-reconstructed remote client
            // fleet (see FleetController.IsReadyToHandleCivChange, which now gates against this race,
            // but this Rpc handler runs on every client and a crash here gets that client disconnected
            // by Mirror - don't trust the caller-side fix alone).
            if (reportingPlayerFleet?.FleetData?.CivController == null || otherFleet?.FleetData?.CivController == null)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Diplomacy,
                    $"FleetControllerVsOtherCivFleet: CivController not yet resolved for '{reportingPlayerFleet?.name}' or '{otherFleet?.name}' - skipping this encounter, it will re-fire on next contact.");
                return;
            }
            StarSysController sysConEmpty = StarSysManager.Instance.InstantiateEmptyStarSysController();
            if (reportingPlayerFleet != null)
            {
                CivController civSideOne;
                CivController civSideTwo;
                FleetController sideOneFleetCon;
                FleetController sideTwoFleetCon;
                if (reportingPlayerFleet.FleetData.CivController.CivData.CivEnum < otherFleet.FleetData.CivController.CivData.CivEnum)
                {
                    civSideOne = reportingPlayerFleet.FleetData.CivController;
                    sideOneFleetCon = reportingPlayerFleet;
                    civSideTwo = otherFleet.FleetData.CivController;
                    sideTwoFleetCon = otherFleet;
                }
                else
                {
                    civSideOne = otherFleet.FleetData.CivController;
                    sideOneFleetCon = otherFleet;
                    civSideTwo = reportingPlayerFleet.FleetData.CivController;
                    sideTwoFleetCon = reportingPlayerFleet;
                }
                if (!DiplomacyManager.Instance.FoundADiplomacyController(civSideOne, civSideTwo))
                {
                    DiplomacyController newDiplomacyCon = DiplomacyManager.Instance.InstantiateDiplomacyController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, sysConEmpty);
                    if (!DiplomacyControllers.Contains(newDiplomacyCon))
                        DiplomacyControllers.Add(newDiplomacyCon);
                    IntelligenceManager.Instance.InitializeNewIntelligenceController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, sysConEmpty);
                    Destroy(sysConEmpty.gameObject);
                    var dip = newDiplomacyCon;
                    TurnEventQueue.Instance?.Enqueue(new TurnEvent
                    {
                        Type = TurnEventType.DiplomacyEncounter,
                        Fleet = reportingPlayerFleet,
                        ShowAction = () => ShowDiplomacyUIForController(dip, dip.UIShipsForQueue)
                    });
                }
                else
                {
                    // Repeat encounter: if relations are Neutral or better, let both fleets pass
                    // without opening the UI. Below Neutral (UnFriendly/Hostile/ColdWar/War), or if
                    // no diplomacy controller was found for some reason, fall through to the normal
                    // paused decision path.
                    DiplomacyController ourDiplomacyController = ReturnADiplomacyController(civSideOne, civSideTwo);
                    bool atPeace = ourDiplomacyController != null &&
                        ourDiplomacyController.DiplomacyData.DiplomacyStatusEnumOfCivs >= DiplomacyStatusEnum.Neutral;

                    DiplomacyManager.Instance.CheckForAIDiplomacy(sideOneFleetCon, sideTwoFleetCon);
                    UpdateDiplomacyEncoutnerType(sideOneFleetCon, sideTwoFleetCon);
                    var c1 = civSideOne; var c2 = civSideTwo;
                    var sl = otherFleet.FleetData.ShipsList;
                    var f1 = sideOneFleetCon; var f2 = sideTwoFleetCon;
                    TurnEventQueue.Instance?.Enqueue(new TurnEvent
                    {
                        Type = TurnEventType.DiplomacyEncounter,
                        Fleet = reportingPlayerFleet,
                        ShowAction = () => OpenDiplomacyUI(c1, c2, sl, f1, f2, null)
                    });

                    // At Neutral+ relations there's no Fight/Withdraw decision pending - release
                    // both fleets immediately instead of leaving them frozen awaiting a response
                    // nobody needs to give.
                    if (atPeace)
                    {
                        sideOneFleetCon.ServerDecrementPendingEncounters();
                        sideTwoFleetCon.ServerDecrementPendingEncounters();
                    }
                    Destroy(sysConEmpty.gameObject);
                }
            }
        }

        private void ExposeCivToLocalPlayer(CivEnum civToExpose)
        {
            Debug.Log($"DiplomacyManager: Exposing {civToExpose} to Local Player.");
            StarSysManager.Instance.ExposeAllSystemName(civToExpose);
            FleetManager.Instance.ExposeAllFleetInsigniaSprites(civToExpose);
        }

        private DiplomacyData EntereDiplomacyData(FleetController fleetConA, StarSysController starSysCon)
        {
            DiplomacyData diplomacyData = new DiplomacyData();
            diplomacyData.FleetControllerCivOne = fleetConA;
            diplomacyData.CivOne = fleetConA.FleetData.CivController;
            diplomacyData.CivEnumSideOne = fleetConA.FleetData.CivEnum;
            diplomacyData.StarSysController = starSysCon;
            diplomacyData.CivTwo = starSysCon.StarSysData.CurrentCivController;
            diplomacyData.CivEnumSideTwo = starSysCon.StarSysData.CurrentOwnerCivEnum;
            diplomacyData.FleetContollerCivTwo = null;
            return diplomacyData;
        }
        private void UpdateDiplomacyEncoutnerType(FleetController fleetA, FleetController fleetB)
        { // *** Will we need this?
            var diplomacyCon = ReturnADiplomacyController(fleetA.FleetData.CivEnum, fleetB.FleetData.CivEnum); // not mono behavior
            if (diplomacyCon != null)
            {
                diplomacyCon.DiplomacyData.EncounterType = EncounterType.Diplomacy;
                // ✅ Update participants so Combat button works with these fleets
                diplomacyCon.DiplomacyData.FleetControllerCivOne = fleetA;
                diplomacyCon.DiplomacyData.FleetContollerCivTwo = fleetB;
                diplomacyCon.DiplomacyData.StarSysController = null;
            }
        }

        internal void ResolveEncounterOtherCivSystem(FleetController reportingPlayerfleet, StarSysController otherCivSysCon)
        {
            Debug.Log("DiplomacyManager: ResolveEncounterOtherCivSystem called.");
            // already not one of our systems
            FleetController fleetConEmpty = FleetManager.Instance.InsatiateEmptyFleetController();
            try
            {
                int firstUninhabited = (int)CivEnum.ZZUNINHABITED1; // all lower than this are inhabited (including Borg UniComplex and inhabitable Nebula)

                if ((int)otherCivSysCon.StarSysData.CurrentOwnerCivEnum < firstUninhabited) // it is inhabited
                {
                    if (reportingPlayerfleet != null) // it is a FleetController and not a StarSystem or other with collider                                                                                                                                                    leetController
                    {
                        CivController civSideOne;
                        CivController civSideTwo;
                        FleetController sideOneFleetCon;
                        FleetController sideTwoFleetCon;
                        if (reportingPlayerfleet.FleetData.CivController.CivData.CivEnum < otherCivSysCon.StarSysData.CurrentCivController.CivData.CivEnum)
                        { // local player is side one
                            civSideOne = reportingPlayerfleet.FleetData.CivController;
                            sideOneFleetCon = reportingPlayerfleet;
                            civSideTwo = otherCivSysCon.StarSysData.CurrentCivController;
                            sideTwoFleetCon = fleetConEmpty; // we do not have the other fleet controller, so we use an empty place holder
                        }
                        else // other civ is side one
                        {
                            civSideOne = otherCivSysCon.StarSysData.CurrentCivController;
                            sideOneFleetCon = fleetConEmpty; // we do not have the other fleet controller, so we use an empty one
                            civSideTwo = reportingPlayerfleet.FleetData.CivController;
                            sideTwoFleetCon = reportingPlayerfleet;
                        }

                        //have we met before? Do I know you?
                        if (!FoundADiplomacyController(civSideOne, civSideTwo))
                        { // First Contact
                            DiplomacyController newDiplomacyCon = InstantiateDiplomacyController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, otherCivSysCon);
                            if (!DiplomacyControllers.Contains(newDiplomacyCon))
                                DiplomacyControllers.Add(newDiplomacyCon);
                            IntelligenceManager.Instance.InitializeNewIntelligenceController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, otherCivSysCon);
                            var dip = newDiplomacyCon;
                            TurnEventQueue.Instance?.Enqueue(new TurnEvent
                            {
                                Type = TurnEventType.DiplomacyEncounter,
                                Fleet = reportingPlayerfleet,
                                ShowAction = () => ShowDiplomacyUIForController(dip, dip.UIShipsForQueue)
                            });
                        }
                        else
                        { // not first contact
                            FeetToSysNotSameCivNotFirstEncounter(sideOneFleetCon, otherCivSysCon);
                            //IntelligenceManager.Instance.UpdateOurIntelController(civSideOne, sideOneFleetCon, civSideTwo, sideTwoFleetCon, otherCivSysCon);
                        }
                    }
                    otherCivSysCon.gameObject.SetActive(true);
                }
                else if ((int)otherCivSysCon.StarSysData.CurrentOwnerCivEnum >= firstUninhabited)
                {
                    //React to Uninhabited system contact and Colonize option
                    FeetsUninhabitedSysEncounter(reportingPlayerfleet, otherCivSysCon);
                    foreach (ShipController shipController in reportingPlayerfleet.FleetData.GetShipList())
                    {
                        if (shipController.ShipData.ShipType == ShipType.Transport)
                        {
                            // ToDo: Colonies Option/ UI?
                        }
                    }
                }
            }
            finally
            {
                // Always runs, even if the block above throws (e.g. a bystander client hitting one of
                // the "remote client civ/player data not ready yet" races documented in
                // FleetController.OnCivEnumChanged) - otherwise this disposable placeholder (see
                // FleetManager.InsatiateEmptyFleetController) never gets destroyed and lingers in the
                // scene forever, inactive but still there.
                if (fleetConEmpty != null)
                    Destroy(fleetConEmpty.gameObject);
            }
        }

        private void FeetsUninhabitedSysEncounter(FleetController reportingPlayerfleet, StarSysController uninhabitedSysCon)
        {
            var diplomacyData = EntereDiplomacyData(reportingPlayerfleet, uninhabitedSysCon); // not mono behavior
            diplomacyData.EncounterType = EncounterType.UninhabitedSystem;

            // Instantiate a DiplomacyController MonoBehaviour from prefab (or add component) so it's Unity-managed
            DiplomacyController diplomacyController = null;
            if (diplomacControllerPrefab != null)
            {
                GameObject dipGo = Instantiate(diplomacControllerPrefab, Vector3.zero, Quaternion.identity);
                dipGo.SetActive(true);
                dipGo.layer = 5;
                dipGo.transform.SetParent(this.transform, false);
                diplomacyController = dipGo.GetComponent<DiplomacyController>();
                if (diplomacyController == null)
                    diplomacyController = dipGo.AddComponent<DiplomacyController>();
            }
            else
            {
                GameObject dipGo = new GameObject("DiplomacyController");
                dipGo.transform.SetParent(this.transform, false);
                diplomacyController = dipGo.AddComponent<DiplomacyController>();
            }

            diplomacyController.DiplomacyData = diplomacyData;
            diplomacyController.ResolveUninhabitedSystem(reportingPlayerfleet.FleetData.CivController, uninhabitedSysCon);

            if (!DiplomacyControllers.Contains(diplomacyController))
                DiplomacyControllers.Add(diplomacyController);
            //DiplomacyControllers.Add(diplomacyController);
        }


        public void FeetToSysNotSameCivNotFirstEncounter(FleetController fleetA, StarSysController sysCon)
        {
            CivController civPartyOne;
            CivController civPartyTwo;
            if (fleetA.FleetData.CivEnum < sysCon.StarSysData.CurrentOwnerCivEnum)
            {
                civPartyOne = fleetA.FleetData.CivController;
                civPartyTwo = sysCon.StarSysData.CurrentCivController;
            }
            else
            {
                civPartyOne = sysCon.StarSysData.CurrentCivController;
                civPartyTwo = fleetA.FleetData.CivController;
            }

            DiplomacyController diplomacyController = ReturnADiplomacyController(civPartyOne, civPartyTwo);
            if (diplomacyController != null)
            {
                bool atPeace = diplomacyController.DiplomacyData.DiplomacyStatusEnumOfCivs >= DiplomacyStatusEnum.Neutral;

                CheckForAIDiplomacy(fleetA, sysCon);
                diplomacyController.DiplomacyData.EncounterType = EncounterType.Diplomacy;
                var cp1 = civPartyOne; var cp2 = civPartyTwo;
                var sl2 = sysCon.StarSysData.ShipsList;
                var fa = (fleetA.FleetData.CivController == civPartyOne ? fleetA : null);
                var fb = (fleetA.FleetData.CivController == civPartyTwo ? fleetA : null);
                var sc = sysCon;
                TurnEventQueue.Instance?.Enqueue(new TurnEvent
                {
                    Type = TurnEventType.DiplomacyEncounter,
                    Fleet = fleetA,
                    ShowAction = () => OpenDiplomacyUI(cp1, cp2, sl2, fa, fb, sc)
                });

                // At Neutral+ relations there's no Fight/Withdraw decision pending - release
                // the fleet immediately instead of leaving it frozen awaiting a response
                // nobody needs to give.
                if (atPeace)
                {
                    fleetA.ServerDecrementPendingEncounters();
                }
            }
        }

        internal void ResolveDiplomacyForClickSystemWeKnow(CivController localPlayerCivContoller, StarSysController starSysController)
        {
            //already not one of our fleets
            CivController civPartyOne;
            CivController civPartyTwo;

            if ((int)localPlayerCivContoller.CivData.CivEnum < (int)starSysController.StarSysData.CurrentCivController.CivData.CivEnum)
            {
                civPartyOne = localPlayerCivContoller;
                civPartyTwo = starSysController.StarSysData.CurrentCivController;
            }
            else // other civ is side one
            {
                civPartyOne = starSysController.StarSysData.CurrentCivController;
                civPartyTwo = localPlayerCivContoller;
            }
            //have we met before?
            if (DiplomacyManager.Instance.FoundADiplomacyController(civPartyOne, civPartyTwo))
            {   // not First Contact, just by clicking on the system
                DiplomacyManager.Instance.OpenDiplomacyUI(civPartyOne, civPartyTwo, starSysController.StarSysData.ShipsList, null, null, starSysController);
            }
else
            {
                // no first contact just on clicking on the system
                // maybe some data if you are high tech level?
            }
        }

        internal void ResolveDiplomacyForClickFleetWeKnow(CivController localPlayerCivContoller, FleetController fleetController)
        {
            //already not one of our fleets
            CivController civPartyOne;
            CivController civPartyTwo;

            if ((int)localPlayerCivContoller.CivData.CivEnum < (int)fleetController.FleetData.CivController.CivData.CivEnum)
            {
                civPartyOne = localPlayerCivContoller;
                civPartyTwo = fleetController.FleetData.CivController;
            }
            else // other civ is side one
            {
                civPartyOne = fleetController.FleetData.CivController;
                civPartyTwo = localPlayerCivContoller;
            }
            //have we met before?
            if (DiplomacyManager.Instance.FoundADiplomacyController(civPartyOne, civPartyTwo))
            {   // not First Contact, just by clicking on the system
                DiplomacyManager.Instance.OpenDiplomacyUI(civPartyOne, civPartyTwo, fleetController.FleetData.ShipsList, null, fleetController, null);
            }
else
            {
                // no first contact just on clicking on the system
                // maybe some data if you are high tech level?
            }
        }
    

        private void OnDestroy()
        {
            ServiceLocator.Unregister<DiplomacyManager>();
        }
}
}
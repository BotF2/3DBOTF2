
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Civilization
{
    public class IntelligenceManager : MonoBehaviour, IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        public static IntelligenceManager Instance;
        [SerializeField]
        private GameObject intelligenceUIPrefab;
        public List<IntelligenceController> IntelligenceControllerList { get; private set; } = new List<IntelligenceController>();

        // Fires after every project resolves: (actionType, initiator, target, succeeded, discovered, techGain)
        public static event System.Action<SecretActionsEnum, CivEnum, CivEnum, bool, bool, int> OnProjectResolved;

        // Fires when first contact is established with a non-local-player civ
        public static event System.Action<CivEnum> OnNewContact;

        private void Awake()
        {
            ServiceLocator.Register<IntelligenceManager>(this);
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        public void InitializeNewIntelligenceController(CivController civSideOne, FleetController fleetControllerSideOne, CivController civSideTwo, FleetController fleetControllerSideTwo, StarSysController sysCon)
        {
            IntelligenceData intelData = null;
            intelData = new IntelligenceData(fleetControllerSideOne, fleetControllerSideTwo, sysCon);
            if (civSideOne.CivData.CivEnum <= CivEnum.TERRAN || civSideTwo.CivData.CivEnum <= CivEnum.TERRAN) // diplomacy only when there is one major civ
            { // one or two is a major civs

                intelData.CivSideOne = civSideOne.CivData.CivEnum;
                intelData.LastSeenFleetOfSideOne = fleetControllerSideOne; // could be null, do this on combat form Diplomacy UI
                if (fleetControllerSideOne != null && fleetControllerSideOne.FleetData != null && fleetControllerSideOne.FleetData.ShipsList != null)
                {
                    intelData.LastSeenFleetOfSideOne.FleetData.ShipsList = fleetControllerSideOne.FleetData.ShipsList; // make a copy of the list
                }
                intelData.CivSideTwo = civSideTwo.CivData.CivEnum;
                intelData.LastSeenFleetOfSideTwo = fleetControllerSideTwo; // do this on combat
                if (fleetControllerSideTwo != null && fleetControllerSideTwo.FleetData != null && fleetControllerSideTwo.FleetData.ShipsList != null)
                {
                    intelData.LastSeenFleetOfSideTwo.FleetData.ShipsList = fleetControllerSideTwo.FleetData.ShipsList;
                }
                intelData.LastSeenStarSysController = sysCon; // do this on combat
            }

            IntelligenceController intelligenceController = new IntelligenceController(intelData);
            IntelligenceControllerList.Add(intelligenceController);
            InstantiateIntelligenceUIGameObject(intelligenceController);

            if (GameController.Instance != null)
            {
                CivEnum localEnum = GameController.Instance.GameData.LocalPlayerCivEnum;
                bool localIsOne = civSideOne.CivData.CivEnum == localEnum;
                bool localIsTwo = civSideTwo.CivData.CivEnum == localEnum;
                if (localIsOne || localIsTwo)
                {
                    CivEnum contactEnum = localIsOne ? civSideTwo.CivData.CivEnum : civSideOne.CivData.CivEnum;
                    OnNewContact?.Invoke(contactEnum);
                }
            }
        }


        private void InstantiateIntelligenceUIGameObject(IntelligenceController intelligenceCon)
        {
            if (intelligenceCon.IntelligenceData.CivSideOne == GameController.Instance.GameData.LocalPlayerCivEnum
                 || intelligenceCon.IntelligenceData.CivSideTwo == GameController.Instance.GameData.LocalPlayerCivEnum)
            {
                if (intelligenceCon.IntelligenceUIGameObject == null)
                {//*** to do - make the intel UI prefab and instantiate it here
                 //GameObject thisIntelUIGameObject = (GameObject)Instantiate(intelligenceUIPrefab, new Vector3(0, 0, 0),
                 //Quaternion.identity);
                 //thisIntelUIGameObject.SetActive(true);
                 //thisIntelUIGameObject.layer = 5;
                 //intelligenceCon.IntelligenceUIGameObject = thisIntelUIGameObject;
                }
            }
        }
        public bool FoundAnIntelController(CivController civPartyOne, CivController civPartyTwo) //, GameObject hitGO)
        {
            bool found = false;
            //List<DiplomacyController> placeholderControllers = new List<DiplomacyController>();
            for (int i = 0; i < IntelligenceControllerList.Count; i++)
            {
                if (IntelligenceControllerList[i] != null)
                {
                    if (IntelligenceControllerList[i].IntelligenceData.CivSideOne == civPartyOne.CivData.CivEnum && IntelligenceControllerList[i].IntelligenceData.CivSideTwo == civPartyTwo.CivData.CivEnum
                        || IntelligenceControllerList[i].IntelligenceData.CivSideTwo == civPartyOne.CivData.CivEnum && IntelligenceControllerList[i].IntelligenceData.CivSideOne == civPartyTwo.CivData.CivEnum)
                    {
                        found = true;
                        break;
                    }
                }
            }
            return found;
        }
        public void OpenIntelligenceUI(CivEnum civPartyOne, CivEnum civPartyTwo)
        {
            IntelligenceController ourIntelligenceController = ReturnAnIntelligenceController(civPartyOne, civPartyTwo);
            if (ourIntelligenceController != null)
            {
                if (GameController.Instance.AreWeLocalPlayer(civPartyOne))
                {
                    ourIntelligenceController.IntelligenceData.CivSideOne = civPartyOne; // local player civ
                    ourIntelligenceController.IntelligenceData.CivSideTwo = civPartyTwo;
                }
                else if (GameController.Instance.AreWeLocalPlayer(civPartyTwo))
                {
                    ourIntelligenceController.IntelligenceData.CivSideOne = civPartyTwo; // local player civ
                    ourIntelligenceController.IntelligenceData.CivSideTwo = civPartyOne;
                }
                // *** build intel UI for this!!
                // GalaxyMenuUIController.Instance.OpenAIntelligenceUI(ourIntelligenceController); // it opens the AIntelligence UI
            }
        }
        public void UpdateOurIntelController(CivController civPartyOne, FleetController fleetConA, CivController civPartyTwo, FleetController fleetConB, StarSysController sysCon) //, StarSysController sysCon)
        {
            //CivController civPartyOne;
            //CivController civPartyTwo;
            //if (fleetConA.FleetData.CivEnum < sysCon.StarSysData.CurrentOwnerCivEnum)
            //{
            //    civPartyOne = fleetCon.FleetData.CivController;
            //    civPartyTwo = sysCon.StarSysData.CurrentCivController;
            //}
            //else
            //{
            //    civPartyOne = sysCon.StarSysData.CurrentCivController;
            //    civPartyTwo = fleetCon.FleetData.CivController;
            //}
            //IntelligenceController ourIntelligenceController = ReturnAnIntelligenceController(civPartyOne, civPartyTwo);
        }
        public IntelligenceController ReturnAnIntelligenceController(CivEnum civPartyOne, CivEnum civPartyTwo)
        {
            IntelligenceController intelligenceController = null;
            for (int i = 0; i < IntelligenceControllerList.Count; i++)
            {
                if (IntelligenceControllerList[i] != null && ((IntelligenceControllerList[i].IntelligenceData.CivSideOne == civPartyOne &&
                    IntelligenceControllerList[i].IntelligenceData.CivSideTwo == civPartyTwo)
                    || (IntelligenceControllerList[i].IntelligenceData.CivSideOne == civPartyTwo && IntelligenceControllerList[i].IntelligenceData.CivSideTwo == civPartyOne)))
                {
                    intelligenceController = IntelligenceControllerList[i];
                    //if (intelligenceController.IntelligenceData.LastSeenFleetOfSideOne != null)
                    //{
                    //    intelligenceController.IntelligenceData.LastSeenFleetOfSideOne.FleetData.ShipsList.;
                    //}
                    break;
                }
            }
            return intelligenceController;
        }
    

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTurnAdvanced += TickIntelProjects;
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTurnAdvanced -= TickIntelProjects;
        }

        // ─── Project creation ────────────────────────────────────────────────

        /// <summary>
        /// Initiates a covert operation. Returns false if the action cannot start
        /// (no contact record, duplicate op already running, or insufficient IntelPoints).
        /// </summary>
        public bool CreateIntelProject(SecretActionsEnum actionType, CivEnum initiatorCiv, CivEnum targetCiv,
            out string failReason)
        {
            failReason = string.Empty;

            CivController initiator = CivManager.Instance.GetCivControllerByCivEnum(initiatorCiv);
            CivController target    = CivManager.Instance.GetCivControllerByCivEnum(targetCiv);

            if (initiator == null || target == null)
            {
                failReason = "civilization not found";
                GameLogger.Log(GameLogger.LogCategory.Diplomacy, $"IntelProject: civ not found ({initiatorCiv} → {targetCiv})");
                return false;
            }

            IntelligenceController intelCon = ReturnAnIntelligenceController(initiatorCiv, targetCiv);
            if (intelCon == null)
            {
                failReason = "no contact record with that civilization";
                GameLogger.Log(GameLogger.LogCategory.Diplomacy, $"IntelProject: no contact record between {initiatorCiv} and {targetCiv}");
                return false;
            }

            // Block duplicate operations of the same type against the same target
            foreach (var existing in intelCon.IntelligenceData.ActiveProjects)
            {
                if (existing.ActionType == actionType && !existing.IsComplete)
                {
                    failReason = "that operation is already running";
                    GameLogger.Log(GameLogger.LogCategory.Diplomacy, $"IntelProject: {actionType} already running against {targetCiv}");
                    return false;
                }
            }

            // Absolute level gap (0–3): same level = easiest, three levels apart = impossible.
            float techGap = Mathf.Abs((int)target.CivData.CurrentTechLevel - (int)initiator.CivData.CurrentTechLevel);

            if (actionType == SecretActionsEnum.IntellectualTheft && techGap >= 3)
            {
                failReason = "technology gap too large — their science is incomprehensible";
                GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                    $"IntelProject: theft impossible — {initiatorCiv} is {techGap} levels below {targetCiv}");
                return false;
            }

            float cost = CalculateIntelCost(actionType, techGap);

            if (initiator.CivData.IntelPoints < cost)
            {
                failReason = $"insufficient Intel Points ({initiator.CivData.IntelPoints:F0} available, {cost:F0} needed)";
                GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                    $"IntelProject: insufficient IntelPoints ({initiator.CivData.IntelPoints:F0} < {cost:F0}) for {actionType}");
                return false;
            }

            initiator.CivData.IntelPoints -= cost;

            int   turns    = TurnsForAction(actionType);
            float success  = CalculateSuccessChance(actionType, techGap);
            float discover = CalculateDiscoveryChance(actionType, techGap);

            var project = new IntelProject(actionType, initiatorCiv, targetCiv, turns, success, discover);
            intelCon.IntelligenceData.ActiveProjects.Add(project);

            GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                $"IntelProject started: {actionType} → {targetCiv} | {turns} turns | " +
                $"Success {success:P0} | Discovery {discover:P0}");
            return true;
        }

        // ─── Per-turn tick ───────────────────────────────────────────────────

        private void TickIntelProjects()
        {
            foreach (var intelCon in IntelligenceControllerList)
            {
                if (intelCon?.IntelligenceData?.ActiveProjects == null) continue;

                for (int i = intelCon.IntelligenceData.ActiveProjects.Count - 1; i >= 0; i--)
                {
                    var project = intelCon.IntelligenceData.ActiveProjects[i];
                    if (project.IsComplete) continue;

                    project.TurnsRemaining--;

                    if (project.TurnsRemaining <= 0)
                    {
                        ResolveProject(project);
                        project.IsComplete = true;
                        intelCon.IntelligenceData.ActiveProjects.RemoveAt(i);
                    }
                }
            }
        }

        // ─── Resolution ──────────────────────────────────────────────────────

        private void ResolveProject(IntelProject project)
        {
            CivController initiator = CivManager.Instance.GetCivControllerByCivEnum(project.InitiatorCiv);
            CivController target    = CivManager.Instance.GetCivControllerByCivEnum(project.TargetCiv);
            if (initiator == null || target == null) return;

            bool succeeded  = UnityEngine.Random.value <= project.SuccessChance;
            bool discovered = !succeeded && UnityEngine.Random.value <= project.DiscoveryChance;

            switch (project.ActionType)
            {
                case SecretActionsEnum.IntellectualTheft:
                    ResolveIntellectualTheft(project, initiator, target, succeeded, discovered);
                    break;
                case SecretActionsEnum.GatherIntelligence:
                    ResolveGatherIntelligence(project, initiator, target, succeeded, discovered);
                    break;
                case SecretActionsEnum.Sabotage:
                    ResolveSabotage(project, initiator, target, succeeded, discovered);
                    break;
                case SecretActionsEnum.Disinformation:
                    ResolveDisinformation(project, initiator, target, succeeded, discovered);
                    break;
            }
        }

        private void ResolveIntellectualTheft(IntelProject project, CivController initiator, CivController target,
            bool succeeded, bool discovered)
        {
            // Reward uses directional gap: stealing from a more-advanced civ yields more points.
            // Floor at 10 so stealing from an equal or lesser civ still pays out something.
            float rewardGap = (target.CivData.TechRating - initiator.CivData.TechRating) / 2.5f;

            if (succeeded)
            {
                int techGain = Mathf.Max(10, Mathf.RoundToInt(20f * (1f + rewardGap)));
                initiator.CivData.AddTechPoints(techGain);
                GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                    $"Tech theft succeeded: {project.InitiatorCiv} ← {project.TargetCiv} | +{techGain} TechPoints");
                OnProjectResolved?.Invoke(project.ActionType, project.InitiatorCiv, project.TargetCiv, true, false, techGain);
            }
            else
            {
                GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                    $"Tech theft failed: {project.InitiatorCiv} → {project.TargetCiv}");
                if (discovered)
                    ApplyDiscoveryPenalty(project, DiplomaticEventEnum.DiscoveredIntellectualTheft);
                OnProjectResolved?.Invoke(project.ActionType, project.InitiatorCiv, project.TargetCiv, false, discovered, 0);
            }
        }

        private void ResolveGatherIntelligence(IntelProject project, CivController initiator, CivController target,
            bool succeeded, bool discovered)
        {
            if (succeeded)
            {
                initiator.CivData.IntelPoints += 10f;
                GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                    $"Intel gathered on {project.TargetCiv} | +10 IntelPoints");
                // ToDo: expose last-seen fleet/system data on the IntelligenceController
            }
            else if (discovered)
                ApplyDiscoveryPenalty(project, DiplomaticEventEnum.DiscoveredDisinformation);
            OnProjectResolved?.Invoke(project.ActionType, project.InitiatorCiv, project.TargetCiv, succeeded, discovered, 0);
        }

        private void ResolveSabotage(IntelProject project, CivController initiator, CivController target,
            bool succeeded, bool discovered)
        {
            if (succeeded)
            {
                target.CivData.IntelPoints = Mathf.Max(0f, target.CivData.IntelPoints - 20f);
                GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                    $"Sabotage succeeded against {project.TargetCiv} | -20 IntelPoints");
                // ToDo: additional effects (factory slowdown, shipyard damage)
            }
            else if (discovered)
                ApplyDiscoveryPenalty(project, DiplomaticEventEnum.DiscoveredSabotage);
            OnProjectResolved?.Invoke(project.ActionType, project.InitiatorCiv, project.TargetCiv, succeeded, discovered, 0);
        }

        private void ResolveDisinformation(IntelProject project, CivController initiator, CivController target,
            bool succeeded, bool discovered)
        {
            if (succeeded)
            {
                GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                    $"Disinformation succeeded against {project.TargetCiv}");
                // ToDo: reduce target's DiplomacyPoints with a third civ
            }
            else if (discovered)
                ApplyDiscoveryPenalty(project, DiplomaticEventEnum.DiscoveredDisinformation);
            OnProjectResolved?.Invoke(project.ActionType, project.InitiatorCiv, project.TargetCiv, succeeded, discovered, 0);
        }

        private void ApplyDiscoveryPenalty(IntelProject project, DiplomaticEventEnum discoveryEvent)
        {
            DiplomacyController diplomacyCon = DiplomacyManager.Instance
                .ReturnADiplomacyController(project.InitiatorCiv, project.TargetCiv);
            if (diplomacyCon == null) return;

            diplomacyCon.SubtractDiplomaticPoints(20);
            GameLogger.Log(GameLogger.LogCategory.Diplomacy,
                $"Intel op discovered ({discoveryEvent}): {project.InitiatorCiv} ↔ {project.TargetCiv} | -20 DiplomacyPoints");
        }

        // ─── Calculation helpers ─────────────────────────────────────────────

        /// <summary>
        /// Returns preview numbers shown in the Intel UI before the player commits.
        /// Gap of 3 is impossible; gap 0–2 scales difficulty.
        /// </summary>
        public void GetTheftPreview(CivController initiator, CivController target,
            out bool possible, out float successChance, out float discoveryChance, out int potentialGain)
        {
            int levelGap = Mathf.Abs((int)target.CivData.CurrentTechLevel - (int)initiator.CivData.CurrentTechLevel);
            possible        = levelGap < 3;
            successChance   = possible ? CalculateSuccessChance(SecretActionsEnum.IntellectualTheft, levelGap) : 0f;
            discoveryChance = possible ? CalculateDiscoveryChance(SecretActionsEnum.IntellectualTheft, levelGap) : 0f;
            float rewardGap = (target.CivData.TechRating - initiator.CivData.TechRating) / 2.5f;
            potentialGain   = possible ? Mathf.Max(10, Mathf.RoundToInt(20f * (1f + rewardGap))) : 0;
        }

        private static int TurnsForAction(SecretActionsEnum action)
        {
            switch (action)
            {
                case SecretActionsEnum.GatherIntelligence: return 1;
                case SecretActionsEnum.Disinformation:     return 2;
                case SecretActionsEnum.Sabotage:           return 2;
                case SecretActionsEnum.IntellectualTheft:  return 3;
                default:                                   return 1;
            }
        }

        private static float CalculateIntelCost(SecretActionsEnum action, float techGap)
        {
            switch (action)
            {
                case SecretActionsEnum.GatherIntelligence: return 5f;
                case SecretActionsEnum.Disinformation:     return 10f;
                case SecretActionsEnum.Sabotage:           return 15f;
                case SecretActionsEnum.IntellectualTheft:  return 10f * (1f + techGap);
                default:                                   return 5f;
            }
        }

        private static float CalculateSuccessChance(SecretActionsEnum action, float techGap)
        {
            switch (action)
            {
                case SecretActionsEnum.GatherIntelligence: return 0.85f;
                case SecretActionsEnum.Disinformation:     return 0.60f;
                case SecretActionsEnum.Sabotage:           return 0.50f;
                case SecretActionsEnum.IntellectualTheft:  return 0.5f / (1f + techGap * 0.5f);
                default:                                   return 0.5f;
            }
        }

        private static float CalculateDiscoveryChance(SecretActionsEnum action, float techGap)
        {
            switch (action)
            {
                case SecretActionsEnum.GatherIntelligence: return 0.10f;
                case SecretActionsEnum.Disinformation:     return 0.20f;
                case SecretActionsEnum.Sabotage:           return 0.35f;
                case SecretActionsEnum.IntellectualTheft:  return 0.30f + techGap * 0.10f;
                default:                                   return 0.20f;
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IntelligenceManager>();
        }
    }
}
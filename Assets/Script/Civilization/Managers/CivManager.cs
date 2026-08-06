
using BOTF3D.Core;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



namespace BOTF3D.Civilization
{
    /// <summary>
    /// Instantiates the Civilizations(factions) (a CivController and a CivData) using CivSO
    /// See civ SOs listed in Unity CivManager SerializeFields
    /// Playable civs are: 0 FED, 1 ROM, 2 KLING, 3 CARD, 4 DOM, 5 BORG, 6 TERRAN
    /// FOR CANON MAP GETS THESE MINORS NEAR THE PLAYALBLES PER MAP SIZE AND + MORE RANDOMS PER MAP SIZE
    /// SMALL map minor race near: FED = 146 VULCAN, ROM = 62 GORN, KLING = 131 THOLIANS, CARD = 24 BAJORANS, DOM = 73 KAREMMA, BORG = 142 VIDIANS, TERRAN = 54 EDO
    /// MEDIUM map minors adds near: FED = 129 TELLARITES, ROM = 37 BREEN, KLING = 96 NAUSICAANS, CARD = 85 LURIANS, DOM = 147 WADI, BORG = 74 KAZON, TERRAN = 30 BETAZOIDS
    /// LARGE map minors adds near: FED = 13 ANDORIAN, ROM = 155 ZAKDORN, KLING = 156 ZIBALIANS, CARD = 121 TAKARANS, DOM = 51 DOSI, BORG = 145 VORI, TERRAN = 47 DELTANS
    /// </summary>

    public class CivManager : MonoBehaviour, IManager
    {
        public void Initialize() { }
        public void Cleanup() { }
        public static CivManager Instance;
        [SerializeField]
        public List<CivSO> CivSOListAllPossible;
        public List<CivSO> CivSOsInGame;
        [SerializeField]
        private List<CivSO> smallMapMinorNeighborsInGame;
        [SerializeField]
        private List<CivSO> mediumMapMinorNeighborsInGame;
        [SerializeField]
        private List<CivSO> largeMapMinorNeighborsInGame;
        private List<CivSO> randomMinorsInGame;

        public List<CivEnum> CivEnumsInGame;
        public List<CivData> CivDataInGameList = new List<CivData> { new CivData() };
        public List<CivController> CivControllersInGame { get; private set; } = new List<CivController>();

        public bool isSinglePlayer;
        public List<CivEnum> InGamePlayableCivs;
        public CivController LocalPlayerCivController;

        //public bool nowCivsCanJoinTheFederation = true; // for use with testing a multiple star system Federation
        private int HoldCivSize = 0;// used in testing of a multiStarSystem civilization/faction
        [SerializeField]
        private GameObject civFolder; // hold civs in Hierarchy, using the CivilizationFolder as a parent in Hierarchy
        [SerializeField]
        private CivController civPrefab;
        private void Awake()
        {
            ServiceLocator.Register<CivManager>(this);
            if (Instance != null) { Destroy(gameObject); }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            //ToDo: early random minor races set before menu selects size and tech
        }


        public void UpdatePlayableCivGameList(List<CivEnum> listPlayableCivEnumForCivSOs, int galaxySize, GalaxyMapType galaxyType)
        {
            if (galaxyType == GalaxyMapType.CANON)
            {
                #region COMMENT OUT SELECTIVE CIS AND TURN ON ALL CIVS BELOW
                //// **********TURN OFF SELECTIVE CIVS HERE AND TURN ON ALL CIVS BELOW *******
                ///

                List<CivSO> _SOsInGame = new List<CivSO>();
                for (int i = 0; i < listPlayableCivEnumForCivSOs.Count; i++)
                {
                    if (listPlayableCivEnumForCivSOs[i] != CivEnum.ZZUNINHABITED1)
                    {
                        _SOsInGame.Add(CivSOListAllPossible[i]); // add the playable
                        _SOsInGame.Add(smallMapMinorNeighborsInGame[i]); // add playable's minor races
                        if (galaxySize >= 1)
                            _SOsInGame.Add(mediumMapMinorNeighborsInGame[i]);
                        if (galaxySize >= 2)
                            _SOsInGame.Add(largeMapMinorNeighborsInGame[i]);
                    }
                }
                SetRandomCanonCivsByGalaxySize(galaxySize, _SOsInGame);
                CivSOsInGame = _SOsInGame;

                Debug.Log($"=== CANON galaxy generated: {CivSOsInGame.Count} civs: {string.Join(", ", CivSOsInGame.Select(c => c.CivShortName))} ===");

                ////**** See all Civs -  ****
                // CivSOsInGame = CivSOListAllPossible;
                #endregion TURN ON ALL CIVs WITH LAST LINE ABOVE
            }
            else if (galaxyType == GalaxyMapType.RANDOM)
            {
                // ✅ FIX: Implement random galaxy generation
                Debug.Log($"=== UpdatePlayableCivGameList: RANDOM galaxy (size={galaxySize}) ===");

                List<CivSO> _SOsInGame = new List<CivSO>();

                // ✅ Step 1: Add selected playable civs (user choices from main menu)
                for (int i = 0; i < listPlayableCivEnumForCivSOs.Count; i++)
                {
                    if (listPlayableCivEnumForCivSOs[i] != CivEnum.ZZUNINHABITED1)
                    {
                        _SOsInGame.Add(CivSOListAllPossible[i]); // Add the playable civ
                        Debug.Log($"  Added playable: {CivSOListAllPossible[i].CivShortName}");
                    }
                }

                // ✅ Step 2: Add random minor races based on galaxy size
                // Small = 7 playables + ~40 minors (~47 total)
                // Medium = 7 playables + ~80 minors (~87 total)
                // Large = 7 playables + ~120 minors (~127 total)
                // Extreme = 7 playables + ~160 minors (~167 total)
                int targetMinorCount = (galaxySize + 1) * 40;

                List<CivSO> availableMinors = new List<CivSO>();
                for (int i = 0; i < CivSOListAllPossible.Count; i++)
                {
                    // Skip playables and uninhabited
                    if (!CivSOListAllPossible[i].Playable &&
                        CivSOListAllPossible[i].CivEnum != CivEnum.ZZUNINHABITED1)
                    {
                        availableMinors.Add(CivSOListAllPossible[i]);
                    }
                }

                // ✅ Shuffle - UnityEngine.Random (not Guid.NewGuid) so this replays identically
                // across clients when seeded via UnityEngine.Random.InitState before generation.
                availableMinors = availableMinors.OrderBy(x => UnityEngine.Random.value).ToList();

                // ✅ Take the first N minors
                int minorsToAdd = Mathf.Min(targetMinorCount, availableMinors.Count);
                for (int i = 0; i < minorsToAdd; i++)
                {
                    _SOsInGame.Add(availableMinors[i]);
                    Debug.Log($"  Added minor: {availableMinors[i].CivShortName}");
                }

                CivSOsInGame = _SOsInGame;
                Debug.Log($"=== RANDOM galaxy complete: {CivSOsInGame.Count} total civs ===");
            }
            else if (galaxyType == GalaxyMapType.RING)
            {
                // TODO: Implement ring galaxy here
                Debug.LogWarning("RING galaxy type not yet implemented - using CANON logic");

                // ✅ Fallback to CANON for now
                List<CivSO> _SOsInGame = new List<CivSO>();
                for (int i = 0; i < listPlayableCivEnumForCivSOs.Count; i++)
                {
                    if (listPlayableCivEnumForCivSOs[i] != CivEnum.ZZUNINHABITED1)
                    {
                        _SOsInGame.Add(CivSOListAllPossible[i]);
                        _SOsInGame.Add(smallMapMinorNeighborsInGame[i]);
                        if (galaxySize >= 1)
                            _SOsInGame.Add(mediumMapMinorNeighborsInGame[i]);
                        if (galaxySize == 2)
                            _SOsInGame.Add(largeMapMinorNeighborsInGame[i]);
                    }
                }
                SetRandomCanonCivsByGalaxySize(galaxySize, _SOsInGame);
                CivSOsInGame = _SOsInGame;
            }
            else if (galaxyType == GalaxyMapType.WHATEVER)
            {
                // do something else here
                Debug.LogWarning("WHATEVER galaxy type not implemented");
            }

        }
        private void SetRandomCanonCivsByGalaxySize(int galaxySize, List<CivSO> _SOsInGame)
        {
            // UnityEngine.Random (not Guid.NewGuid) so this replays identically across clients
            // when seeded via UnityEngine.Random.InitState before generation.
            CivSOListAllPossible = CivSOListAllPossible.OrderBy(i => UnityEngine.Random.value).ToList();

            for (int i = 0; i < (50 * (1 + galaxySize)); i++)
            {
                for (int j = 0; j < CivSOListAllPossible.Count; j++)
                {
                    int oneMoreCiv = j;
                    {
                        if (!_SOsInGame.Contains(CivSOListAllPossible[i]))
                        {
                            _SOsInGame.Add(CivSOListAllPossible[i]);
                            break;
                        }
                        else if (!_SOsInGame.Contains(CivSOListAllPossible[i + 1]))
                        {
                            _SOsInGame.Add(CivSOListAllPossible[i + 1]);
                            j++;
                            break;
                        }
                        else
                            j++;
                    }
                }
            }
        }
        public IEnumerator CreateNewGameBySelections(int sizeGame, int gameTechLevel, int galaxyType, int localPlayerCivInt, bool isSingleVsMultiplayer)
        {
            // MainMenuUIController.Instance is null on a true dedicated server (no local player, no
            // MainMenuScene ever loaded there) - this cache is purely for the menu UI's own reads
            // elsewhere, so it's skipped when absent. GameController.Instance.GameData below is the
            // one every downstream generation method (this class + StarSysManager) should read from,
            // since that manager exists on every machine including a headless dedicated server.
            if (MainMenuUIController.Instance != null)
            {
                MainMenuUIController.Instance.MainMenuData.SelectedGalaxySize = (GalaxySize)sizeGame;
                MainMenuUIController.Instance.MainMenuData.SelectedTechLevel = (TechLevel)gameTechLevel;
                MainMenuUIController.Instance.MainMenuData.SelectedGalaxyType = (GalaxyMapType)galaxyType;
            }
            GameController.Instance.GameData.GalaxySize = (GalaxySize)sizeGame;
            GameController.Instance.GameData.StartingTechLevel = (TechLevel)gameTechLevel;
            GameController.Instance.GameData.GalaxyMapType = (GalaxyMapType)galaxyType;
            isSinglePlayer = isSingleVsMultiplayer;
            GameController.Instance.GameData.LocalPlayerCivEnum = (CivEnum)localPlayerCivInt;

            // Stardate is a server-authoritative SyncVar (TimeManager.syncedStardate) - only the
            // server may assign it, but this coroutine runs on every client (see comment above on
            // MainMenuUIController.Instance). Re-applying it here (not just once at app launch in
            // TimeManager.Start()) also makes sure a second game started in the same app session
            // resets the clock instead of carrying over the previous game's stardate.
            if (NetworkServer.active && TimeManager.Instance != null)
                TimeManager.Instance.ApplyStartingStardate((TechLevel)gameTechLevel);

            // Keep ThemeManager.CurrentTheme in sync with the final local player civ.
            // MainMenuUIController.SetLocalCivilization() updates both together during civ browsing,
            // but this is the authoritative "start game" commit and must re-sync in case it drifted.
            if (ThemeManager.Instance != null)
                ThemeManager.Instance.ApplyTheme((ThemeEnum)localPlayerCivInt);

            yield return CivDataFromSO(CivSOsInGame, localPlayerCivInt);
            CreateCivEnumList(CivSOsInGame);
        }
        public IEnumerator CivDataFromSO(List<CivSO> civSOList, int localPayerCivInt)
        {
            Debug.Log($"=== CivDataFromSO: Creating {civSOList.Count} civilizations ===");

            for (int i = 0; i < civSOList.Count; i++)
            {
                CivData civData = new CivData();
                civData.CivInt = civSOList[i].CivInt;
                civData.CivEnum = civSOList[i].CivEnum;
                civData.CivLongName = civSOList[i].CivLongName;
                civData.CivShortName = civSOList[i].CivShortName;
                civData.QualityScore = civSOList[i].QualityScore;
                civData.Warlike = (WarLikeEnum)civSOList[i].WarLikeEnum; // a scale from 0 to neutral 3 and most peaceful at 5
                civData.Xenophobia = civSOList[i].XenophbiaEnum;
                civData.Ruthless = civSOList[i].RuthlessEnum;
                civData.Greedy = civSOList[i].GreedyEnum;
                civData.CivRaceSprite = civSOList[i].CivImage;
                civData.InsigniaSprite = civSOList[i].Insignia;
                TechLevel startLevel = GameController.Instance.GameData.StartingTechLevel;
                civData.TechPoints = CivData.TechThresholds[startLevel] + civSOList[i].TechPoints;
                civData.Playable = civSOList[i].Playable;
                civData.HasWarp = civSOList[i].HasWarp;
                civData.Decription = civSOList[i].Decription;
                civData.IntelPoints = civSOList[i].Playable
                    ? Mathf.Max(25f, civSOList[i].IntelPoints)
                    : civSOList[i].IntelPoints;
                CivDataInGameList.Add(civData);
                InstantiateCivilizations(civData, localPayerCivInt);
            }

            if (CivDataInGameList[0].CivHomeSystemName != null) { }
            else
                CivDataInGameList.Remove(CivDataInGameList[0]);

            Debug.Log($"CivDataFromSO: Calling StarSysManager.SysDataFromSO with {civSOList.Count} civs");

            // CRITICAL: Check if StarSysManager exists
            if (StarSysManager.Instance != null)
            {
                yield return StarSysManager.Instance.SysDataFromSO(civSOList);
                Debug.Log($"CivDataFromSO: StarSysManager created systems");
            }
            else
            {
                Debug.LogError("CivDataFromSO: ? StarSysManager.Instance is NULL! Systems won't be created!");
                Debug.LogError("  Make sure StarSysManager exists in GalaxyScene and scene is loaded");
            }
        }
        private void InstantiateCivilizations(CivData civData, int localPlayerCivInt)
        {
            CivController civController = Instantiate(civPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            civController.Init(this);
            civController.CivData = civData;
            civController.CivShortName = civData.CivShortName;
            CivControllersInGame.Add(civController);
            civController.transform.SetParent(civFolder.transform, true);
            civController.name = civData.CivShortName.ToString();
            if (localPlayerCivInt == civController.CivData.CivInt)
            {
                LocalPlayerCivController = civController;
                StarSysManager.Instance.SetShipBuildPrefabs(civController.CivData.CivEnum);
            }

        }

        void CreateCivEnumList(List<CivSO> listOfCivSO)
        {
            for (int i = 0; i < listOfCivSO.Count; i++)
            {
                CivEnumsInGame.Add(listOfCivSO[i].CivEnum);
            }
            FleetManager.Instance.CleanUpDictionaryForFleetNums();
        }
        public CivData GetCivDataByName(string shortName)
        {

            CivData result = null;
            for (int i = 0; i < CivDataInGameList.Count; i++)
            {
                if (CivDataInGameList[i].CivShortName.Equals(shortName))
                {
                    result = CivDataInGameList[i];
                }
            }
            return result;

        }
        public CivData GetCivDataByCivEnum(CivEnum civEnum)
        {
            CivData result = null;
            for (int i = 0; i < CivDataInGameList.Count; i++)
            {

                if (CivDataInGameList[i].CivEnum.Equals(civEnum))
                {
                    result = CivDataInGameList[i];
                }
            }
            return result;

        }
        public IEnumerator OnNewGameButtonClicked(int gameSize, int gameTechLevel, int galaxyType, int selectedLocalCiv, bool isSingle)
        {
            yield return CreateNewGameBySelections(gameSize, gameTechLevel, galaxyType, selectedLocalCiv, isSingle);
        }

        public void AddSystemToOwnSystemListAndHomeSys(List<StarSysController> controllers)
        {
            for (int i = 0; i < CivControllersInGame.Count; i++)
            {
                if (CivControllersInGame[i].CivData.CivEnum == controllers[0].StarSysData.CurrentOwnerCivEnum)
                {
                    CivControllersInGame[i].CivData.StarSysWeOwn = controllers;
                    CivControllersInGame[i].CivData.CivHomeSystemName = controllers[0].StarSysData.SysName;
                    CivControllersInGame[i].CivData.HomeStarSystemPosition = controllers[0].transform.position;
                }
            }
        }
        /// <summary>
        /// Full immediate annexation when a minor race civ reaches Membership status with a major civ:
        /// transfers every system the minor owns to the major civ and fires an elimination event.
        /// Existing minor-race ships/facilities are reflagged to the major civ in place (same hulls,
        /// same models) - only new ships built afterward come out looking like the major civ, since
        /// ship builds already key off the system's CurrentOwnerCivEnum.
        /// </summary>
        public void AnnexMinorCiv(CivEnum majorCivEnum, CivEnum minorCivEnum)
        {
            CivController majorCiv = GetCivControllerByCivEnum(majorCivEnum);
            CivController minorCiv = GetCivControllerByCivEnum(minorCivEnum);
            if (majorCiv?.CivData == null || minorCiv?.CivData == null) return;

            majorCiv.CivData.StarSysWeOwn ??= new List<StarSysController>();

            if (minorCiv.CivData.StarSysWeOwn != null)
            {
                foreach (var sysCon in minorCiv.CivData.StarSysWeOwn)
                {
                    if (sysCon == null) continue;

                    sysCon.UpdateOwner(majorCivEnum);
                    sysCon.StarSysData.CurrentCivController = majorCiv;
                    ReflagSystemAssets(sysCon, majorCivEnum);

                    if (!majorCiv.CivData.StarSysWeOwn.Contains(sysCon))
                        majorCiv.CivData.StarSysWeOwn.Add(sysCon);

                    GameEvents.SystemOwnershipChanged(sysCon.StarSysData.SysName, majorCivEnum);
                }
                minorCiv.CivData.StarSysWeOwn.Clear();
            }

            // Fleets in transit (not docked at any system) also belong to the minor civ and need
            // to convert - a Vulcan patrol fleet en route somewhere becomes a Federation fleet too.
            if (FleetManager.Instance != null)
            {
                foreach (var fleetCon in FleetManager.Instance.FleetControllerList)
                {
                    if (fleetCon?.FleetData == null || fleetCon.FleetData.CivEnum != minorCivEnum) continue;
                    fleetCon.FleetData.CivEnum = majorCivEnum;
                    if (fleetCon.FleetData.ShipsList != null)
                        foreach (var ship in fleetCon.FleetData.ShipsList)
                            if (ship?.ShipData != null) ship.ShipData.CivEnum = majorCivEnum;
                }
            }

            Debug.Log($"[CivManager] {minorCivEnum} annexed into {majorCivEnum} via Membership — full ownership transfer complete.");
            GameEvents.CivEliminated(minorCivEnum);
        }

        /// <summary>
        /// Reflags every ship docked at a system plus its six facility types to a new owner civ.
        /// Ships keep their original hulls/visuals; facilities carry per-system CivEnum data used by
        /// the build UI, so this alone makes the system's future builds come out as the new owner.
        /// </summary>
        private void ReflagSystemAssets(StarSysController sysCon, CivEnum newOwnerCivEnum)
        {
            if (sysCon.StarSysData.ShipsList != null)
                foreach (var ship in sysCon.StarSysData.ShipsList)
                    if (ship?.ShipData != null) ship.ShipData.CivEnum = newOwnerCivEnum;

            if (sysCon.StarSysData.PowerPlantData != null) sysCon.StarSysData.PowerPlantData.CivEnum = newOwnerCivEnum;
            if (sysCon.StarSysData.FactoryData != null) sysCon.StarSysData.FactoryData.CivEnum = newOwnerCivEnum;
            if (sysCon.StarSysData.ShipyardData != null) sysCon.StarSysData.ShipyardData.CivEnum = newOwnerCivEnum;
            if (sysCon.StarSysData.ShieldGeneratorData != null) sysCon.StarSysData.ShieldGeneratorData.CivEnum = newOwnerCivEnum;
            if (sysCon.StarSysData.OrbitalBatteryData != null) sysCon.StarSysData.OrbitalBatteryData.CivEnum = newOwnerCivEnum;
            if (sysCon.StarSysData.ResearchCenterData != null) sysCon.StarSysData.ResearchCenterData.CivEnum = newOwnerCivEnum;
        }

        /// <summary>
        /// Transfers a single system's ownership to a new civ (e.g. Borg assimilation after winning
        /// a system-defense combat). Unlike AnnexMinorCiv this moves only one system, not a civ's
        /// whole territory, and does not eliminate the previous owner.
        /// </summary>
        public void AssimilateSystem(StarSysController sysCon, CivEnum newOwnerCivEnum)
        {
            if (sysCon?.StarSysData == null) return;

            CivEnum previousOwnerCivEnum = sysCon.StarSysData.CurrentOwnerCivEnum;
            CivController previousOwner = GetCivControllerByCivEnum(previousOwnerCivEnum);
            CivController newOwner = GetCivControllerByCivEnum(newOwnerCivEnum);
            if (newOwner?.CivData == null) return;

            previousOwner?.CivData?.StarSysWeOwn?.Remove(sysCon);

            newOwner.CivData.StarSysWeOwn ??= new List<StarSysController>();
            if (!newOwner.CivData.StarSysWeOwn.Contains(sysCon))
                newOwner.CivData.StarSysWeOwn.Add(sysCon);

            sysCon.UpdateOwner(newOwnerCivEnum);
            sysCon.StarSysData.CurrentCivController = newOwner;

            Debug.Log($"[CivManager] {sysCon.StarSysData.SysName} assimilated by {newOwnerCivEnum} (was {previousOwnerCivEnum}).");
            GameEvents.SystemOwnershipChanged(sysCon.StarSysData.SysName, newOwnerCivEnum);
        }

        public CivController GetLocalPlayerCivController()
        {
            CivController civController = null;
            for (int i = 0; i < CivControllersInGame.Count; i++)
            {
                if (CivControllersInGame[i] == CivManager.Instance.LocalPlayerCivController)
                    civController = CivControllersInGame[i];
            }
            return civController;
        }
        public List<CivController> GetAllCivControllers()
        {
            return CivControllersInGame;
        }
        public CivController GetCivControllerByCivEnum(CivEnum civEnum)
        {
            CivController civController = null;
            for (int i = 0; i < CivControllersInGame.Count; i++)
            {
                if (CivControllersInGame[i].CivData.CivEnum == civEnum)
                {
                    civController = CivControllersInGame[i];
                    break;
                }
            }
            return civController;
        }

        // NEW: Store galaxy state data (not the actual GameObjects)
        [System.Serializable]
        public class GalaxyStateData
        {
            public List<StarSysData> starSystemsData = new List<StarSysData>();
            public List<FleetData> fleetsData = new List<FleetData>();
            // etc.
        }

        public GalaxyStateData CurrentGalaxyState = new GalaxyStateData();

        // Called by StarSysManager after it loads
        public void RegisterStarSystemData(StarSysData data)
        {
            if (!CurrentGalaxyState.starSystemsData.Contains(data))
            {
                CurrentGalaxyState.starSystemsData.Add(data);
            }
        }


        private void OnDestroy()
        {
            ServiceLocator.Unregister<CivManager>();
        }
    }
}

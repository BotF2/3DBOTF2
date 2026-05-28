
using BOTF3D.Core;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using System;
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

                // ✅ Shuffle using Fisher-Yates
                availableMinors = availableMinors.OrderBy(x => Guid.NewGuid()).ToList();

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
            CivSOListAllPossible = CivSOListAllPossible.OrderBy(i => Guid.NewGuid()).ToList();

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
        public void CreateNewGameBySelections(int sizeGame, int gameTechLevel, int galaxyType, int localPlayerCivInt, bool isSingleVsMultiplayer)
        {
            MainMenuUIController.Instance.MainMenuData.SelectedGalaxySize = (GalaxySize)sizeGame;
            GameController.Instance.GameData.GalaxySize = (GalaxySize)sizeGame;
            MainMenuUIController.Instance.MainMenuData.SelectedTechLevel = (TechLevel)gameTechLevel;
            GameController.Instance.GameData.StartingTechLevel = (TechLevel)gameTechLevel;
            MainMenuUIController.Instance.MainMenuData.SelectedGalaxyType = (GalaxyMapType)galaxyType;
            GameController.Instance.GameData.GalaxyMapType = (GalaxyMapType)galaxyType;
            isSinglePlayer = isSingleVsMultiplayer;
            GameController.Instance.GameData.LocalPlayerCivEnum = (CivEnum)localPlayerCivInt;
            CivDataFromSO(CivSOsInGame, localPlayerCivInt);
            CreateCivEnumList(CivSOsInGame);
        }
        public void CivDataFromSO(List<CivSO> civSOList, int localPayerCivInt)
        {
            Debug.Log($"=== CivDataFromSO: Creating {civSOList.Count} civilizations ===");

            for (int i = 0; i < civSOList.Count; i++)
            {
                CivData civData = new CivData();
                civData.CivInt = civSOList[i].CivInt;
                civData.CivEnum = civSOList[i].CivEnum;
                civData.CivLongName = civSOList[i].CivLongName;
                civData.CivShortName = civSOList[i].CivShortName;
                civData.Warlike = (WarLikeEnum)civSOList[i].WarLikeEnum; // a scale from 0 to neutral 3 and most peaceful at 5
                civData.Xenophbia = civSOList[i].XenophbiaEnum;
                civData.Ruthelss = civSOList[i].RuthlessEnum;
                civData.Greedy = civSOList[i].GreedyEnum;
                civData.CivRaceSprite = civSOList[i].CivImage;
                civData.InsigniaSprite = civSOList[i].Insignia;
                civData.TechPoints = civSOList[i].TechPoints;
                civData.CurrentTechLevel = MainMenuUIController.Instance.MainMenuData.SelectedTechLevel;
                civData.Playable = civSOList[i].Playable;
                civData.HasWarp = civSOList[i].HasWarp;
                civData.Decription = civSOList[i].Decription;
                civData.IntelPoints = civSOList[i].IntelPoints;
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
                StarSysManager.Instance.SysDataFromSO(civSOList);
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
        public void OnNewGameButtonClicked(int gameSize, int gameTechLevel, int galaxyType, int selectedLocalCiv, bool isSingle)
        {
            CreateNewGameBySelections(gameSize, gameTechLevel, galaxyType, selectedLocalCiv, isSingle);
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

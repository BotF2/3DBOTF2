using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Core
{
    /// <summary>
    /// Instantiates the Civilizations(factions) (a CivController and a CivData) using CivSO
    /// See civ SOs listed in Unity CivManager SerializeFields
    /// Playable civs are: 0 FED, 1 ROM, 2 KLING, 3 CARD, 4 DOM, 5 BORG, 6 TERRAN
    /// FOR CANON MAP GETS THESE MINORS NEAR THE PLAYALBLES PER MAP SIZE AND + MORE RANDOMS PER MAP SIZE
    ///  SMALL map minor race near: FED = 146 VULCAN, ROM = 62 GORN, KLING = 131 THOLIANS, CARD = 24 BAJORANS, DOM = 73 KAREMMA, BORG = 142 VIDIANS, TERRAN = 54 EDO
    ///  MEDIUM map minors adds near: FED = 129 TELLARITES, ROM = 37 BREEN, KLING = 96 NAUSICAANS, CARD = 85 LURIANS, DOM = 147 WADI, BORG = 74 KAZON, TERRAN = 30 BETAZOIDS
    ///  LARGE map minors adds near: FED = 13 ANDORIAN, ROM = 155 ZAKDORN, KLING = 156 ZIBALIANS, CARD = 121 TAKARANS, DOM = 51 DOSI, BORG = 145 VORI, TERRAN = 47 DELTANS
    /// </summary>

    public class CivManager : MonoBehaviour
    {
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
        public List<CivData> CivDataInGameList = new List<CivData> { new CivData() }; // CivData is not MonoBehavior so new is OK
        public List<CivController> CivControllersInGame { get; private set; } = new List<CivController>();

        //public CivData LocalPlayerCivEnum;// This will be set by NetCode checking if NetworkObject belongs to the local player by comparing the NetworkObject.OwnerClientId with NetworkManager.Singleton.LocalClientId. 
        public bool isSinglePlayer;
        public List<CivEnum> InGamePlayableCivs;
        public CivController LocalPlayerCivContoller; // ToDo: set by NetCode checking if NetworkObject belongs to the local player by comparing the NetworkObject.OwnerClientId with NetworkManager.Singleton.LocalClientId.

        //public bool nowCivsCanJoinTheFederation = true; // for use with testing a muliple star system Federation
        private int HoldCivSize = 0;// used in testing of a multiStarSystem civilization/faction
        [SerializeField]
        private GameObject civFolder; // hold civs in Hierachy, using the CivManager as a parent in Hierarchy
        [SerializeField]
        private CivController civPrefab;
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            //ToDo: early random minor races set before menu selects size and tech
        }
        private void Update()
        {
            #region temp multi-starsystem hack
            //***** This is temporary so we can test a multi-starsystem civCon
            //******* before diplomacy will alow civs/systems to join another civCon

            //if (nowCivsCanJoinTheFederation && HoldCivSize == 1)
            //{
            //    foreach (var civCon in CivControllersInGame)
            //    {
            //        if(civCon.CivData.CivEnum == CivEnum.ANDORIANS || civCon.CivData.CivEnum == CivEnum.VULCANS || civCon.CivData.CivEnum == CivEnum.TELLARITES)
            //        {
            //            civCon.CivData.CivInt = CivControllersInGame[0].CivData.CivInt;
            //            civCon.CivData.CivEnum = CivControllersInGame[0].CivData.CivEnum;
            //            civCon.CivData.CivShortName = CivControllersInGame[0].CivData.CivShortName;
            //            civCon.CivData.CivLongName = CivControllersInGame[0].CivData.CivLongName;
            //            civCon.CivData.CivHomeSystemName = CivControllersInGame[0].CivData.CivHomeSystemName;
            //            civCon.CivData.TraitOne = CivControllersInGame[0].CivData.TraitOne;
            //            civCon.CivData.TraitTwo = CivControllersInGame[0].CivData.TraitTwo;
            //            civCon.CivData.CivRaceSprite = CivControllersInGame[0].CivData.CivRaceSprite;
            //            civCon.CivData.InsigniaSprite = CivControllersInGame[0].CivData.InsigniaSprite;
            //            civCon.CivData.Playable = true;
            //            civCon.CivData.PlayedByAI = true;
            //            civCon.CivData.HasWarp =true;
            //            civCon.CivData.Decription = "temp civCon member of Federation";
            //        }
            //    }
            //    StarSysManager.current.UpdateStarSystemOwner(CivEnum.ANDORIANS, CivEnum.FED);
            //    StarSysManager.current.UpdateStarSystemOwner(CivEnum.VULCANS, CivEnum.FED);
            //    StarSysManager.current.UpdateStarSystemOwner(CivEnum.TELLARITES, CivEnum.FED);
            //}
            //nowCivsCanJoinTheFederation = false;
            #endregion
        }
        public void SetSingleVsMulitplayer()
        {

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
                        if (galaxySize == 2)
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
                // do random map here
            }
            else if (galaxyType == GalaxyMapType.RING)
            {
                // do ring galaxy here
            }
            else if (galaxyType == GalaxyMapType.WHATEVER)
            {
                // do something else here
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
            for (int i = 0; i < civSOList.Count; i++) 
            {
                CivData civData = new CivData(); // CivData is not MonoBehavior so new is OK
                civData.CivInt = civSOList[i].CivInt;
                civData.CivEnum = civSOList[i].CivEnum;
                civData.CivLongName = civSOList[i].CivLongName;
                civData.CivShortName = civSOList[i].CivShortName;
                civData.Warlike = (WarLikeEnum)civSOList[i].WarLikeEnum; // a scale from most worklike 0 to neutral 3 and most peasful at 5
                civData.Xenophbia = civSOList[i].XenophbiaEnum;
                civData.Ruthelss = civSOList[i].RuthlessEnum;
                civData.Greedy = civSOList[i].GreedyEnum;
                civData.CivRaceSprite = civSOList[i].CivImage;
                civData.InsigniaSprite = civSOList[i].Insignia;
                civData.Population = civSOList[i].Population;
                civData.LocalPlayerCivEnum = ((CivEnum)localPayerCivInt);
                civData.TechPoints = civSOList[i].TechPoints;
                civData.TechLevel = MainMenuUIController.Instance.MainMenuData.SelectedTechLevel;
                civData.Playable = civSOList[i].Playable;
                civData.HasWarp = civSOList[i].HasWarp;
                civData.Decription = civSOList[i].Decription;
                civData.IntelPoints = civSOList[i].IntelPoints;
                CivDataInGameList.Add(civData);
                InstantiateCivilizations(civData, localPayerCivInt);

            }
            if (CivDataInGameList[0].CivHomeSystemName != null) { }
            else
                CivDataInGameList.Remove(CivDataInGameList[0]); // remove the null entered by field
            StarSysManager.Instance.SysDataFromSO(civSOList);
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

            // NetCode, NetworkObject belongs to the local player
            if (localPlayerCivInt == civController.CivData.CivInt)
            {
                LocalPlayerCivContoller = civController;
                StarSysManager.Instance.SetShipBuildPerfabs(civController.CivData.CivEnum);
            }

        }

        void CreateStarSystemsWeOwnList(List<CivSO> list)
        {
            //for (int i = 0; i < .Count; i++)
            //{

            //}
        }
        void CreateCivEnumList(List<CivSO> listOfCivSO)
        {
            for (int i = 0; i < listOfCivSO.Count; i++) 
            {
                CivEnumsInGame.Add(listOfCivSO[i].CivEnum);
            }
            FleetManager.Instance.CleanUpDictinaryForFleetNums();
            }
        public CivData GetCivDataByName(string shortName)
        {

            CivData result = null;
            for (int i = 0;i < CivDataInGameList.Count;i++)
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
            for(int i = 0; i<CivDataInGameList.Count;i++)
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
                    CivControllersInGame[i].CivData.StarSysOwned = controllers;
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
                if (CivControllersInGame[i] == CivManager.Instance.LocalPlayerCivContoller)
                    civController = CivControllersInGame[i];
            }
            return civController;
        }
    }
}

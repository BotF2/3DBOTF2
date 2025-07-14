using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Assets.Core;

public class Combat
{
    //ToDo: get a list of combatants form galaxy map / diplomacy
    // call diplomayc WhoFigthsWithMe(CivController civ) with civs from galaxy map
    // call WhoIsAtWar(CivController civ) with civs and build a list of FriendShips on left and EnemyShips on right.
    // Hard coded for now
    public List<GameObject> _friendCombatans; // for now get the combatant gameObjects as they are instantiated in InstantiatCombatShips
    public List<GameObject> _enemyCombatans;

    public List<CivController> _friendCivs = new List<CivController>(); //{ CivController.FED };
    public List<CivController> _enemyCivs = new List<CivController>(); // { CivController.KLING, CivController.ROM, CivController.CARD };

    public void AddCombatant(GameObject combatant)
    {
        string[] nameArray = new string[3] { "CivController", "shipType", "era" };
        if (combatant.name != "Ship")
        {
            nameArray = combatant.name.Split('_');
        }
        string civName = nameArray[0];
        CivController daCiv;
        //switch (civName.ToUpper())
        //{
        //    case "FED":
        //        daCiv = CivController.FED;
        //        break;
        //    case "TERRAN":
        //        daCiv = CivController.TERRAN;
        //        break;
        //    case "ROM":
        //        daCiv = CivController.ROM;
        //        break;
        //    case "KLING":
        //        daCiv = CivController.KLING;
        //        break;
        //    case "CARD":
        //        daCiv = CivController.CARD;
        //        break;
        //    case "DOM":
        //        daCiv = CivController.DOM;
        //        break;
        //    case "BORG":
        //        daCiv = CivController.BORG;
        //        break;
        //    default:
        //        daCiv = CivController.FED;
        //        break;
        //}
        //if (_friendCivs.Contains(daCiv))
        //{
        //    _friendCivs.Add(daCiv);
        //}
        //else if (_enemyCivs.Contains(daCiv))
        //{
        //    _enemyCivs.Add(daCiv);
        //}
    }

    public List<GameObject> UpdateFriendCombatants()
    {
        return _friendCombatans;
    }
    public List<GameObject> UpdateEnemyCombatants()
    {
        return _enemyCombatans;
    }
    public List<CivController> FriendCivCombatants()
    {
        return _friendCivs;
    }
    public List<CivController> EnemyCivCombatants()
    {
        return _enemyCivs;
    }
    // do something
    /*   string[] _friendNameArray = new string[] { "FED_CRUISER_II", "FED_CRUISER_III", "FED_DESTROYER_II", "FED_DESTROYER_II",
            "FED_DESTROYER_I", "FED_SCOUT_II", "FED_SCOUT_IV" , "FED_COLONYSHIP_I" };
    FriendNameArray = _friendNameArray;
        string[] _enemyNameArray = new string[] {"KLING_DESTROYER_I", "KLING_DESTROYER_I", "KLING_CRUISER_II", "KLING_SCOUT_II", "KLING_COLONYSHIP_I","CARD_SCOUT_I",
            "ROM_CRUISER_III", "ROM_CRUISER_II", "ROM_SCOUT_III"} */

}

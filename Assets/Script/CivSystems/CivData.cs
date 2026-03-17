using BOTF3D.GamePlay;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Core
{
    public class CivData // has list of civsInGame, it's starSytems Data
    {
        public int CivInt;
        public CivEnum CivEnum;
        public int PlayerId; // network player ID, not used in single player
        public string CivShortName;
        public string CivLongName;
        public string CivHomeSystemName;
        public Vector3 HomeStarSystemPosition;
        public WarLikeEnum Warlike;// a enum scale from most work like 0 to neutral 3 and most peaceful 5
        public XenophobiaEnum Xenophbia; // XenophobiaEnum
        public RuthlessEnum Ruthelss; //XenophobiaEnum
        public GreedyEnum Greedy; //XenophobiaEnum
        public Sprite CivRaceSprite;
        public Sprite InsigniaSprite;
        public int Population = 5;
        // public int Credits = 100;
        public int TechPoints = 10; // 10 for pre warp and playable get 90 more to be tech level early at 100; 
        public TechLevel TechLevel; // all cis have tech points and the tech level enum value sets a level threshold
        public bool Playable;
        public bool PlayedByAI = true;
        public CivEnum LocalPlayerCivEnum;
        public bool HasWarp;
        public string Decription = "We are the Borg";
        public List<StarSysController> StarSysOwned;
        //public List<CivController> CivControllersWeKnow;
        //public List<CivEnum> CivEnumsWeKnow;
        //public float TaxRate; // universal or variable by civ/system??
        //public float GrowthRate; // universal or variable by civ/system??
        public float IntelPoints;

        //public void AddToCivControllersWeKnow(CivController civControllerWeFound)
        //{
        //    CivControllersWeKnow.Add(civControllerWeFound);
        //    CivEnumsWeKnow.Add(civControllerWeFound.CivData.CivEnum);
        //}
    }
}


using System.Collections.Generic;
using UnityEngine;
namespace Assets.Core
{

    public class CivData // has list of civsInGame, it's starSytems Data
    {
        public int CivInt;
        public CivEnum CivEnum;
        public string CivShortName;
        public string CivLongName;
        public string CivHomeSystemName;
        public Vector3 HomeStarSystemPosition; 
        public WarLikeEnum Warlike;// a enum scale from most worklike 0 to neutral 3 and most peasful 5
        public XenophobiaEnum Xenophbia; // XenophobiaEnum
        public RuthlessEnum Ruthelss; //XenophobiaEnum
        public GreedyEnum Greedy; //XenophobiaEnum
        public Sprite CivRaceSprite;
        public Sprite InsigniaSprite;
        public int Population = 5;

        public int TechPoints = 10; // 10 for pre warp and playable get 90 more to be tech level early at 100; 
        public TechLevel TechLevel; // all cis have tech points and the techlevel enum value sets a level threashold
        public bool Playable;
        public bool PlayedByAI = true;
        public CivEnum LocalPlayerCivEnum;
        public bool HasWarp;
        public string Decription = "We are the Borg";
        public List<StarSysController> StarSysOwned;
        public float IntelPoints;
    }
}


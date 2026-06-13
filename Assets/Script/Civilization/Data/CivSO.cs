//using UnityEditor.Build.Reporting;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Civilization
{
    [CreateAssetMenu(fileName = "CivSO", menuName = "CivSO")]
    public class CivSO : ScriptableObject
    {
        public int CivInt;
        public CivEnum CivEnum;
        public string CivShortName;
        public string CivLongName;
        public string CivHomeSystem; //best way???
        public WarLikeEnum WarLikeEnum;
        public XenophobiaEnum XenophbiaEnum;
        public RuthlessEnum RuthlessEnum;
        public GreedyEnum GreedyEnum;
        public Sprite CivImage;
        public Sprite Insignia;
        public int Population;
        public int Credits;
        public int TechPoints;
        public TechLevel CivTechLevel;
        public bool Playable;
        public bool HasWarp;
        public string Decription;
        //public List<StarSysController> StarSysOwned;
        //public List<CivController> ContactList;
        //public float TaxRate; // universal or variable by civ/sys??
        //public float GrowthRate; // universal or variable by civ/sys??
        public float IntelPoints;
        //public List<CivData> ContactList = new List<CivData>();

        [Header("Civilization Build Profile")]
        [Tooltip("Multiplier on all ship BuildDuration. >1 = slower. Borg: 2.0, Cardassian: 0.6")]
        public float shipBuildTimeMultiplier = 1.0f;

        [Tooltip("Multiplier on all facility BuildDuration (Factory, Shipyard, Research, etc.)")]
        public float facilityBuildTimeMultiplier = 1.0f;

        [Tooltip("Weapon damage multiplier applied at ship construction time")]
        public float weaponDamageMultiplier = 1.0f;

        [Tooltip("Shield health multiplier applied at ship construction time")]
        public float shieldMultiplier = 1.0f;

        [Tooltip("Hull health multiplier applied at ship construction time")]
        public float hullMultiplier = 1.0f;

        [Tooltip("Tech points awarded per captured ship (assimilation/salvage). Borg: 25, others: 10")]
        public int techPointsPerCapturedShip = 10;

        [Tooltip("Flat bonus tech points per Research Center per stardate (stacks with TechManager level multiplier)")]
        public float researchRateBonus = 0.0f;

        [Header("Intelligence")]
        [Tooltip("Intel points generated per game turn. Represents quality of the civ's intelligence service. " +
                 "Romulan (Tal Shiar)=8, Cardassian (Obsidian Order)=7, Dominion=6, Federation=5, Terran=5, Klingon=3, Borg=2.")]
        public float intelPointsPerTurn = 0f;

        [Tooltip("Tech points a successful espionage project steals from this civ. 0 = derive from EspionageTechYieldTable (recommended for minor civs).")]
        public int espionageTechYield = 0;

        [Header("Base Cruiser Stats — EARLY tech, before ShipType and TechLevel multipliers")]
        [Tooltip("Beam damage of a Cruiser at EARLY tech. All ship types derive from this × ShipTypeProfile.WeaponMult.")]
        public int baseCruiserBeamDmg = 7;

        [Tooltip("Torpedo damage of a Cruiser at EARLY tech.")]
        public int baseCruiserTorpDmg = 8;

        [Tooltip("Shield HP of a Cruiser at EARLY tech.")]
        public int baseCruiserShieldHP = 55;

        [Tooltip("Hull HP of a Cruiser at EARLY tech.")]
        public int baseCruiserHullHP = 65;

        [Tooltip("Max warp factor of a Cruiser at EARLY tech.")]
        public float baseCruiserWarpFactor = 5.0f;

        [Tooltip("Build duration (stardates) of a Cruiser at EARLY tech with 1 production unit/day baseline.")]
        public int baseCruiserBuildDuration = 20;
    }
}



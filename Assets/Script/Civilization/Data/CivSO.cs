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
        [Header("Ship Rendering")]
        [Tooltip("Shared across all of this civilization's ships. Applied at runtime via " +
            "MaterialPropertyBlock to the shared Ship_Glow material's _EmissionColor — " +
            "do not author per-ship glow colors on ShipSO.")]
        public Color GlowColor = Color.white;
        [Tooltip("Authored home-system population (e.g. a Star Trek lore figure). Leave 0 for minor " +
            "races without a canon number to instead roll a random value in [MinHomePopulation, MaxHomePopulation].")]
        public int Population;
        [Tooltip("Used only when Population is 0 (unauthored minor races) to roll a random home-system population.")]
        public int MinHomePopulation = 10;
        [Tooltip("Used only when Population is 0 (unauthored minor races) to roll a random home-system population.")]
        public int MaxHomePopulation = 30;
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
        [Tooltip("0 = pure quantity (cheap/fast/weak), 10 = pure quality (expensive/slow/powerful). Set by designer; read by CivBalanceCalculator.")]
        [Range(0, 10)]
        public int QualityScore = 5;
        //public List<CivData> ContactList = new List<CivData>();
    }
}



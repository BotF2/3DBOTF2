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
        [Tooltip("0 = pure quantity (cheap/fast/weak), 10 = pure quality (expensive/slow/powerful). Set by designer; read by CivBalanceCalculator.")]
        [Range(0, 10)]
        public int QualityScore = 5;
        //public List<CivData> ContactList = new List<CivData>();
    }
}



using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BOTF3D.Civilization;
using BOTF3D.Combat;
using BOTF3D.Core;

/// <summary>
/// Writes balanced facility counts to each playable civ's homeworld StarSysSO.
/// Run from Tools > BOTF2 > Recalculate Balance.
///
/// Ship combat stats are NO LONGER written here — they are derived at runtime
/// by ShipStatCalculator from ShipType, TechLevel, CivSO.QualityScore, and
/// per-civ flavor.  To tune ship balance, adjust those values in ShipStatCalculator.cs.
/// </summary>
public static class CivBalanceCalculator
{
    // Power load per facility type — uniform across all civs
    const int FACTORY_POWER  = 4;
    const int SHIPYARD_POWER = 4;
    const int RESEARCH_POWER = 1;
    const int SHIELD_POWER   = 2;
    const int MIN_POWER_PLANTS = 1;

    // Fallbacks used only when a civ's PowerPlantSO / OrbitalBatterySO is missing.
    const int ORBITAL_POWER_FALLBACK   = 2;
    const int POWER_PER_PLANT_FALLBACK = 20;

    // Home system layouts indexed by QualityScore 0–10.
    // (factories, shipyards, researchCenters, shieldGenerators, orbitalBatteries)
    static readonly (int f, int sy, int rc, int sg, int ob)[] Layout = new (int, int, int, int, int)[]
    {
        (4, 2, 1, 1, 11), // Q=0
        (4, 2, 1, 1, 11), // Q=1  Cardassian — max production, minimal defense quality
        (4, 3, 1, 2, 12), // Q=2
        (3, 2, 1, 2, 12), // Q=3
        (3, 2, 1, 3, 12), // Q=4  Klingon — strong production, heavy orbital cover
        (2, 2, 2, 2,  7), // Q=5  Federation / Terran — balanced
        (2, 1, 3, 3,  7), // Q=6  Romulan — research & shield focus, one shipyard
        (2, 1, 3, 3,  6), // Q=7
        (1, 1, 3, 3,  6), // Q=8  Dominion — fewer facilities, high-quality output
        (1, 1, 4, 3,  5), // Q=9
        (1, 1, 4, 4,  4), // Q=10 Borg — maximum research/shields, minimal quantity
    };

    [MenuItem("Tools/BOTF2/Recalculate Balance")]
    public static void RecalculateBalance()
    {
        List<CivSO>            allCivSOs       = LoadAll<CivSO>("t:CivSO");
        List<StarSysSO>        allStarSysSOs   = LoadAll<StarSysSO>("t:StarSysSO");
        List<PowerPlantSO>     allPowerPlants  = LoadAll<PowerPlantSO>("t:PowerPlantSO");
        List<OrbitalBatterySO> allOrbBatteries = LoadAll<OrbitalBatterySO>("t:OrbitalBatterySO");

        int homesUpdated = 0;

        foreach (CivSO civ in allCivSOs)
        {
            if (!civ.Playable) continue;

            int q      = Mathf.Clamp(civ.QualityScore, 0, 10);
            int civInt = (int)civ.CivEnum;

            PowerPlantSO   plantSO = allPowerPlants.Find(p => p.CivInt == civInt);
            OrbitalBatterySO obSO  = allOrbBatteries.Find(o => o.CivInt == civInt);

            int powerPerPlant = plantSO != null ? plantSO.PowerOutput : POWER_PER_PLANT_FALLBACK;
            int orbitalLoad   = obSO    != null ? obSO.PowerLoad      : ORBITAL_POWER_FALLBACK;

            if (plantSO == null)
                Debug.LogWarning($"[Balance] No PowerPlantSO for {civ.CivShortName} (CivInt={civInt}) — using fallback {POWER_PER_PLANT_FALLBACK}");
            if (obSO == null)
                Debug.LogWarning($"[Balance] No OrbitalBatterySO for {civ.CivShortName} (CivInt={civInt}) — using fallback {ORBITAL_POWER_FALLBACK}");

            StarSysSO home = allStarSysSOs.Find(s => s.IsHomeworld && s.FirstOwner == civ.CivEnum);
            if (home != null)
            {
                var lay = Layout[q];
                int load = lay.f  * FACTORY_POWER
                         + lay.sy * SHIPYARD_POWER
                         + lay.rc * RESEARCH_POWER
                         + lay.sg * SHIELD_POWER
                         + lay.ob * orbitalLoad;
                int pp = Mathf.Max(MIN_POWER_PLANTS, Mathf.CeilToInt(load / (float)powerPerPlant));

                home.Factories        = lay.f;
                home.Shipyards        = lay.sy;
                home.ResearchCenters  = lay.rc;
                home.ShieldGenerators = lay.sg;
                home.OrbitalBatteries = lay.ob;
                home.PowerStations    = pp;
                home.Dilitium         = pp + 1;

                EditorUtility.SetDirty(home);
                homesUpdated++;
                Debug.Log($"[Balance] {civ.CivShortName} (Q{q}): home={home.SysName} " +
                          $"F={lay.f} SY={lay.sy} RC={lay.rc} SG={lay.sg} OB={lay.ob} " +
                          $"Load={load} PP={pp} (plant={powerPerPlant}/ea ob={orbitalLoad}/ea)");
            }
            else
            {
                Debug.LogWarning($"[Balance] No homeworld found for {civ.CivShortName} (CivEnum={civ.CivEnum})");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"Updated {homesUpdated} home systems.\n\n" +
                     "Ship stats are calculated at runtime by ShipStatCalculator — " +
                     "no ShipSO assets need updating.";
        Debug.Log($"[CivBalanceCalculator] Done. {msg}");
        EditorUtility.DisplayDialog("Balance Recalculated", msg, "OK");
    }

    static List<T> LoadAll<T>(string filter) where T : Object
    {
        var list = new List<T>();
        foreach (string guid in AssetDatabase.FindAssets(filter))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) list.Add(asset);
        }
        return list;
    }
}

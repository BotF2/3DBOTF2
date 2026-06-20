using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BOTF3D.Civilization;
using BOTF3D.Combat;
using BOTF3D.Core;

/// <summary>
/// Reads QualityScore from each playable CivSO and writes balanced facility counts
/// to home StarSysSOs and combat stats to ShipSOs. Run from Tools > BOTF2 > Recalculate Balance.
/// After running, individual ShipSO values can be hand-tuned in the Inspector for lore exceptions.
/// </summary>
public static class CivBalanceCalculator
{
    // Power load per facility type — uniform across all civs
    const int FACTORY_POWER  = 4;
    const int SHIPYARD_POWER = 4;
    const int RESEARCH_POWER = 1;
    const int SHIELD_POWER   = 2;
    const int MIN_POWER_PLANTS = 1;

    // Orbital battery PowerLoad and plant PowerOutput vary per civ — read from SO assets.
    // These fallbacks are used only if a SO is missing.
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

    // Baseline ship stats at QualityScore=5 (Federation/Terran reference).
    // Key = "TYPE_TIER", e.g. "DESTROYER_II", "HVYCRUISER_IV".
    static readonly Dictionary<string, (int shield, int hull, int torpedo, int beam, int build)> Baseline =
        new Dictionary<string, (int, int, int, int, int)>
    {
        ["SCOUT_I"]       = (22, 10, 10, 10,  5),
        ["SCOUT_II"]      = (30, 14, 14, 14,  6),
        ["SCOUT_III"]     = (40, 18, 18, 18,  7),
        ["SCOUT_IV"]      = (52, 24, 24, 24,  8),
        ["DESTROYER_I"]   = (33, 15, 15, 15,  8),
        ["DESTROYER_II"]  = (45, 22, 22, 22,  9),
        ["DESTROYER_III"] = (58, 30, 30, 30, 10),
        ["DESTROYER_IV"]  = (72, 40, 40, 40, 11),
        ["CRUISER_II"]    = (55, 35, 30, 30,  9),
        ["CRUISER_III"]   = (68, 45, 40, 40, 10),
        ["LTCRUISER_IV"]  = (75, 52, 48, 48, 11),
        ["HVYCRUISER_IV"] = (88, 66, 60, 60, 12),
        ["TRANSPORT_I"]   = (15, 30,  3,  3,  5),
        ["TRANSPORT_II"]  = (18, 38,  4,  4,  6),
        ["TRANSPORT_III"] = (22, 46,  5,  5,  7),
        ["TRANSPORT_IV"]  = (26, 55,  6,  6,  8),
    };

    [MenuItem("Tools/BOTF2/Recalculate Balance")]
    public static void RecalculateBalance()
    {
        List<CivSO>            allCivSOs        = LoadAll<CivSO>("t:CivSO");
        List<StarSysSO>        allStarSysSOs    = LoadAll<StarSysSO>("t:StarSysSO");
        List<ShipSO>           allShipSOs       = LoadAll<ShipSO>("t:ShipSO");
        List<PowerPlantSO>     allPowerPlants   = LoadAll<PowerPlantSO>("t:PowerPlantSO");
        List<OrbitalBatterySO> allOrbBatteries  = LoadAll<OrbitalBatterySO>("t:OrbitalBatterySO");

        int homesUpdated = 0;
        int shipsUpdated = 0;

        foreach (CivSO civ in allCivSOs)
        {
            if (!civ.Playable) continue;

            int q      = Mathf.Clamp(civ.QualityScore, 0, 10);
            int civInt = (int)civ.CivEnum;

            // Per-civ power economics — varies by civilization
            PowerPlantSO   plantSO = allPowerPlants.Find(p => p.CivInt == civInt);
            OrbitalBatterySO obSO  = allOrbBatteries.Find(o => o.CivInt == civInt);

            int powerPerPlant = plantSO != null ? plantSO.PowerOutput : POWER_PER_PLANT_FALLBACK;
            int orbitalLoad   = obSO    != null ? obSO.PowerLoad      : ORBITAL_POWER_FALLBACK;

            if (plantSO == null)
                Debug.LogWarning($"[Balance] No PowerPlantSO for {civ.CivShortName} (CivInt={civInt}) — using fallback {POWER_PER_PLANT_FALLBACK}");
            if (obSO == null)
                Debug.LogWarning($"[Balance] No OrbitalBatterySO for {civ.CivShortName} (CivInt={civInt}) — using fallback {ORBITAL_POWER_FALLBACK}");

            // Home system
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

            // Ships — quality multiplier anchored so Q=5 → ×1.00
            float qm = QualityMult(q);
            float bm = BuildMult(q);

            foreach (ShipSO ship in allShipSOs)
            {
                if (ship.CivEnum != civ.CivEnum) continue;

                string key = ParseShipKey(ship.ShipName);
                if (key == null || !Baseline.ContainsKey(key))
                {
                    Debug.LogWarning($"[Balance] No baseline for ship: {ship.ShipName} (key={key})");
                    continue;
                }

                var b = Baseline[key];
                ship.ShieldMaxHealth = Mathf.Max(1, Mathf.RoundToInt(b.shield  * qm));
                ship.HullMaxHealth   = Mathf.Max(1, Mathf.RoundToInt(b.hull    * qm));
                ship.TorpedoDamage   = Mathf.Max(0, Mathf.RoundToInt(b.torpedo * qm));
                ship.BeamDamage      = Mathf.Max(0, Mathf.RoundToInt(b.beam    * qm));
                ship.BuildDuration   = Mathf.Max(1, Mathf.RoundToInt(b.build   * bm));

                EditorUtility.SetDirty(ship);
                shipsUpdated++;
            }
        }

        // Lore exceptions — applied after main pass so they override the calculated values
        ApplyLoreExceptions(allShipSOs);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"Updated {homesUpdated} home systems and {shipsUpdated} ships.\nLore exceptions applied.";
        Debug.Log($"[CivBalanceCalculator] Done. {msg}");
        EditorUtility.DisplayDialog("Balance Recalculated", msg, "OK");
    }

    static void ApplyLoreExceptions(List<ShipSO> allShipSOs)
    {
        // Romulan D'deridex warbird: large, powerful, slow — Q8 stats despite ROM being Q6
        ApplyShipException(allShipSOs, "ROM_HVYCRUISER_IV", "HVYCRUISER_IV", 8,
            "Romulan D'deridex warbird");

        // Jem'Hadar fighters: fast and expendable — Q5 speed/stats despite DOM being Q8
        ApplyShipException(allShipSOs, "DOM_SCOUT_I",  "SCOUT_I",  5, "Jem'Hadar fighter");
        ApplyShipException(allShipSOs, "DOM_SCOUT_II", "SCOUT_II", 5, "Jem'Hadar raider");

        // Flagship heavy cruisers: each empire's prestige ship, one tier above their baseline
        ApplyShipException(allShipSOs, "KLING_HVYCRUISER_IV", "HVYCRUISER_IV", 6, "Vor'cha-class");
        ApplyShipException(allShipSOs, "FED_HVYCRUISER_IV",   "HVYCRUISER_IV", 6, "Galaxy-class");
        ApplyShipException(allShipSOs, "CARD_HVYCRUISER_IV",  "HVYCRUISER_IV", 3, "Galor-class");
        ApplyShipException(allShipSOs, "TERRAN_HVYCRUISER_IV","HVYCRUISER_IV", 6, "ISS flagship");
    }

    static void ApplyShipException(List<ShipSO> allShipSOs, string namePrefix, string baselineKey, int exceptionQ, string label)
    {
        ShipSO ship = allShipSOs.Find(s => s.ShipName != null && s.ShipName.StartsWith(namePrefix));
        if (ship == null)
        {
            Debug.LogWarning($"[Balance] {namePrefix} not found — lore exception skipped ({label})");
            return;
        }

        float qm = QualityMult(exceptionQ);
        float bm = BuildMult(exceptionQ);
        var b = Baseline[baselineKey];

        ship.ShieldMaxHealth = Mathf.RoundToInt(b.shield  * qm);
        ship.HullMaxHealth   = Mathf.RoundToInt(b.hull    * qm);
        ship.TorpedoDamage   = Mathf.RoundToInt(b.torpedo * qm);
        ship.BeamDamage      = Mathf.RoundToInt(b.beam    * qm);
        ship.BuildDuration   = Mathf.RoundToInt(b.build   * bm);

        EditorUtility.SetDirty(ship);
        Debug.Log($"[Balance] Lore exception — {label} (Q{exceptionQ}): " +
                  $"Sh={ship.ShieldMaxHealth} Hu={ship.HullMaxHealth} " +
                  $"To={ship.TorpedoDamage} Be={ship.BeamDamage} Build={ship.BuildDuration}");
    }

    // Quality multiplier: Q=1→0.48, Q=5→1.00, Q=10→1.65
    static float QualityMult(int q) => Mathf.Lerp(0.48f, 1.65f, (q - 1) / 9f);

    // Build time multiplier: Q=1→0.44 (faster), Q=5→1.00, Q=10→1.70 (slower)
    static float BuildMult(int q) => Mathf.Lerp(0.44f, 1.70f, (q - 1) / 9f);

    // Parses "CIV_TYPE_TIER(CLONE)" → "TYPE_TIER", e.g. "ROM_HVYCRUISER_IV(CLONE)" → "HVYCRUISER_IV"
    static string ParseShipKey(string shipName)
    {
        if (string.IsNullOrEmpty(shipName)) return null;
        string name = shipName.Replace("(CLONE)", "").Trim();
        string[] parts = name.Split('_');
        if (parts.Length < 3) return null;
        string tier = parts[parts.Length - 1];
        if (tier != "I" && tier != "II" && tier != "III" && tier != "IV") return null;
        string type = string.Join("_", parts, 1, parts.Length - 2);
        return $"{type}_{tier}";
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

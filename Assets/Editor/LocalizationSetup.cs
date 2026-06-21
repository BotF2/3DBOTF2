using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using BOTF3D.UI;

namespace BOTF3D.Editor
{
    public static class LocalizationSetup
    {
        private const string Table = "StringTableCollection";

        private static readonly (string Key, string En)[] NewKeys =
        {
            ("No Destination",       "No Destination"),
            ("Stardate",             "Stardate"),
            ("Report.Combat",        "COMBAT"),
            ("Report.Diplomacy",     "DIPL"),
            ("Report.Intel",         "INTEL"),
            ("Intel.TechTheft",      "Tech Theft"),
            ("Intel.GatherIntel",    "Gather Intel"),
            ("Intel.Sabotage",       "Sabotage"),
            ("Intel.Disinformation", "Disinformation"),
            ("Dipl.War",             "War declared with {0}"),
            ("Dipl.Neutral",         "Relations with {0}: neutral"),
            ("Dipl.Peace",           "Peace accord with {0}"),
            ("Dipl.Allied",          "Alliance forged with {0}"),
            ("Dipl.Changed",         "Diplomacy changed with {0}"),
            ("Dipl.War.Detail",      "A state of war now exists between {0} and {1}. All peace treaties are void."),
            ("Dipl.Neutral.Detail",  "Relations between {0} and {1} have settled at neutral."),
            ("Dipl.Peace.Detail",    "A peace accord has been reached between {0} and {1}."),
            ("Dipl.Allied.Detail",   "An alliance has been forged between {0} and {1}. Mutual defense is now active."),
            ("Resistance is Futile", "Resistance is Futile"),
            ("Ships",                "{0} ships"),
            // Galaxy ribbon buttons
            ("Ribbon.Systems",       "Systems"),
            ("Ribbon.Fleets",        "Fleets"),
            ("Ribbon.Diplomacy",     "Diplomacy"),
            ("Ribbon.Intel",         "Intelligence"),
            ("Ribbon.Report",        "Report"),
            ("Ribbon.Encyclopedia",  "Encyclopedia"),
            ("Ribbon.Home",          "Home"),
        };

        /// <summary>
        /// Adds all new localization keys to every language table, with English as the default.
        /// Run this once, then translate non-English tables via Window > Asset Management > Localization Tables.
        /// </summary>
        [MenuItem("BOTF/Localization/Add Missing Keys")]
        public static void AddMissingKeys()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(Table);
            if (collection == null)
            {
                Debug.LogError($"[LocalizationSetup] StringTableCollection '{Table}' not found.");
                return;
            }

            int added = 0, skipped = 0;
            foreach (var (key, en) in NewKeys)
            {
                if (collection.SharedData.GetEntry(key) != null) { skipped++; continue; }

                var entry = collection.SharedData.AddKey(key);
                EditorUtility.SetDirty(collection.SharedData);

                foreach (var table in collection.StringTables)
                {
                    if (table is StringTable st)
                    {
                        st.AddEntry(entry.Id, en);
                        EditorUtility.SetDirty(st);
                    }
                }
                added++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[LocalizationSetup] Added {added} keys ({skipped} already existed). " +
                      "Translate via Window > Asset Management > Localization Tables.");
        }

        /// <summary>
        /// Scans all TMP_Text in the open scene. For each whose text matches a key in the
        /// string table, adds LocalizeStringEvent + LocalizeTMP so it auto-updates on locale change.
        /// Open each target scene, run this, then save.
        /// </summary>
        [MenuItem("BOTF/Localization/Wire TMP_Text — Current Scene")]
        public static void WireTMPText()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(Table);
            if (collection == null) { Debug.LogError("[LocalizationSetup] Collection not found."); return; }

            // Build English-value → key lookup from all known entries
            var valueToKey = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var (key, en) in NewKeys)
                valueToKey[en] = key;

            // Also pull from the English string table for keys not in NewKeys
            foreach (var table in collection.StringTables)
            {
                if (table is StringTable st && st.LocaleIdentifier.Code == "en")
                {
                    foreach (var entry in st.Values)
                    {
                        if (!string.IsNullOrEmpty(entry.Value) && entry.SharedEntry?.Key != null)
                            valueToKey.TryAdd(entry.Value, entry.SharedEntry.Key);
                    }
                    break;
                }
            }

            var tmps = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
            int wired = 0, skipped = 0;

            foreach (var tmp in tmps)
            {
                if (!IsGameScene(tmp.gameObject)) { skipped++; continue; }
                if (tmp.GetComponent<LocalizeStringEvent>() != null) { skipped++; continue; }

                string text = tmp.text?.Trim();
                if (string.IsNullOrEmpty(text) || !valueToKey.TryGetValue(text, out string key))
                { skipped++; continue; }

                Undo.RecordObject(tmp.gameObject, "Add LocalizeStringEvent");
                var lse = Undo.AddComponent<LocalizeStringEvent>(tmp.gameObject);
                lse.StringReference.SetReference(Table, key);
                Undo.AddComponent<LocalizeTMP>(tmp.gameObject);
                EditorUtility.SetDirty(tmp.gameObject);
                wired++;

                Debug.Log($"[LocalizationSetup] Wired '{tmp.gameObject.name}' → key '{key}'");
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[LocalizationSetup] Wired {wired} TMP_Text ({skipped} skipped — no match or already wired).");
        }

        private static bool IsGameScene(GameObject go)
        {
            if (!go.scene.IsValid()) return false;
            var p = go.scene.path;
            return !p.Contains("/Mirror/") && !p.Contains("/Packages/") && !p.Contains("Examples");
        }
    }
}

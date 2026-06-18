using BOTF3D.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    /// <summary>
    /// Manages civilization-specific UI display (insignias, race portraits, names).
    /// Handles loading and assigning sprites for the local player's civilization.
    /// </summary>
    public class GalaxyCivDisplayManager : IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        // UI components
        private readonly Image insigniaImage;
        private readonly Image raceImage;
        private readonly TextMeshProUGUI civShortNameText;

        // Civilization sprites
        private readonly Sprite federationInsignia;
        private readonly Sprite romulanInsignia;
        private readonly Sprite klingonInsignia;
        private readonly Sprite cardassianInsignia;
        private readonly Sprite dominionInsignia;
        private readonly Sprite borgInsignia;
        private readonly Sprite terranInsignia;

        private readonly Sprite federationRace;
        private readonly Sprite romulanRace;
        private readonly Sprite klingonRace;
        private readonly Sprite cardassianRace;
        private readonly Sprite dominionRace;
        private readonly Sprite borgRace;
        private readonly Sprite terranRace;

        public GalaxyCivDisplayManager(
            Image insigniaImage,
            Image raceImage,
            TextMeshProUGUI civShortNameText,
            Sprite federationInsignia,
            Sprite romulanInsignia,
            Sprite klingonInsignia,
            Sprite cardassianInsignia,
            Sprite dominionInsignia,
            Sprite borgInsignia,
            Sprite terranInsignia,
            Sprite federationRace,
            Sprite romulanRace,
            Sprite klingonRace,
            Sprite cardassianRace,
            Sprite dominionRace,
            Sprite borgRace,
            Sprite terranRace)
        {
            this.insigniaImage = insigniaImage;
            this.raceImage = raceImage;
            this.civShortNameText = civShortNameText;
            this.federationInsignia = federationInsignia;
            this.romulanInsignia = romulanInsignia;
            this.klingonInsignia = klingonInsignia;
            this.cardassianInsignia = cardassianInsignia;
            this.dominionInsignia = dominionInsignia;
            this.borgInsignia = borgInsignia;
            this.terranInsignia = terranInsignia;
            this.federationRace = federationRace;
            this.romulanRace = romulanRace;
            this.klingonRace = klingonRace;
            this.cardassianRace = cardassianRace;
            this.dominionRace = dominionRace;
            this.borgRace = borgRace;
            this.terranRace = terranRace;
        }

        /// <summary>
        /// Load and display UI for the local player's civilization
        /// </summary>
        public void LoadLocalPlayerCivilizationUI()
        {
            if (GameController.Instance?.GameData == null)
            {
                Debug.LogWarning("GalaxyCivDisplayManager: GameController or GameData is null");
                return;
            }

            CivEnum localPlayerCiv = GameController.Instance.GameData.LocalPlayerCivEnum;
            Debug.Log($"GalaxyCivDisplayManager: Loading UI for {localPlayerCiv}");

            string civShortName = GetCivilizationShortName(localPlayerCiv);

            // Load insignia
            Sprite insignia = GetInsigniaForCivilization(civShortName);
            if (insigniaImage != null && insignia != null)
            {
                insigniaImage.sprite = insignia;
                Debug.Log($"✅ Loaded insignia for {civShortName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not load insignia for {civShortName}");
            }

            // Load race portrait
            Sprite racePortrait = GetRacePortraitForCivilization(civShortName);
            if (raceImage != null && racePortrait != null)
            {
                raceImage.sprite = racePortrait;
                Debug.Log($"✅ Loaded race portrait for {civShortName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not load race portrait for {civShortName}");
            }

            // Set civ short name text
            if (civShortNameText != null)
            {
                civShortNameText.text = civShortName;
                Debug.Log($"✅ Set civ short name to {civShortName}");
            }
        }

        /// <summary>
        /// Get the short name for a civilization
        /// </summary>
        private string GetCivilizationShortName(CivEnum civEnum)
        {
            switch (civEnum)
            {
                case CivEnum.FED: return "FED";
                case CivEnum.ROM: return "ROM";
                case CivEnum.KLING: return "KLING";
                case CivEnum.CARD: return "CARD";
                case CivEnum.DOM: return "DOM";
                case CivEnum.BORG: return "BORG";
                case CivEnum.TERRAN: return "TERRAN";
                default:
                    Debug.LogWarning($"Unknown CivEnum: {civEnum}");
                    return "UNKNOWN";
            }
        }

        /// <summary>
        /// Get insignia sprite for a civilization
        /// </summary>
        private Sprite GetInsigniaForCivilization(string civShortName)
        {
            switch (civShortName)
            {
                case "FED": return federationInsignia;
                case "ROM": return romulanInsignia;
                case "KLING": return klingonInsignia;
                case "CARD": return cardassianInsignia;
                case "DOM": return dominionInsignia;
                case "BORG": return borgInsignia;
                case "TERRAN": return terranInsignia;
                default:
                    Debug.LogWarning($"No insignia found for {civShortName}");
                    return null;
            }
        }

        /// <summary>
        /// Get race portrait sprite for a civilization
        /// </summary>
        private Sprite GetRacePortraitForCivilization(string civShortName)
        {
            switch (civShortName)
            {
                case "FED": return federationRace;
                case "ROM": return romulanRace;
                case "KLING": return klingonRace;
                case "CARD": return cardassianRace;
                case "DOM": return dominionRace;
                case "BORG": return borgRace;
                case "TERRAN": return terranRace;
                default:
                    Debug.LogWarning($"No race portrait found for {civShortName}");
                    return null;
            }
        }
    }
}

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

            string displayName = CivManager.Instance?.GetCivControllerByCivEnum(localPlayerCiv)?.CivData?.CivShortName;
            if (string.IsNullOrEmpty(displayName))
                displayName = GetFallbackDisplayName(localPlayerCiv);

            Sprite insignia = GetInsigniaForCivilization(localPlayerCiv);
            if (insigniaImage != null && insignia != null)
                insigniaImage.sprite = insignia;
            else
                Debug.LogWarning($"GalaxyCivDisplayManager: No insignia found for {localPlayerCiv}");

            Sprite racePortrait = GetRacePortraitForCivilization(localPlayerCiv);
            if (raceImage != null && racePortrait != null)
                raceImage.sprite = racePortrait;
            else
                Debug.LogWarning($"GalaxyCivDisplayManager: No race portrait found for {localPlayerCiv}");

            if (civShortNameText != null)
                civShortNameText.text = displayName;
        }

        private string GetFallbackDisplayName(CivEnum civEnum)
        {
            switch (civEnum)
            {
                case CivEnum.FED:    return "Federation";
                case CivEnum.ROM:    return "Romulan";
                case CivEnum.KLING:  return "Klingon";
                case CivEnum.CARD:   return "Cardassian";
                case CivEnum.DOM:    return "Dominion";
                case CivEnum.BORG:   return "Borg";
                case CivEnum.TERRAN: return "Terran";
                default:             return civEnum.ToString();
            }
        }

        private Sprite GetInsigniaForCivilization(CivEnum civEnum)
        {
            switch (civEnum)
            {
                case CivEnum.FED:    return federationInsignia;
                case CivEnum.ROM:    return romulanInsignia;
                case CivEnum.KLING:  return klingonInsignia;
                case CivEnum.CARD:   return cardassianInsignia;
                case CivEnum.DOM:    return dominionInsignia;
                case CivEnum.BORG:   return borgInsignia;
                case CivEnum.TERRAN: return terranInsignia;
                default:             return null;
            }
        }

        private Sprite GetRacePortraitForCivilization(CivEnum civEnum)
        {
            switch (civEnum)
            {
                case CivEnum.FED:    return federationRace;
                case CivEnum.ROM:    return romulanRace;
                case CivEnum.KLING:  return klingonRace;
                case CivEnum.CARD:   return cardassianRace;
                case CivEnum.DOM:    return dominionRace;
                case CivEnum.BORG:   return borgRace;
                case CivEnum.TERRAN: return terranRace;
                default:             return null;
            }
        }
    }
}

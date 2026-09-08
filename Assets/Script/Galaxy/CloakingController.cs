using BOTF3D.Civilization;
using BOTF3D.Core;
using UnityEngine;

namespace BOTF3D.Galaxy
{
    /// <summary>
    /// §5b's cloak arc (TechTree_Phase2_Design.md): Romulan Basic Cloaking Field (T4) / Klingon
    /// Battle Cloak (T5) both set CivData.Effects.GalaxyMapCloak; the shared Branch E Tachyon
    /// Detection Grid (T6, TechEffectHook.CloakDetection) lets a civ see through those two grades;
    /// each cloak-owning civ's Tier-7 capstone (Near-Perfect Cloak / Adaptive Battle Cloak
    /// Refinement) sets CloakDefeatsDetection, restoring invisibility even against a civ with
    /// detection researched - the "unseen again" beat.
    ///
    /// Having the tech only makes a fleet CLOAK-CAPABLE - actually hiding also requires the player
    /// to have toggled FleetData.IsCloakActive on for that specific fleet (FleetUI_Fields.
    /// CloakToggleButton, see FleetMenuUIController.ClickCloakToggleButton), a deliberate tactical
    /// on/off choice rather than an automatic always-on state the instant the tech completes.
    ///
    /// Pure logic, no MonoBehaviour - csFogVisibilityAgent.Update() (the existing fog-of-war
    /// visibility layer §6 says this belongs alongside) calls IsFleetCloakedFromViewer once it's
    /// already determined a fleet is otherwise fog-visible.
    /// </summary>
    public static class CloakingController
    {
        /// <summary>
        /// UI-gating check: true if this fleet's civ has completed a cloak tech at all (Basic
        /// Cloaking Field / Battle Cloak) and could toggle cloak on this fleet - independent of
        /// whether it's currently toggled on. Drives whether FleetUI_Fields.CloakToggleButton shows.
        /// </summary>
        public static bool CanToggleCloak(FleetController fleet)
        {
            if (fleet?.FleetData == null) return false;
            return CivManager.Instance?.GetCivDataByCivEnum(fleet.FleetData.CivEnum)?.Effects?.GalaxyMapCloak ?? false;
        }

        /// <summary>
        /// True if this specific fleet should be hidden from viewerCiv right now - requires BOTH the
        /// owning civ's tech (GalaxyMapCloak) AND this fleet's own cloak currently toggled on
        /// (FleetData.IsCloakActive). A civ is never cloaked from itself.
        /// </summary>
        public static bool IsFleetCloakedFromViewer(FleetController fleet, CivEnum viewerCiv)
        {
            if (fleet?.FleetData == null || !fleet.FleetData.IsCloakActive) return false;

            CivEnum fleetOwnerCiv = fleet.FleetData.CivEnum;
            if (fleetOwnerCiv == viewerCiv) return false;

            CivData ownerData = CivManager.Instance?.GetCivDataByCivEnum(fleetOwnerCiv);
            if (ownerData?.Effects == null || !ownerData.Effects.GalaxyMapCloak) return false;

            // Tier-7 upgrade defeats Tachyon Detection Grid outright - "unseen again" (§5b beat 4).
            if (ownerData.Effects.CloakDefeatsDetection) return true;

            CivData viewerData = CivManager.Instance?.GetCivDataByCivEnum(viewerCiv);
            bool viewerHasDetection = viewerData?.Effects?.CloakDetection ?? false;
            return !viewerHasDetection;
        }

        // One shared Material for every cloaked fleet's insignia, lazily created the first time any
        // fleet cloaks rather than authored as a .mat asset - avoids hand-writing Unity's YAML
        // material format, and a single shared instance is all that's needed since the shader takes
        // no per-fleet parameters (see FleetController.UpdateCloakVisual, the only caller).
        private static Material grayscaleSpriteMaterial;

        /// <summary>
        /// Returns the shared grayscale sprite material (Assets/Shaders/CloakGrayscaleSprite.shader),
        /// or null if that shader can't be found (e.g. stripped from a build) - callers must treat
        /// null as "leave the sprite's material alone" rather than throw.
        /// </summary>
        public static Material GetGrayscaleSpriteMaterial()
        {
            if (grayscaleSpriteMaterial != null) return grayscaleSpriteMaterial;

            Shader shader = Shader.Find("BOTF/CloakGrayscaleSprite");
            if (shader == null)
            {
                Debug.LogWarning("CloakingController: shader 'BOTF/CloakGrayscaleSprite' not found - cloaked fleets won't show a grayscale insignia.");
                return null;
            }

            grayscaleSpriteMaterial = new Material(shader) { name = "CloakGrayscaleSprite (shared)" };
            return grayscaleSpriteMaterial;
        }
    }
}

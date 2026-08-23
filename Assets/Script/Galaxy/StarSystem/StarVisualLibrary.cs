using UnityEngine;
using BOTF3D.Core;

namespace BOTF3D.Galaxy
{
    public readonly struct StarVisualProfile
    {
        public readonly Color Color;
        public readonly float SizeMultiplier;
        public readonly float Brightness;

        public StarVisualProfile(Color color, float sizeMultiplier, float brightness)
        {
            Color = color;
            SizeMultiplier = sizeMultiplier;
            Brightness = brightness;
        }
    }

    // Per-star-type look used by the galaxy map's billboarded star sprite and its selection
    // ring (see StarSysController.ApplyStarVisual / CreateSelectionRing). Only the five star
    // color buckets get a profile - non-star GalaxyObjectTypes (Nebula, BlackHole, WormHole,
    // UniComplex, Station, ...) keep their existing sprite-only rendering untouched.
    public static class StarVisualLibrary
    {
        public static bool TryGetProfile(GalaxyObjectType starType, out StarVisualProfile profile)
        {
            switch (starType)
            {
                case GalaxyObjectType.BlueStar:
                    profile = new StarVisualProfile(new Color(0.55f, 0.75f, 1f), 1.4f, 6f);
                    return true;
                case GalaxyObjectType.WhiteStar:
                    profile = new StarVisualProfile(new Color(1f, 1f, 1f), 1.2f, 7.5f);
                    return true;
                case GalaxyObjectType.YellowStar:
                    profile = new StarVisualProfile(new Color(1f, 0.92f, 0.7f), 1f, 4f);
                    return true;
                case GalaxyObjectType.OrangeStar:
                    profile = new StarVisualProfile(new Color(1f, 0.55f, 0.25f), 0.8f, 3.5f);
                    return true;
                case GalaxyObjectType.RedStar:
                    profile = new StarVisualProfile(new Color(1f, 0.35f, 0.25f), 0.65f, 3f);
                    return true;
                default:
                    profile = default;
                    return false;
            }
        }

        // Placeholder name shown for a system before its owning civ has been discovered/contacted
        // (see StarSysManager - the real name is swapped in later by ExposeAllSystemName). Falls
        // back to "Unknown" for types with no obvious generic label (Station, TargetDestination,
        // UnknownFleet, Fleet, SomeBadAssGalacticObject, ...).
        public static string GetGenericName(GalaxyObjectType type)
        {
            switch (type)
            {
                case GalaxyObjectType.BlueStar:
                    return "Blue Star";
                case GalaxyObjectType.WhiteStar:
                    return "White Star";
                case GalaxyObjectType.YellowStar:
                    return "Yellow Star";
                case GalaxyObjectType.OrangeStar:
                    return "Orange Star";
                case GalaxyObjectType.RedStar:
                    return "Red Star";
                case GalaxyObjectType.Nebula:
                case GalaxyObjectType.OmarianNebula:
                case GalaxyObjectType.ORIONNEBULA:
                    return "Nebula";
                case GalaxyObjectType.BlackHole:
                    return "Black Hole";
                case GalaxyObjectType.WormHole:
                    return "Wormhole";
                default:
                    return "Unknown";
            }
        }
    }
}

using BOTF3D.Core;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Defines per-ShipType multipliers relative to Cruiser = 1.0 baseline.
    /// Final ship stat = CivBase × ShipTypeProfile × TechMultiplier.
    /// LtCruiser is treated as Cruiser (same multipliers, SUPREME-tier visual model).
    /// </summary>
    public static class ShipTypeProfiles
    {
        public struct Profile
        {
            public float WarpMult;
            public float WeaponMult;
            public float ShieldMult;
            public float HullMult;
            public float BuildMult;
            public int   DilithiumCost;
        }

        public static Profile Get(ShipType type)
        {
            switch (type)
            {
                case ShipType.Scout:
                    return new Profile { WarpMult=1.50f, WeaponMult=0.35f, ShieldMult=0.25f, HullMult=0.30f, BuildMult=0.35f, DilithiumCost=1 };
                case ShipType.Destroyer:
                    return new Profile { WarpMult=1.25f, WeaponMult=0.60f, ShieldMult=0.50f, HullMult=0.55f, BuildMult=0.55f, DilithiumCost=2 };
                case ShipType.Cruiser:
                case ShipType.LtCruiser:
                    return new Profile { WarpMult=1.00f, WeaponMult=1.00f, ShieldMult=1.00f, HullMult=1.00f, BuildMult=1.00f, DilithiumCost=3 };
                case ShipType.HvyCruiser:
                    return new Profile { WarpMult=0.70f, WeaponMult=1.55f, ShieldMult=1.50f, HullMult=1.60f, BuildMult=1.75f, DilithiumCost=5 };
                case ShipType.Transport:
                    return new Profile { WarpMult=0.55f, WeaponMult=0.08f, ShieldMult=0.55f, HullMult=0.65f, BuildMult=0.60f, DilithiumCost=2 };
                default:
                    return new Profile { WarpMult=1.00f, WeaponMult=1.00f, ShieldMult=1.00f, HullMult=1.00f, BuildMult=1.00f, DilithiumCost=3 };
            }
        }

        /// <summary>
        /// Shipyard gate: which ship types can be built at a given tech level.
        /// EARLY=Scout/Destroyer/Transport. DEVELOPED+ADVANCED=+Cruiser. SUPREME=Cruiser→LtCruiser, +HvyCruiser.
        /// </summary>
        public static bool IsAvailableAtTechLevel(ShipType type, TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.EARLY:
                    return type == ShipType.Scout || type == ShipType.Destroyer || type == ShipType.Transport;
                case TechLevel.DEVELOPED:
                case TechLevel.ADVANCED:
                    return type == ShipType.Scout    || type == ShipType.Destroyer ||
                           type == ShipType.Cruiser  || type == ShipType.Transport;
                case TechLevel.SUPREME:
                    return type == ShipType.Scout    || type == ShipType.Destroyer  ||
                           type == ShipType.LtCruiser || type == ShipType.HvyCruiser ||
                           type == ShipType.Transport;
                default:
                    return false;
            }
        }
    }
}

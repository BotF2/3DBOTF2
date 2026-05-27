using BOTF3D.Core;
using System.Collections.Generic;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// Provides weapon and audio assets based on civilization.
    /// Handles prefab and audio clip assignment for both sides of combat.
    /// </summary>
    public class WeaponAssetProvider
    {
        // Weapon prefab lists
        private readonly List<GameObject> torpedoPrefabs;
        private readonly List<GameObject> beamPrefabs;

        // Audio clip arrays
        private readonly AudioClip[] beamFireClips;
        private readonly AudioClip[] torpedoFireClips;

        // Current combat assignments (cached)
        public GameObject SideOneTorpedoPrefab { get; private set; }
        public GameObject SideTwoTorpedoPrefab { get; private set; }
        public GameObject SideOneBeamPrefab { get; private set; }
        public GameObject SideTwoBeamPrefab { get; private set; }
        public AudioClip SideOneBeamFireClip { get; private set; }
        public AudioClip SideTwoBeamFireClip { get; private set; }
        public AudioClip SideOneTorpedoFireClip { get; private set; }
        public AudioClip SideTwoTorpedoFireClip { get; private set; }

        public WeaponAssetProvider(
            List<GameObject> torpedos,
            List<GameObject> beams,
            AudioClip[] beamClips,
            AudioClip[] torpedoClips)
        {
            torpedoPrefabs = torpedos;
            beamPrefabs = beams;
            beamFireClips = beamClips;
            torpedoFireClips = torpedoClips;
        }

        /// <summary>
        /// Assign weapon assets for a combat between two civilizations
        /// </summary>
        public void AssignWeaponsForCombat(CivEnum sideOneCiv, CivEnum sideTwoCiv)
        {
            // Assign torpedo prefabs
            SideOneTorpedoPrefab = GetTorpedoPrefab(sideOneCiv);
            SideTwoTorpedoPrefab = GetTorpedoPrefab(sideTwoCiv);

            // Assign beam prefabs
            SideOneBeamPrefab = GetBeamPrefab(sideOneCiv);
            SideTwoBeamPrefab = GetBeamPrefab(sideTwoCiv);

            // Assign audio clips
            SideOneBeamFireClip = GetBeamFireClip(sideOneCiv);
            SideTwoBeamFireClip = GetBeamFireClip(sideTwoCiv);
            SideOneTorpedoFireClip = GetTorpedoFireClip(sideOneCiv);
            SideTwoTorpedoFireClip = GetTorpedoFireClip(sideTwoCiv);

            Debug.Log($"✅ Weapon assets assigned for {sideOneCiv} vs {sideTwoCiv}");
        }

        /// <summary>
        /// Get torpedo prefab for a civilization
        /// </summary>
        private GameObject GetTorpedoPrefab(CivEnum civEnum)
        {
            int civIndex = (int)civEnum;

            if (torpedoPrefabs == null || torpedoPrefabs.Count == 0)
            {
                Debug.LogError("WeaponAssetProvider: torpedoPrefabs list is null or empty!");
                return null;
            }

            // Use specific civ's prefab if available
            if (civIndex >= 0 && civIndex < torpedoPrefabs.Count)
            {
                return torpedoPrefabs[civIndex];
            }

            // Fallback to last prefab (minor civ default)
            return torpedoPrefabs[torpedoPrefabs.Count - 1];
        }

        /// <summary>
        /// Get beam prefab for a civilization
        /// </summary>
        private GameObject GetBeamPrefab(CivEnum civEnum)
        {
            int civIndex = (int)civEnum;

            if (beamPrefabs == null || beamPrefabs.Count == 0)
            {
                Debug.LogError("WeaponAssetProvider: beamPrefabs list is null or empty!");
                return null;
            }

            // Use specific civ's prefab if available
            if (civIndex >= 0 && civIndex < beamPrefabs.Count)
            {
                return beamPrefabs[civIndex];
            }

            // Fallback to last prefab (minor civ default)
            return beamPrefabs[beamPrefabs.Count - 1];
        }

        /// <summary>
        /// Get beam fire audio clip for a civilization
        /// For 7 playable civs (FED=0 through TERRAN=6), use their index
        /// For all other civs (index > 7), use the last clip (minor civ fallback)
        /// </summary>
        private AudioClip GetBeamFireClip(CivEnum civEnum)
        {
            if (beamFireClips == null || beamFireClips.Length == 0)
            {
                Debug.LogWarning($"⚠️ BeamFireClips array is null or empty!");
                return null;
            }

            int civIndex = (int)civEnum;

            // For playable civs (0-6), use their specific clip
            if (civIndex >= 0 && civIndex < beamFireClips.Length - 1)
            {
                AudioClip clip = beamFireClips[civIndex];
                Debug.Log($"✅ Assigned BeamFireClip for {civEnum} (index {civIndex}): {clip?.name ?? "NULL"}");
                return clip;
            }

            // For minor civs (index > 7), use the last clip as fallback
            AudioClip fallbackClip = beamFireClips[beamFireClips.Length - 1];
            Debug.Log($"✅ Assigned fallback BeamFireClip for {civEnum} (index {civIndex}): {fallbackClip?.name ?? "NULL"}");
            return fallbackClip;
        }

        /// <summary>
        /// Get torpedo fire audio clip for a civilization
        /// For 7 playable civs (FED=0 through TERRAN=6), use their index
        /// For all other civs (index > 7), use the last clip (minor civ fallback)
        /// </summary>
        private AudioClip GetTorpedoFireClip(CivEnum civEnum)
        {
            if (torpedoFireClips == null || torpedoFireClips.Length == 0)
            {
                Debug.LogWarning($"⚠️ TorpedoFireClips array is null or empty!");
                return null;
            }

            int civIndex = (int)civEnum;

            // For playable civs (0-6), use their specific clip
            if (civIndex >= 0 && civIndex < torpedoFireClips.Length - 1)
            {
                AudioClip clip = torpedoFireClips[civIndex];
                Debug.Log($"✅ Assigned TorpedoFireClip for {civEnum} (index {civIndex}): {clip?.name ?? "NULL"}");
                return clip;
            }

            // For minor civs (index > 7), use the last clip as fallback
            AudioClip fallbackClip = torpedoFireClips[torpedoFireClips.Length - 1];
            Debug.Log($"✅ Assigned fallback TorpedoFireClip for {civEnum} (index {civIndex}): {fallbackClip?.name ?? "NULL"}");
            return fallbackClip;
        }
    }
}

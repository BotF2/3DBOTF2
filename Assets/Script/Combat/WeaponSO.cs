using BOTF3D.Audio;
using BOTF3D.Core;
using UnityEngine;

namespace BOTF3D.Combat
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon", order = 1)]
    public class WeaponSO : ScriptableObject
    {
        [Header("Weapon Identity")]
        public string weaponName;
        public CivEnum ownerCiv;

        [Header("Weapon Stats")]
        public float damage;
        public float fireRate;
        public GameObject projectilePrefab; // Torpedo prefab

        [Header("Audio (drag SoundData assets here)")]
        [Tooltip("Sound played when weapon fires")]
        public SoundData fireSound;

        [Tooltip("Sound played when projectile hits target")]
        public SoundData impactSound;

        [Tooltip("Looping sound that follows torpedo during travel")]
        public SoundData travelLoopSound; // For torpedoes
    }
}
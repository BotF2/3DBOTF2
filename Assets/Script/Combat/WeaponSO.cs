using BOTF3D.Audio;
using BOTF3D.Core;
using UnityEngine;


public class WeaponSO : ScriptableObject
{
    [Header("Weapon Identity")]
    public string weaponName;
    public CivEnum ownerCiv;

    [Header("Weapon Stats")]
    public float damage;
    public float fireRate;
    public GameObject projectilePrefab; // Torpedo prefab

    [Header("Audio")]
    public SoundData fireSound;
    public SoundData impactSound;
    public SoundData travelLoopSound; // For torpedoes
}
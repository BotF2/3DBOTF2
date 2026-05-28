using BOTF3D.Audio;

using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Beam weapon with distance-based damage falloff.
    /// Initialized with damage and sounds from the firing ship.
    /// No longer uses WeaponSO - uses ship's BeamDamage and audio clips.
    /// </summary>
    public class BeamWeapon : MonoBehaviour
    {
        [Header("Beam Rendering")]
        public LineRenderer LineRenderer;
        public Transform TargetTransform;
        public Transform WeaponTransform;
        [SerializeField] private Transform[] _weaponAndTargetTrans = new Transform[2];

        [Header("Weapon Data (Set by Ship)")]
        private ShipController ownerShip;
        private int beamDamage;
        private AudioClip beamFireSound;
        private AudioClip beamImpactSound;

        // ✅ Distance-based damage settings
        [Header("Distance-Based Damage")]
        [Tooltip("Distance at which beam does full damage")]
        [SerializeField] private float minEffectiveRange = 50f;

        [Tooltip("Maximum range - damage drops to minimum beyond this")]
        [SerializeField] private float maxEffectiveRange = 300f;

        [Tooltip("Minimum damage multiplier at max range (0.2 = 20% damage)")]
        [SerializeField] private float minDamageMultiplier = 0.2f;
        private void Start()
        {
            LineRenderer = GetComponent<LineRenderer>();
            if (LineRenderer == null)
            {
                Debug.LogError("BeamWeapon: LineRenderer component not found!");
                enabled = false;
                return;
            }
        }
        /// <summary>
        /// Set weapon and target transforms for beam rendering
        /// </summary>
        public void SetWeaponAndTarget(Transform weapon, Transform target)
        {
            TargetTransform = target;
            WeaponTransform = weapon;
            _weaponAndTargetTrans[0] = WeaponTransform;
            _weaponAndTargetTrans[1] = TargetTransform;
        }

        private void Update()
        {
            if (LineRenderer == null || _weaponAndTargetTrans[0] == null || _weaponAndTargetTrans[1] == null)
            {
                Destroy(gameObject);
                return;
            }

            // Update beam line renderer positions
            LineRenderer.positionCount = 2;
            LineRenderer.SetPosition(0, _weaponAndTargetTrans[0].position);
            LineRenderer.SetPosition(1, _weaponAndTargetTrans[1].position);
        }
        /// <summary>
        /// Initialize beam weapon with damage and sounds from the firing ship.
        /// Call this immediately after instantiating the beam weapon prefab.
        /// </summary>
        /// <param name="ship">The ship firing this beam</param>
        /// <param name="damage">Beam damage from ShipData.BeamDamage</param>
        /// <param name="fireSound">Fire sound from ShipController.clipBeamFire</param>
        /// <param name="impactSound">Optional impact sound</param>
        public void Initialize(ShipController ship, int damage, AudioClip fireSound, AudioClip impactSound = null)
        {
            ownerShip = ship;
            beamDamage = damage;
            beamFireSound = fireSound;
            beamImpactSound = impactSound;

            Debug.Log($"🔫 BeamWeapon initialized: damage={damage}, hasFireSound={fireSound != null}");
        }

        /// <summary>
        /// Fire beam at target with distance-based damage reduction.
        /// Damage = baseDamage × falloff (1.0 at close range, 0.2 at max range).
        /// </summary>
        public void Fire(ShipController targetShip)
        {
            if (targetShip == null || WeaponTransform == null || TargetTransform == null)
            {
                Debug.LogWarning("BeamWeapon.Fire: Missing target or transform!");
                return;
            }

            // ✅ Play fire sound at weapon position (3D spatial audio)
            if (beamFireSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX3DClip(beamFireSound, WeaponTransform.position);
            }

            // ✅ Calculate distance between weapon and target
            float distance = Vector3.Distance(WeaponTransform.position, TargetTransform.position);

            // ✅ Calculate damage falloff based on distance
            float damageFalloff = CalculateDistanceFalloff(distance);

            // ✅ Calculate final damage (int) after applying falloff
            int actualDamage = Mathf.RoundToInt(beamDamage * damageFalloff);

            Debug.Log($"🔫 Beam fired: distance={distance:F0}u, baseDamage={beamDamage}, falloff={damageFalloff:F2}, actualDamage={actualDamage}");

            // ✅ Apply damage to target
            targetShip.TakeDamage(actualDamage);

            // ✅ Play impact sound at hit location (optional)
            if (beamImpactSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX3DClip(beamImpactSound, TargetTransform.position);
            }
        }

        /// <summary>
        /// Calculate damage falloff based on distance.
        /// Returns 1.0 (100%) at close range, down to minDamageMultiplier (20%) at max range.
        /// Uses linear interpolation between min and max effective ranges.
        /// </summary>
        private float CalculateDistanceFalloff(float distance)
        {
            if (distance <= minEffectiveRange)
            {
                // ✅ Full damage at close range
                return 1.0f;
            }
            else if (distance >= maxEffectiveRange)
            {
                // ✅ Minimum damage beyond max range
                return minDamageMultiplier;
            }
            else
            {
                // ✅ Linear falloff between min and max range
                // Example: At midpoint, damage = (1.0 + 0.2) / 2 = 0.6 (60%)
                float rangeFraction = (distance - minEffectiveRange) / (maxEffectiveRange - minEffectiveRange);
                return Mathf.Lerp(1.0f, minDamageMultiplier, rangeFraction);
            }
        }

        /// <summary>
        /// Legacy method - kept for backward compatibility
        /// </summary>
        public void OnHit(Vector3 hitPosition)
        {
            // Now handled in Fire() method
        }
    }
}

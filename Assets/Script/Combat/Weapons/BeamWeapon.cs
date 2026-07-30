using BOTF3D.Audio;
using BOTF3D.Core;

using UnityEngine;



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

        [Header("Camera-Distance Width Scaling")]
        [Tooltip("Beam width in world units when camera is at its closest (matches ShipCombatCameraController.MinimumCameraDistance)")]
        [SerializeField] private float minBeamWidth = 0.02f;
        [Tooltip("Beam width in world units when camera is far away (large fleet engagements)")]
        [SerializeField] private float maxBeamWidth = 0.15f;
        [Tooltip("Camera distance that produces minimum beam width — set equal to ShipCombatCameraController.MinimumCameraDistance (200)")]
        [SerializeField] private float minCameraDistance = 200f;
        [Tooltip("Camera distance that produces maximum beam width — covers 13-15 ship battles (~800 units)")]
        [SerializeField] private float maxCameraDistance = 800f;

        private Camera _combatCamera;

        [Header("Weapon Data (Set by Ship)")]
        private ShipController ownerShip;
        private int beamDamage;
        private AudioClip beamFireSound;
        private AudioClip beamImpactSound;

        // ✅ Distance-based damage settings
        // Flattened from the original (50/300/0.2/500/0.5) — under Engage, ships fly past each
        // other rather than holding at range (ShipMovementController.cs), so combat's 400u start
        // distance and the pass-through dynamic meant most of a fight happened deep in the falloff
        // zone. Wider full-damage band and a higher floor mean beam-reliant kits realize more of
        // their stat advantage across a whole turn instead of only during the brief crossing
        // window. Applies identically to all civs (see per-civ *_BEAM.prefab overrides, which all
        // mirror these same values — the C# defaults here are just the fallback for a beam that
        // isn't spawned from one of those prefabs).
        [Header("Distance-Based Damage")]
        [Tooltip("Distance at which beam does full damage")]
        [SerializeField] private float minEffectiveRange = 100f;

        [Tooltip("Maximum range - damage drops to minimum beyond this")]
        [SerializeField] private float maxEffectiveRange = 400f;

        [Tooltip("Minimum damage multiplier at max range (0.4 = 40% damage)")]
        [SerializeField] private float minDamageMultiplier = 0.4f;

        [Tooltip("Distance beyond which an additional long-range penalty is applied (aligns with TorpedoMaxRange — Formation/Retreat engagement distance)")]
        [SerializeField] private float longRangeThreshold = 600f;
        [Tooltip("Multiplier applied on top of base falloff beyond longRangeThreshold (0.65 = 65% of already-reduced damage gets through)")]
        [SerializeField] private float longRangePenalty = 0.65f;
        private void Start()
        {
            LineRenderer = GetComponent<LineRenderer>();
            if (LineRenderer == null)
            {
                GameLogger.LogError(GameLogger.LogCategory.Combat, "BeamWeapon: LineRenderer component not found!", this);
                enabled = false;
                return;
            }
            // Camera is resolved lazily in Update so it works even if the
            // beam spawns before ShipCombatCameraController.Instance is ready.
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

            // Lazily resolve the combat camera — avoids Camera.main which is null
            // when the combat camera child is not tagged MainCamera.
            if (_combatCamera == null)
            {
                if (ShipCombatCameraController.Instance != null)
                    _combatCamera = ShipCombatCameraController.Instance.GetComponentInChildren<Camera>();
                if (_combatCamera == null)
                    _combatCamera = Camera.main; // last-resort fallback
            }

            // Scale beam width by camera distance: thin when close, thicker when far
            if (_combatCamera != null)
            {
                Vector3 midpoint = (_weaponAndTargetTrans[0].position + _weaponAndTargetTrans[1].position) * 0.5f;
                float camDist = Vector3.Distance(_combatCamera.transform.position, midpoint);
                float t = Mathf.InverseLerp(minCameraDistance, maxCameraDistance, camDist);
                LineRenderer.widthMultiplier = Mathf.Lerp(minBeamWidth, maxBeamWidth, t);
            }
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

            GameLogger.Log(GameLogger.LogCategory.Combat, $"🔫 BeamWeapon initialized: damage={damage}, hasFireSound={fireSound != null}", this);
        }

        /// <summary>
        /// Fire beam at target with distance-based damage reduction.
        /// Damage = baseDamage × falloff (1.0 at close range, 0.2 at max range).
        /// </summary>
        public void Fire(ShipController targetShip)
        {
            if (targetShip == null || WeaponTransform == null || TargetTransform == null)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.Combat, "BeamWeapon.Fire: Missing target or transform!", this);
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

            // ✅ Calculate final damage (int) after applying falloff, then a small symmetric random
            // roll (see CombatDamageRandomizer) so evenly matched fights aren't fully deterministic.
            int actualDamage = CombatDamageRandomizer.ApplyVariance(Mathf.RoundToInt(beamDamage * damageFalloff));

            GameLogger.Log(GameLogger.LogCategory.Combat, $"🔫 {ownerShip?.ShipData?.CivEnum} {ownerShip?.ShipData?.ShipName} beam → {targetShip.ShipData.ShipName}: " +
                      $"distance={distance:F0}u, baseDamage={beamDamage}, falloff={damageFalloff:F2}, actualDamage={actualDamage}", this);

            // ✅ Apply damage to target
            bool wasAliveBeforeHit = !targetShip.ShipData.Distroyed;
            targetShip.TakeDamage(actualDamage);
            bool destroyedByThisHit = wasAliveBeforeHit && targetShip.ShipData.Distroyed;

            BOTF3D.Combat.Testing.CombatShotLog.LogShot(ownerShip, targetShip, "Beam", actualDamage, distance, destroyedByThisHit);

            // ✅ Play impact sound at hit location (optional)
            if (beamImpactSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX3DClip(beamImpactSound, TargetTransform.position);
            }
        }

        /// <summary>
        /// Calculate damage falloff based on distance.
        /// Returns 1.0 (100%) at close range, down to minDamageMultiplier (40%) at max range.
        /// Beyond longRangeThreshold (~600u), an additional longRangePenalty (0.65x) is applied
        /// for the Formation-vs-Retreat engagement range and other extreme-separation cases.
        /// </summary>
        private float CalculateDistanceFalloff(float distance)
        {
            float baseFalloff;

            if (distance <= minEffectiveRange)
            {
                // Full damage at close range
                baseFalloff = 1.0f;
            }
            else if (distance >= maxEffectiveRange)
            {
                // Minimum damage beyond max range
                baseFalloff = minDamageMultiplier;
            }
            else
            {
                // Linear falloff between min and max range
                float rangeFraction = (distance - minEffectiveRange) / (maxEffectiveRange - minEffectiveRange);
                baseFalloff = Mathf.Lerp(1.0f, minDamageMultiplier, rangeFraction);
            }

            // Additional penalty at long range (Formation-vs-Retreat zone: ~500-700+ units)
            if (distance >= longRangeThreshold)
            {
                baseFalloff *= longRangePenalty;
            }

            return baseFalloff;
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

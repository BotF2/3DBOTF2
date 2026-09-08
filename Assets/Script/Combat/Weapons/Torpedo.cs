using BOTF3D.Audio;
using BOTF3D.Civilization;
using BOTF3D.Core;
using BOTF3D.UI;
using System.Collections;
using UnityEngine;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Homing torpedo that always tracks its assigned target.
    /// Damage, velocity, and sounds are set via Initialize() from the firing ship.
    /// </summary>
    public class Torpedo : MonoBehaviour
    {
        [Header("Torpedo Movement")]
        [Tooltip("Torpedo flight speed (units per second)")]
        public float Velocity = 100f;

        [Tooltip("How fast torpedo can turn to track target")]
        public float TurnRatio = 10f;

        public Transform Target;
        public Rigidbody torpedoRigidbody;

        [Header("Torpedo Identity")]
        public CivEnum OwnerCivEnum;
        public ShipController OwnerShip;

        [Header("Damage (Set by Ship)")]
        public int TorpedoDamage;

        private AudioClip torpedoFireSound;
        private AudioClip torpedoImpactSound;

        private float distanceTraveled = 0f;
        private const float MAX_TRAVEL_DISTANCE = 2000f;

        private void Awake()
        {
            torpedoRigidbody = GetComponent<Rigidbody>();
            if (torpedoRigidbody != null)
            {
                torpedoRigidbody.isKinematic = true;
                torpedoRigidbody.useGravity = false;
            }
        }

        /// <summary>
        /// Initialize torpedo with damage, sounds, and velocity from the firing ship.
        /// Must be called immediately after Instantiate.
        /// </summary>
        public void Initialize(int damage, AudioClip fireSound, AudioClip impactSound = null, float velocity = 150f)
        {
            TorpedoDamage = damage;
            Velocity = velocity;
            torpedoFireSound = fireSound;
            torpedoImpactSound = impactSound;
        }

        private void Start()
        {
            if (Target == null)
            {
                Destroy(gameObject);
                return;
            }

            // Flip model if it's Federation (Blender mesh correction)
            // FBX models often face backwards (-Z) so we rotate the mesh child 180 degrees
            // Only flip if it's NOT a billboarded sprite
            if (OwnerCivEnum == CivEnum.FED && GetComponent<BOTF3D.Core.Billboard>() == null)
            {
                foreach (Transform child in transform)
                {
                    // Find the mesh model child - usually has a renderer or specific naming
                    if (child.name.Contains("_Model") || child.name.Contains("Mesh") || (child.GetComponent<Renderer>() != null && !child.name.Contains("Sprit")))
                    {
                        child.localRotation *= Quaternion.Euler(0, 180, 0);
                        Debug.Log($"✅ Flipped Federation torpedo model 180° on {gameObject.name} (Owner: {OwnerCivEnum})");
                        break;
                    }
                }
            }

            if (torpedoFireSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX3DClip(torpedoFireSound, transform.position);

            Debug.Log($"🚀 {OwnerCivEnum} torpedo launched → {Target.name}, velocity={Velocity}, damage={TorpedoDamage}");
        }

        private void Update()
        {
            if (Target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 currentPosition = transform.position;
            Vector3 targetPosition = Target.position;
            Vector3 direction = (targetPosition - currentPosition).normalized;

            // Move toward target
            float speedThisFrame = Velocity * Time.unscaledDeltaTime;
            Vector3 newPosition = Vector3.MoveTowards(currentPosition, targetPosition, speedThisFrame);
            distanceTraveled += Vector3.Distance(currentPosition, newPosition);
            transform.position = newPosition;

            // Safety: self-destruct if torpedo somehow travels too far without hitting
            if (distanceTraveled >= MAX_TRAVEL_DISTANCE)
            {
                Destroy(gameObject);
                return;
            }

            // Rotate to face travel direction
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    TurnRatio * 100f * Time.unscaledDeltaTime
                );
            }

            // Hit detection: within 2 units of target
            if (Vector3.Distance(newPosition, targetPosition) < 2f)
            {
                OnReachedTarget();
            }
        }

        private void OnReachedTarget()
        {
            // Red torpedo hit explosion — spawn immediately so it's visible before ship destruction
            GameObject hitFX = ShipManager.Instance?.torpedoHitExplosionPrefab
                               ?? ShipManager.Instance?.explosionPrefab;
            if (hitFX != null)
                Instantiate(hitFX, transform.position, Quaternion.identity);

            if (torpedoImpactSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX3DClip(torpedoImpactSound, transform.position);

            ShipController targetShip = Target.GetComponentInParent<ShipController>();
            if (targetShip != null && OwnerCivEnum != targetShip.ShipData.CivEnum)
            {
                // Rolled once here (not inside ApplyDamageAndLog) so the delayed and immediate paths
                // both apply/log the same value - see CombatDamageRandomizer for why this exists.
                int actualDamage = CombatDamageRandomizer.ApplyVariance(TorpedoDamage);

                // Delay damage so the red explosion is visible for a moment before
                // a potential blue ship-destruction explosion appears on top of it
                var cc = CombatUIManager.Instance?.CurrentCombatController;
                if (cc != null)
                    cc.StartCoroutine(ApplyDamageAfterDelay(OwnerShip, targetShip, actualDamage, distanceTraveled, 0.15f));
                else
                    ApplyDamageAndLog(OwnerShip, targetShip, actualDamage, distanceTraveled);
            }

            Destroy(gameObject);
        }

        private static IEnumerator ApplyDamageAfterDelay(ShipController owner, ShipController target, int damage, float distance, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (target != null && target.ShipData != null && !target.ShipData.Distroyed)
                ApplyDamageAndLog(owner, target, damage, distance);
        }

        private static void ApplyDamageAndLog(ShipController owner, ShipController target, int damage, float distance)
        {
            // Phase II tech tree (§8 II.3) - Ordnance-class unlocks. No separate torpedo-class
            // projectile system exists (single TorpedoDamage stat only - see TechEffects' own
            // comment), so Quantum-class adds flat bonus damage on this hit, Transphasic-class adds a
            // chance to bypass shields entirely, and Plasma-class schedules a damage-over-time tick
            // after impact (PlasmaBurnTicks below). Photon-class (the Tier-2 baseline unlock) carries
            // no extra mechanic beyond the shared WeaponDamageMultiplier curve.
            TechEffects fx = owner?.ShipData != null
                ? CivManager.Instance?.GetCivDataByCivEnum(owner.ShipData.CivEnum)?.Effects
                : null;

            if (fx != null && fx.QuantumTorpedoes)
                damage = Mathf.RoundToInt(damage * 1.25f);

            bool bypassShields = fx != null && fx.TransphasicTorpedoes && UnityEngine.Random.value < 0.35f;

            // Klingon/Terran/Dominion/Romulan attacker-side bonuses - same shared step BeamWeapon uses.
            damage = CombatOrderHelper.ApplyAttackerTechBonuses(owner, damage, out bool crippled);
            bypassShields |= crippled;

            bool wasAliveBeforeHit = !target.ShipData.Distroyed;
            target.TakeDamage(damage, bypassShields);
            bool destroyedByThisHit = wasAliveBeforeHit && target.ShipData.Distroyed;

            BOTF3D.Combat.Testing.CombatShotLog.LogShot(owner, target, "Torpedo", damage, distance, destroyedByThisHit);

            if (fx != null && fx.PlasmaTorpedoes && !destroyedByThisHit)
            {
                var cc = CombatUIManager.Instance?.CurrentCombatController;
                if (cc != null)
                    cc.StartCoroutine(PlasmaBurnTicks(target, Mathf.Max(1, Mathf.RoundToInt(damage * 0.15f))));
            }
        }

        /// <summary>Plasma-class torpedo damage-over-time: 3 ticks, 1 real second apart, stopping
        /// early if the target dies or combat ends around it.</summary>
        private static IEnumerator PlasmaBurnTicks(ShipController target, int tickDamage)
        {
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSecondsRealtime(1f);
                if (target == null || target.ShipData == null || target.ShipData.Distroyed) yield break;
                target.TakeDamage(tickDamage);
            }
        }
    }
}

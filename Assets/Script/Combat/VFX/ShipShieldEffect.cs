using BOTF3D.Core;
using System.Collections;
using UnityEngine;

namespace BOTF3D.Combat
{
    /// <summary>
    /// A brief "shields absorbed a hit" flash: a translucent egg-shaped shell around the ship,
    /// visible only for a fraction of a second per hit. Built entirely at runtime from Unity's
    /// built-in Sphere primitive rather than a prefab, so it works for every ship class/model
    /// without per-prefab wiring, and unlike VFXController's Instantiate/Destroy-per-effect
    /// pattern, one shell is built lazily on a ship's first shielded hit and then reused (just
    /// toggled + re-flashed) for every hit after that - cheaper for an effect that can repeat
    /// many times per ship in a single battle.
    ///
    /// Disabled (SetActive(false)) whenever it isn't actively flashing, so an unhit ship's shield
    /// costs zero draw calls, and all ships share one Material (per-ship brightness is set via
    /// MaterialPropertyBlock) so the SRP batcher isn't broken by having many unique materials.
    /// </summary>
    public class ShipShieldEffect : MonoBehaviour
    {
        private static Material sharedShieldMaterial;
        private static readonly int FlashIntensityID = Shader.PropertyToID("_FlashIntensity");

        private MeshRenderer shieldRenderer;
        private MaterialPropertyBlock propBlock;
        private Coroutine flashRoutine;

        private const float FlashDuration = 0.35f;
        private const float PeakIntensity = 1.5f;
        // The shell is scaled to the ship's own render bounds plus this padding, so it sits just
        // outside the hull instead of clipping through it.
        private const float PaddingMultiplier = 1.15f;
        // Mild extra stretch along the ship's forward axis (local Z), on top of its own length, so
        // the shell reads as an egg rather than a perfectly round sphere.
        private const float ForwardStretchMultiplier = 1.15f;

        /// <summary>
        /// Returns this ship's shield effect, building it on first use. Safe to call every hit -
        /// after the first call it just finds the existing child component.
        /// </summary>
        public static ShipShieldEffect GetOrCreate(ShipController ship)
        {
            if (ship == null) return null;

            var existing = ship.GetComponentInChildren<ShipShieldEffect>(true);
            if (existing != null) return existing;

            if (sharedShieldMaterial == null)
            {
                // Loaded from Assets/Resources/Materials/Mat_ShipShield_Shared.mat so its color/
                // _FresnelPower/_BaseGlow can be tuned in the Inspector like any other material,
                // instead of being an in-memory-only instance nobody could find or edit.
                sharedShieldMaterial = Resources.Load<Material>("Materials/Mat_ShipShield_Shared");
                if (sharedShieldMaterial == null)
                {
                    Shader shader = Shader.Find("BOTF3D/ShipShield");
                    if (shader == null)
                    {
                        GameLogger.Log(GameLogger.LogCategory.Combat, "ShipShieldEffect: 'BOTF3D/ShipShield' shader not found.", ship);
                        return null;
                    }
                    GameLogger.Log(GameLogger.LogCategory.Combat, "ShipShieldEffect: Mat_ShipShield_Shared.mat not found under Resources/Materials - falling back to an in-memory material.", ship);
                    sharedShieldMaterial = new Material(shader) { name = "Mat_ShipShield_Shared" };
                }
            }

            // Combined bounds of every renderer under the ship (hull, turrets, nacelles, etc.) so
            // the shell fits any ship model/class without per-prefab tuning.
            Renderer[] renderers = ship.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return null;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            GameObject shellGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shellGO.name = "ShieldShell";
            Destroy(shellGO.GetComponent<Collider>()); // purely visual, never needs physics/raycasts

            Transform shipTransform = ship.transform;
            shellGO.transform.SetParent(shipTransform, false);
            shellGO.transform.position = bounds.center;
            shellGO.transform.localRotation = Quaternion.identity;

            // Ships only rotate in ±90°-Y steps in combat (see CLAUDE.md), so converting the
            // world-space AABB extents into the ship's local axes here is exact, not approximate.
            Vector3 localExtents = shipTransform.InverseTransformVector(bounds.extents);
            float radiusX = Mathf.Abs(localExtents.x);
            float radiusY = Mathf.Abs(localExtents.y);
            float radiusZ = Mathf.Abs(localExtents.z);

            // Ship models are typically much shorter (Y) than they are wide/long (X/Z). Scaling
            // the sphere directly from each axis's raw extent collapses it into a flat disc lying
            // in the ship's XZ plane instead of a rounded shell enclosing it in 3D. Using the wider
            // of X/Y for BOTH the width and height keeps the cross-section circular, so the shell
            // wraps fully around the hull top-to-bottom, not just side-to-side.
            float crossRadius = Mathf.Max(radiusX, radiusY);

            shellGO.transform.localScale = new Vector3(
                crossRadius * 2f * PaddingMultiplier,
                crossRadius * 2f * PaddingMultiplier,
                Mathf.Max(radiusZ, crossRadius) * 2f * PaddingMultiplier * ForwardStretchMultiplier);

            var mr = shellGO.GetComponent<MeshRenderer>();
            mr.sharedMaterial = sharedShieldMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            shellGO.SetActive(false);

            var effect = shellGO.AddComponent<ShipShieldEffect>();
            effect.shieldRenderer = mr;
            effect.propBlock = new MaterialPropertyBlock();
            return effect;
        }

        /// <summary>Triggers (or restarts) the shield flash. Safe to call repeatedly.</summary>
        public void Flash()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }
            gameObject.SetActive(true);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            float t = 0f;
            while (t < FlashDuration)
            {
                // Unscaled time: combat resolution runs while game time is paused (CLAUDE.md).
                t += Time.unscaledDeltaTime;
                float intensity = Mathf.Sin(Mathf.Clamp01(t / FlashDuration) * Mathf.PI) * PeakIntensity;
                propBlock.SetFloat(FlashIntensityID, intensity);
                shieldRenderer.SetPropertyBlock(propBlock);
                yield return null;
            }

            gameObject.SetActive(false);
            flashRoutine = null;
        }
    }
}

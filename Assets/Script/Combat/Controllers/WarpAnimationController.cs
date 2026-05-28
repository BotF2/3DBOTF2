using BOTF3D.Audio;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;



namespace BOTF3D.Combat
{
    /// <summary>
    /// Manages warp-in and warp-out animations for ships.
    /// Handles stretching effects and staggered arrivals.
    /// </summary>
    public class WarpAnimationController : IController
    {
        public void Initialize() { }
        public void UpdateState() { }
        // Warp animation constants
        public const float SIDE1_COMBAT_START_X = -3000f;
        public const float SIDE1_COMBAT_END_X = -200f;
        public const float SIDE1_TRANSPORT_START_X = -3200f;
        public const float SIDE1_TRANSPORT_END_X = -400f;
        public const float SIDE2_COMBAT_START_X = 3000f;
        public const float SIDE2_COMBAT_END_X = 200f;
        public const float SIDE2_TRANSPORT_START_X = 3200f;
        public const float SIDE2_TRANSPORT_END_X = 400f;
        private const float WARP_DURATION = 2.5f;
        private const float CONTRACTION_DURATION = 0.4f;

        private readonly CombatController combatController;
        private readonly SoundData warpInSound;

        public WarpAnimationController(CombatController controller, SoundData warpSound)
        {
            combatController = controller;
            warpInSound = warpSound;
        }

        /// <summary>
        /// Get warp start X position
        /// </summary>
        public static float GetWarpStartX(int side, bool isTransport)
        {
            if (side == 1)
            {
                return isTransport ? SIDE1_TRANSPORT_START_X : SIDE1_COMBAT_START_X;
            }
            else
            {
                return isTransport ? SIDE2_TRANSPORT_START_X : SIDE2_COMBAT_START_X;
            }
        }

        /// <summary>
        /// Get warp end X position
        /// </summary>
        public static float GetWarpEndX(int side, bool isTransport)
        {
            if (side == 1)
            {
                return isTransport ? SIDE1_TRANSPORT_END_X : SIDE1_COMBAT_END_X;
            }
            else
            {
                return isTransport ? SIDE2_TRANSPORT_END_X : SIDE2_COMBAT_END_X;
            }
        }

        /// <summary>
        /// Start the warp-in animation coroutine with stretch effect
        /// </summary>
        public IEnumerator StartWarpInAnimation()
        {
            Debug.Log("🌀 Starting warp-in animation...");

            combatController.WarpingIn = true;
            combatController.WarpingAnimationOver = false;

            // Tell camera we're warping in
            if (ShipCombatCameraController.Instance != null)
            {
                ShipCombatCameraController.Instance.SetWarpingIn(true);
            }

            // Play warp sound
            if (warpInSound != null)
            {
                AudioManager.Instance?.PlaySFX3D(warpInSound.name, Vector3.zero);
            }

            // Collect all ships with warp data
            List<WarpData> allWarpData = CollectWarpData();

            Debug.Log($"  Animating {allWarpData.Count} ships - staggered start with individual contraction");

            // Initial stretch: all ships start stretched along travel direction
            float warpStretchScale = 50f;
            ApplyInitialStretch(allWarpData, warpStretchScale);

            yield return null;

            // Animate warp travel and contraction
            yield return AnimateWarpTravel(allWarpData, warpStretchScale);

            Debug.Log("✅ Warp-in and contraction complete for all ships");

            // Final cleanup - reset scales
            FinalizeWarpIn(allWarpData);

            Debug.Log("✅ Warp-in animation complete");

            combatController.WarpingIn = false;
            combatController.WarpingAnimationOver = true;
        }

        /// <summary>
        /// Collect all warp data components from ships
        /// </summary>
        private List<WarpData> CollectWarpData()
        {
            List<WarpData> allWarpData = new List<WarpData>();

            foreach (var ship in combatController.CombatData.SideOneShipCons)
            {
                var wd = ship?.GetComponent<WarpData>();
                if (wd != null) allWarpData.Add(wd);
            }

            foreach (var ship in combatController.CombatData.SideTwoShipCons)
            {
                var wd = ship?.GetComponent<WarpData>();
                if (wd != null) allWarpData.Add(wd);
            }

            return allWarpData;
        }

        /// <summary>
        /// Apply initial stretch to all ships
        /// </summary>
        private void ApplyInitialStretch(List<WarpData> allWarpData, float stretchScale)
        {
            foreach (var wd in allWarpData)
            {
                if (wd != null && wd.gameObject != null && wd.shipModel != null)
                {
                    wd.shipModel.transform.localScale = new Vector3(1f, 1f, stretchScale);
                }
            }
        }

        /// <summary>
        /// Animate warp travel with staggered starts and individual contractions
        /// </summary>
        private IEnumerator AnimateWarpTravel(List<WarpData> allWarpData, float warpStretchScale)
        {
            float maxPhaseDuration = WARP_DURATION + 1.5f; // Extra buffer
            float elapsed = 0f;

            while (elapsed < maxPhaseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                bool allShipsComplete = true;

                foreach (var wd in allWarpData)
                {
                    if (wd == null || wd.gameObject == null) continue;

                    ShipController shipController = wd.GetComponent<ShipController>();
                    if (shipController == null) continue;

                    // Phase 1: Ship waiting to start
                    if (elapsed < wd.startDelay)
                    {
                        allShipsComplete = false;
                        continue;
                    }

                    // Phase 2: Ship traveling
                    if (!wd.hasArrived)
                    {
                        float travelElapsed = elapsed - wd.startDelay;
                        float travelT = Mathf.Clamp01(travelElapsed / wd.travelDuration);

                        if (travelT < 1f)
                        {
                            float smoothT = 1f - Mathf.Pow(1f - travelT, 3f);
                            shipController.transform.position = Vector3.Lerp(wd.startPosition, wd.endPosition, smoothT);
                            allShipsComplete = false;
                        }
                        else
                        {
                            shipController.transform.position = wd.endPosition;
                            wd.hasArrived = true;
                            wd.isContracting = true;
                            wd.contractionStartTime = elapsed;
                            wd.contractionProgress = 0f;
                        }
                    }

                    // Phase 3: Ship contracting
                    if (wd.isContracting && wd.contractionProgress < 1f)
                    {
                        float contractionElapsed = elapsed - wd.contractionStartTime;
                        wd.contractionProgress = Mathf.Clamp01(contractionElapsed / CONTRACTION_DURATION);

                        float smoothContractionT = Mathf.Pow(wd.contractionProgress, 2f);
                        float currentScale = Mathf.Lerp(warpStretchScale, 1f, smoothContractionT);

                        if (wd.shipModel != null)
                        {
                            wd.shipModel.transform.localScale = new Vector3(1f, 1f, currentScale);
                        }

                        if (wd.contractionProgress < 1f)
                        {
                            allShipsComplete = false;
                        }
                    }
                }

                if (allShipsComplete)
                {
                    Debug.Log($"✅ All ships complete at {elapsed:F2}s");
                    break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Finalize warp-in: reset scales and clean up warp data
        /// </summary>
        private void FinalizeWarpIn(List<WarpData> allWarpData)
        {
            foreach (var wd in allWarpData)
            {
                if (wd != null && wd.gameObject != null)
                {
                    ShipController shipController = wd.GetComponent<ShipController>();
                    if (shipController != null)
                    {
                        shipController.transform.position = wd.endPosition;
                        shipController.transform.localScale = Vector3.one;
                    }

                    if (wd.shipModel != null)
                    {
                        wd.shipModel.transform.localScale = Vector3.one;
                    }

                    shipController.SetWarpInOver();
                    Object.Destroy(wd);
                }
            }
        }
    }

    /// <summary>
    /// Component to store warp start/end positions and ship model reference
    /// </summary>
    public class WarpData : MonoBehaviour
    {
        public Vector3 startPosition;
        public Vector3 endPosition;
        public GameObject shipModel;
        public float startDelay;
        public float travelDuration;
        public bool hasArrived;
        public bool isContracting;
        public float contractionStartTime;
        public float contractionProgress;

        public void Initialize(Vector3 start, Vector3 end, GameObject model, int shipSide)
        {
            startPosition = start;
            endPosition = end;
            shipModel = model;

            startDelay = Random.Range(0f, 1.0f);
            travelDuration = 0.6f;

            hasArrived = false;
            isContracting = false;
            contractionStartTime = 0f;
            contractionProgress = 0f;
        }
    }
}

using BOTF3D.UI;
using UnityEngine;

namespace BOTF3D.Core
{
    /// <summary>
    /// Handles Side 2 Area 1 parent GameObject for warp-in animation.
    /// Replaces old Unity Animator system with manual GameObject control.
    /// </summary>
    public class S2A1Animator : MonoBehaviour
    {
        [Header("Parent GameObject Reference")]
        public GameObject parentGameObject;  // Reference to the parent holding ships

        private bool isWarpingIn = false;

        void Start()
        {
            Debug.Log($"🔵 S2A1Animator.Start() CALLED on GameObject '{gameObject.name}', active={gameObject.activeInHierarchy}");

            // Auto-assign parent if not set
            if (parentGameObject == null)
            {
                parentGameObject = gameObject;
                Debug.Log($"  Auto-assigned parent GameObject: {parentGameObject.name}");
            }
            else
            {
                Debug.Log($"  Using assigned parent GameObject: {parentGameObject.name}");
            }

            Debug.Log("S2A1Animator initialized - waiting for RunAnimation()");
        }

        public void RunAnimation()
        {
            // ✅ Check if WarpingIn is TRUE
            if (CombatUIManager.Instance?.CurrentCombatController != null &&
                CombatUIManager.Instance.CurrentCombatController.WarpingIn)
            {
                isWarpingIn = true;
                Debug.Log("✅ S2A1 animation triggered - GameObject is ready for warp");

                // The actual animation is handled by CombatController.AnimateWarpIn()
                // This script just tracks state
            }
            else
            {
                Debug.LogWarning($"⚠️ S2A1: Cannot trigger animation - WarpingIn={CombatUIManager.Instance?.CurrentCombatController?.WarpingIn}");
            }
        }

        /// <summary>
        /// Called when warp audio should play (stub for compatibility)
        /// Audio is now handled centrally by CombatController
        /// </summary>
        public void PlayWarp()
        {
            Debug.Log("S2A1: PlayWarp event received (audio handled centrally by CombatController)");
        }

        /// <summary>
        /// Called when warp-in animation completes
        /// Signals that ships are in position
        /// </summary>
        public void EndOfFiendWarp()
        {
            Debug.Log("S2A1: EndOfFiendWarp called - Warp animation complete");
            isWarpingIn = false;

            if (CombatUIManager.Instance?.CurrentCombatController != null)
            {
                CombatUIManager.Instance.CurrentCombatController.WarpingAnimationOver = true;
                Debug.Log("  ✅ Set WarpingAnimationOver = true");
            }
            else
            {
                Debug.LogWarning("⚠️ S2A1: CombatUIManager or CurrentCombatController is null!");
            }
        }
    }
}


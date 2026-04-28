using BOTF3D.UI;
using UnityEngine;

namespace BOTF3D.Core
{
    public class S1A1Animator : MonoBehaviour
    {
        public Animator anim;
        void Start()
        {
            Debug.Log($"🔵 S1A1Animator.Start() CALLED on GameObject '{gameObject.name}', active={gameObject.activeInHierarchy}");

            anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;

                // ✅ CRITICAL: Use UnscaledTime so animations run even when Time.timeScale = 0
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;

                anim.SetBool("WarpInS1A1", false);
                Debug.Log("S1A1 animator initialized - waiting for RunAnimation()");
            }
            else
            {
                Debug.LogError($"❌ S1A1Animator: No Animator component found on {gameObject.name}!");
            }
        }

        public void RunAnimation()
        {
            // ✅ Fixed: Check if WarpingIn is TRUE (not false!)
            if (CombatUIManager.Instance?.CurrentCombatController != null &&
                CombatUIManager.Instance.CurrentCombatController.WarpingIn)  // ✅ Changed from !WarpingIn
            {
                if (anim != null)
                {
                    anim.SetBool("WarpInS1A1", true);
                    Debug.Log("✅ S1A1 animation triggered");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ S1A1: Cannot trigger animation - WarpingIn={CombatUIManager.Instance?.CurrentCombatController?.WarpingIn}");
            }
        }
        /// <summary>
        /// ✅ Called by AnimationEvent in S1A1_Warp animation
        /// Audio is now handled centrally by CombatController, so this is just a stub
        /// </summary>
        public void PlayWarp()
        {
            // ✅ Empty method to satisfy Animation Event
            // Audio is played by CombatController.RunAnimation() instead
            Debug.Log("S1A1: PlayWarp AnimationEvent received (audio handled centrally)");
        }
        /// <summary>
        /// Called by AnimationEvent in S1A1_Stop/End animations
        /// Signals that warp-in animation has completed
        /// </summary>
        public void EndOfFiendWarp()
        {
            Debug.Log("S1A1: EndOfFiendWarp called - Warp animation complete");

            if (CombatUIManager.Instance?.CurrentCombatController != null)
            {
                CombatUIManager.Instance.CurrentCombatController.WarpingAnimationOver = true;
                Debug.Log("  ✅ Set WarpingAnimationOver = true");
            }
            else
            {
                Debug.LogWarning("⚠️ S1A1: CombatUIManager or CurrentCombatController is null!");
            }
        }
    }
}

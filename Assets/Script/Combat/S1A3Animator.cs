using BOTF3D.UI;
using UnityEngine;

namespace BOTF3D.Core
{

    public class S1A3Animator : MonoBehaviour
    {
        public Animator anim;

        void Start()
        {
            anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.SetBool("WarpInS1A3", false);
                Debug.Log("S1A3 animator initialized - waiting for RunAnimation()");
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
                    anim.SetBool("WarpInS1A3", true);
                    Debug.Log("✅ S1A3 animation triggered");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ S1A3: Cannot trigger animation - WarpingIn={CombatUIManager.Instance?.CurrentCombatController?.WarpingIn}");
            }
        }
        /// <summary>
        /// ✅ Called by AnimationEvent in S1A3_Warp animation
        /// Audio is now handled centrally by CombatController, so this is just a stub
        /// </summary>
        public void PlayWarp()
        {
            // ✅ Empty method to satisfy Animation Event
            // Audio is played by CombatController.RunAnimation() instead
            Debug.Log("S1A3: PlayWarp AnimationEvent received (audio handled centrally)");
        }
        /// <summary>
        /// Called by AnimationEvent in S1A3_Stop/End animations
        /// Signals that warp-in animation has completed
        /// </summary>
        public void EndOfFiendWarp()
        {
            Debug.Log("S1A3: EndOfFiendWarp called - Warp animation complete");

            if (CombatUIManager.Instance?.CurrentCombatController != null)
            {
                CombatUIManager.Instance.CurrentCombatController.WarpingAnimationOver = true;
                Debug.Log("  ✅ Set WarpingAnimationOver = true");
            }
            else
            {
                Debug.LogWarning("⚠️ S1A3: CombatUIManager or CurrentCombatController is null!");
            }
        }
    }
}

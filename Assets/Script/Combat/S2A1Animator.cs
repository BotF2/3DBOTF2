using BOTF3D.UI;
using UnityEngine;

namespace BOTF3D.Core
{
    public class S2A1Animator : MonoBehaviour
    {
        public Animator anim;

        void Start()
        {
            anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.SetBool("WarpInS2A1", false);
                Debug.Log("S2A1 animator initialized - waiting for RunAnimation()");
            }
        }

        public void RunAnimation()
        {
            // ✅ Add null checks and trigger animation here
            if (CombatUIManager.Instance?.CurrentCombatController != null &&
                CombatUIManager.Instance.CurrentCombatController.WarpingIn)
            {
                if (anim != null)
                {
                    anim.SetBool("WarpInS2A1", true);
                    Debug.Log("✅ S2A1 animation triggered");
                }
            }
        }

        /// <summary>
        /// ✅ Called by AnimationEvent in S2A1_Warp animation
        /// Audio is now handled centrally by CombatController, so this is just a stub
        /// </summary>
        public void PlayWarp()
        {
            // ✅ Empty method to satisfy Animation Event
            // Audio is played by CombatController.RunAnimation() instead
            Debug.Log("S2A1: PlayWarp AnimationEvent received (audio handled centrally)");
        }

        /// <summary>
        /// Called by AnimationEvent in S2A1_Stop/End animations
        /// Signals that warp-in animation has completed
        /// </summary>
        public void EndOfFiendWarp()
        {
            Debug.Log("S2A1: EndOfFiendWarp called - Warp animation complete");

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


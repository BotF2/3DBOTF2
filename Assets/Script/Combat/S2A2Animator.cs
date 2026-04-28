using BOTF3D.UI;
using UnityEngine;

namespace BOTF3D.Core
{

    public class S2A2Animator : MonoBehaviour
    {
        public Animator anim;

        void Start()
        {
            Debug.Log($"🔵 S2A2Animator.Start() CALLED on GameObject '{gameObject.name}', active={gameObject.activeInHierarchy}");

            anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;

                // ✅ CRITICAL: Use UnscaledTime so animations run even when Time.timeScale = 0
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;

                anim.SetBool("WarpInS2A2", false);
                Debug.Log("S2A2 animator initialized - waiting for RunAnimation()");
            }
            else
            {
                Debug.LogError($"❌ S2A2Animator: No Animator component found on {gameObject.name}!");
            }
        }

        public void RunAnimation()
        {
            if (CombatUIManager.Instance.CurrentCombatController != null
                && CombatUIManager.Instance.CurrentCombatController.WarpingIn)
            {
                if (anim != null)
                {
                    anim.SetBool("WarpInS2A2", true);
                    Debug.Log("✅ S2A2 animation triggered");
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
            Debug.Log("S2A2: PlayWarp AnimationEvent received (audio handled centrally)");
        }
        /// <summary>
        /// Called by AnimationEvent in S2A2_Stop/End animations
        /// Signals that warp-in animation has completed
        /// </summary>
        public void EndOfFiendWarp()
        {
            Debug.Log("S2A2: EndOfFiendWarp called - Warp animation complete");

            if (CombatUIManager.Instance?.CurrentCombatController != null)
            {
                CombatUIManager.Instance.CurrentCombatController.WarpingAnimationOver = true;
                Debug.Log("  ✅ Set WarpingAnimationOver = true");
            }
            else
            {
                Debug.LogWarning("⚠️ S2A2: CombatUIManager or CurrentCombatController is null!");
            }
        }
    }
}

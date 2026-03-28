using BOTF3D.UI;
using UnityEngine;

namespace BOTF3D.Core
{

    public class S2A3Animator : MonoBehaviour
    {
        public Animator anim;

        void Start()
        {
            anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.SetBool("WarpInS2A3", false);
                Debug.Log("S2A3 animator initialized - waiting for RunAnimation()");
            }
        }

        // Update is called once per frame  
        public void RunAnimation()
        {
            if (CombatUIManager.Instance.CurrentCombatController != null &&
                !CombatUIManager.Instance.CurrentCombatController.WarpingIn)
            {
                if (anim != null)
                {
                    anim.SetBool("WarpInS2A3", true);
                    Debug.Log("✅ S2A3 animation triggered");
                }
            }
            // lets warp animation run
        }
        /// <summary>
        /// ✅ Called by AnimationEvent in S2A1_Warp animation
        /// Audio is now handled centrally by CombatController, so this is just a stub
        /// </summary>
        public void PlayWarp()
        {
            // ✅ Empty method to satisfy Animation Event
            // Audio is played by CombatController.RunAnimation() instead
            Debug.Log("S2A3: PlayWarp AnimationEvent received (audio handled centrally)");
        }
        /// <summary>
        /// Called by AnimationEvent in S2A3_Stop/End animations
        /// Signals that warp-in animation has completed
        /// </summary>
        public void EndOfFiendWarp()
        {
            Debug.Log("S2A3: EndOfFiendWarp called - Warp animation complete");

            if (CombatUIManager.Instance?.CurrentCombatController != null)
            {
                CombatUIManager.Instance.CurrentCombatController.WarpingAnimationOver = true;
                Debug.Log("  ✅ Set WarpingAnimationOver = true");
            }
            else
            {
                Debug.LogWarning("⚠️ S2A3: CombatUIManager or CurrentCombatController is null!");
            }
        }
    }
}

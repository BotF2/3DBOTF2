using UnityEngine;

namespace BOTF3D.Core
{
    public class S1A1Animator : MonoBehaviour
    {
        public Animator anim;
        public AudioSource warpAudioSource_0;

        void Start()
        {
            anim = GetComponent<Animator>();
            anim.enabled = true;
            anim.SetBool("WarpInS1A1", true);
        }

        public void RunAnimation()
        {
            if (CombatUIController.Instance.CombatController != null && !CombatUIController.Instance.CombatController.WarpingIn)
            {
                anim.SetBool("WarpInS1A1", true);
                PlayWarp();
            }
        }

        public void PlayWarp()
        {
            if (CombatUIController.Instance.CombatController.WarpingIn)
            {
                //warpAudioSource_0.volume = 1f;
                //warpAudioSource_0.Play();
            }
        }

        /// <summary>
        /// Called by AnimationEvent in S1A1_Stop/End animations
        /// Signals that warp-in animation has completed
        /// </summary>
        public void EndOfFiendWarp()
        {
            Debug.Log("S1A1: EndOfFiendWarp called - Warp animation complete");

            if (CombatUIController.Instance?.CombatController != null)
            {
                CombatUIController.Instance.CombatController.WarpingAnimationOver = true;
                Debug.Log("  ✅ Set WarpingAnimationOver = true");
            }
        }
    }
}

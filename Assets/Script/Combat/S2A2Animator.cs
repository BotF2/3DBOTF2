using UnityEngine;

namespace Assets.Core
{

    public class S2A2Animator : MonoBehaviour
    {
        public Animator anim;
        public AudioSource warpAudioSource_0;

        void Start()
        {
            anim = GetComponent<Animator>();
            anim.enabled = true; // Ensure the animator is enabled
            anim.SetBool("WarpInS2A2", true); // Ensure the animation is not running at start
        }

        public void RunAnimation()
        {
            if (CombatUIController.Instance.CombatController != null & !CombatUIController.Instance.CombatController.WarpingIn)
            {
                anim.SetBool("WarpInS2A2", true); // code state turns on warp animation
                PlayWarp();
               // CombatUIController.Instance.CombatController.warpingInOver = false; // reset the warping in state
            }
            // lets warp animation run
        }
        public void PlayWarp() // called in animation - warp
        {
            //if (GameManager.Instance._statePassedCombatInit)
            //{
            //    warpAudioSource_0.volume = 1f;
            //    warpAudioSource_0.Play();
            //}
        }
    }
}

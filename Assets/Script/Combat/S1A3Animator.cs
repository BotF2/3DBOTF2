using UnityEngine;

namespace BOTF3D.Core
{

    public class S1A3Animator : MonoBehaviour
    {
        public Animator anim;
        public AudioSource warpAudioSource_0;

        void Start()
        {
            anim = GetComponent<Animator>();
            anim.enabled = true; // Ensure the animator is enabled
            anim.SetBool("WarpInS1A3", true); // Ensure the animation is not running at start
        }

        public void RunAnimation()
        {
            if (CombatUIController.Instance.CombatController != null & !CombatUIController.Instance.CombatController.WarpingIn)
            {
                anim.SetBool("WarpInS1A3", true);   // Animator parameter to trigger the warp animation
                PlayWarp();
                //CombatUIController.Instance.CombatController.warpingInOver = false; // reset the warping in state   
            }
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

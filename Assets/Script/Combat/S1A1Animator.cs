using UnityEngine;

namespace Assets.Core
{

    public class S1A1Animator : MonoBehaviour
    {
        // must CivName class and file the same
        public Animator anim;
        public AudioSource warpAudioSource_0;


        void Start()
        {
            anim = GetComponent<Animator>();
            anim.enabled = true; // Ensure the animator is enabled
            anim.SetBool("WarpInS1A1", false); // Ensure the animation is not running at start
        }

        // Update is called once per frame  
        public void RunAnimation()
        {
            if (CombatUIController.Instance.CombatController != null & !CombatUIController.Instance.CombatController.warpingIn)
            {
                anim.SetBool("WarpInS1A1", true); // Anamator parameter to trigger the warp animation
                PlayWarp();
                //CombatUIController.Instance.CombatController.warpingInOver = false; // reset the warping in state
            }
        }

        public void PlayWarp() // called in animation - warps by event to function PlayWarp()
        {
            if (CombatUIController.Instance.CombatController.warpingIn)
            {
                //warpAudioSource_0.volume = 1f;
                //warpAudioSource_0.Play();
            }
        }
    }
}

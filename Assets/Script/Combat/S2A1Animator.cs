using UnityEngine;

namespace Assets.Core
{

    public class S2A1Animator : MonoBehaviour
    {
        public Animator anim;
        public AudioSource warpAudioSource_0;

        void Start()
        {
            anim = GetComponent<Animator>();
            anim.enabled = true;
            anim.SetBool("WarpInS2A1", true);
        }

        public void RunAnimation()
        {
            if (CombatUIController.Instance.CombatController != null & !CombatUIController.Instance.CombatController.warpingIn)
            {
                anim.SetBool("WarpInS2A1", true);
                PlayWarp();
                //CombatUIController.Instance.CombatController.warpingInOver  = false; // reset the warping in state
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

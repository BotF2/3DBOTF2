using UnityEngine;

namespace BOTF3D.Core
{

    public class S1A2Animator : MonoBehaviour
    {
        public Animator anim;
        public AudioSource warpAudioSource_0;

        void Start()
        {
            anim = GetComponent<Animator>();
            anim.enabled = true;
            anim.SetBool("WarpInS1A2", true);

        }

        // Update is called once per frame  
        void Update()
        {
            //if (CombatUIController.Instance.CombatController != null & CombatUIController.Instance.CombatController.warpingInOver)
            //{
            //    anim.SetBool("WarpIn", true);
            //    PlayWarp();
            //    CombatUIController.Instance.CombatController.warpingInOver = false; // reset the warping in state
            //}
        }
        public void RunAnimation()
        {
            if (CombatUIController.Instance.CombatController != null && !CombatUIController.Instance.CombatController.WarpingIn)
            {
                anim.SetBool("WarpInS1A2", true); // code state turns on warp animation
                PlayWarp();
               // CombatUIController.Instance.CombatController.warpingInOver = false; // reset the warping in state
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

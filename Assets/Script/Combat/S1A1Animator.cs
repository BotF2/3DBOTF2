using BOTF3D.UI;
using UnityEngine;

namespace BOTF3D.Core
{
    public class S1A1Animator : MonoBehaviour
    {
        public Animator anim;
        void Start()
        {
            anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.SetBool("WarpInS1A1", false);
                Debug.Log("S1A1 animator initialized - waiting for RunAnimation()");
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
                    // ✅ Log current animator state
                    AnimatorStateInfo currentState = anim.GetCurrentAnimatorStateInfo(0);
                    Debug.Log($"S1A1: Current state BEFORE trigger: hash={currentState.shortNameHash}, normalizedTime={currentState.normalizedTime}");

                    // ✅ Log animator state BEFORE triggering
                    Debug.Log($"S1A1: Triggering animation. Animator enabled={anim.enabled}, hasController={anim.runtimeAnimatorController != null}");
                    Debug.Log($"  GameObject active={gameObject.activeInHierarchy}, updateMode={anim.updateMode}, speed={anim.speed}");

                    // ✅ CRITICAL: Ensure animator is using Normal update mode and speed is 1
                    if (anim.updateMode != AnimatorUpdateMode.Normal)
                    {
                        Debug.LogWarning($"  ⚠️ Animator was in {anim.updateMode} mode - changing to Normal");
                        anim.updateMode = AnimatorUpdateMode.Normal;
                    }

                    if (anim.speed <= 0f)
                    {
                        Debug.LogWarning($"  ⚠️ Animator speed was {anim.speed} - setting to 1");
                        anim.speed = 1f;
                    }

                    // ✅ Check if parameter exists
                    bool hasParam = false;
                    foreach (var param in anim.parameters)
                    {
                        if (param.name == "WarpInS1A1")
                        {
                            hasParam = true;
                            Debug.Log($"  ✅ Found parameter 'WarpInS1A1', current value={anim.GetBool("WarpInS1A1")}");
                            break;
                        }
                    }

                    if (!hasParam)
                    {
                        Debug.LogError("  ❌ Animator parameter 'WarpInS1A1' NOT FOUND!");
                        Debug.Log($"     Available parameters: {string.Join(", ", System.Array.ConvertAll(anim.parameters, p => p.name + " (" + p.type + ")"))}");
                    }

                    anim.SetBool("WarpInS1A1", true);

                    // ✅ Force animator to update
                    anim.Update(0f);

                    // ✅ Log state AFTER setting parameter
                    AnimatorStateInfo newState = anim.GetCurrentAnimatorStateInfo(0);
                    Debug.Log($"✅ S1A1 animation triggered, parameter now={anim.GetBool("WarpInS1A1")}");
                    Debug.Log($"   State AFTER trigger: hash={newState.shortNameHash}, normalizedTime={newState.normalizedTime}");

                    // ✅ Check if we're transitioning
                    if (anim.IsInTransition(0))
                    {
                        AnimatorTransitionInfo transitionInfo = anim.GetAnimatorTransitionInfo(0);
                        Debug.Log($"   🔄 In transition! normalizedTime={transitionInfo.normalizedTime}");
                    }
                    else
                    {
                        Debug.LogWarning($"   ⚠️ NOT in transition - state might not be changing!");
                    }
                }
                else
                {
                    Debug.LogError("❌ S1A1: anim is NULL!");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ S1A1: Cannot trigger animation - WarpingIn={CombatUIManager.Instance?.CurrentCombatController?.WarpingIn}");
            }
        }
        /// <summary>
        /// ✅ Called by AnimationEvent in S1A1_Warp animation
        /// Audio is now handled centrally by CombatController, so this is just a stub
        /// </summary>
        public void PlayWarp()
        {
            // ✅ Empty method to satisfy Animation Event
            // Audio is played by CombatController.RunAnimation() instead
            Debug.Log("S1A1: PlayWarp AnimationEvent received (audio handled centrally)");
        }
        /// <summary>
        /// Called by AnimationEvent in S1A1_Stop/End animations
        /// Signals that warp-in animation has completed
        /// </summary>
        public void EndOfFiendWarp()
        {
            Debug.Log("S1A1: EndOfFiendWarp called - Warp animation complete");

            if (CombatUIManager.Instance?.CurrentCombatController != null)
            {
                CombatUIManager.Instance.CurrentCombatController.WarpingAnimationOver = true;
                Debug.Log("  ✅ Set WarpingAnimationOver = true");
            }
            else
            {
                Debug.LogWarning("⚠️ S1A1: CombatUIManager or CurrentCombatController is null!");
            }
        }
    }
}

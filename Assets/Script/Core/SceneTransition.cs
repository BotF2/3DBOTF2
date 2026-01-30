using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    //public static SceneTransition Instance;
    public Animator Transition;
    public float WaitForSeconds = 1f;

    public void LoadCombatScene()
    {
        StartCoroutine("LoadSceneCombat");
    }
    public void LoadMainMenuScene()
    {
        StartCoroutine("LoadSceneMainMenu");
    }
    IEnumerable LoadSceneCombat()
    {
        Transition.SetTrigger("Start");
        yield return new WaitForSeconds(WaitForSeconds);
        SceneManager.LoadScene("CombatScene");
    }
    IEnumerable LoadSceneMainMenu()

    {
        Transition.SetTrigger("Start");
        yield return new WaitForSeconds(WaitForSeconds);
        SceneManager.LoadScene("MainMenuGalaxyScene");
    }
}

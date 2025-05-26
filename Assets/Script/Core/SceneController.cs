using UnityEngine;
using Assets.Core;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    /// <summary>
    /// We do not yet have a loading scene, the Persistent and main menu are already there at play
    /// Galaxy scene is added as we load up the user game choices
    /// Combat hides Main Menu including what really are Galaxy elements 
    /// </summary>
    public static SceneController Instance { get; private set; }
    private static string previousSceneName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps it across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        //if (SceneController.Instance != null)
        //{
        //   SceneController.Instance.LoadMainMenuScene();
        //}
        //else
        //{
        //    Debug.LogError("GameManager instance not found!");
        //}
    }
    public void LoadCombatScene(DiplomacyController diplomacyController)
    {
        previousSceneName = SceneManager.GetActiveScene().name; 
       // TimeManager.Instance.PauseTime(); does not work
        SceneManager.LoadSceneAsync("CombatScene", LoadSceneMode.Additive); 
        HideScene(previousSceneName);
    }
    private void HideScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid())
        {
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                obj.SetActive(false); // Disable all root objects
            }
        }
    }
    private void ExposeScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid())
        {
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                obj.SetActive(true); // Disable all root objects
            }
        }
    }
    private void SetSceneActive(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid())
        {
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                obj.SetActive(true); // Disable all root objects
            }
        }
    }
    public void UnloadCombatScene()
    {
        SceneManager.UnloadSceneAsync("CombatScene");
        ExposeScene("MainMenuScene"); // Re-enable the previous scene

        //if (!string.IsNullOrEmpty(previousSceneName))
        //{
        //    Scene scene = SceneManager.GetSceneByName(previousSceneName);
        //    if (scene.IsValid())
        //    {
        //        foreach (GameObject obj in scene.GetRootGameObjects())
        //        {
        //            obj.SetActive(true); // Re-enable all objects
        //        }
        //    }
        //}
        //else if (string.IsNullOrEmpty(previousSceneName)) ;
    }
    public void LoadNextScene(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName);
    }
}

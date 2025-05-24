using UnityEngine;
using Assets.Core;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
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
    public void LoadCombatScene()
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
    public void LoadMainMenuScene()
    {
        //SceneManager.UnloadSceneAsync("CombatScene");
        //GalaxyMenuUIController.Instance.CloseMenu(Menu.DiplomacyMenu);

        if (!string.IsNullOrEmpty(previousSceneName))
        {
            Scene scene = SceneManager.GetSceneByName(previousSceneName);
            if (scene.IsValid())
            {
                foreach (GameObject obj in scene.GetRootGameObjects())
                {
                    obj.SetActive(true); // Re-enable all objects
                }
            }
        }
        else if (string.IsNullOrEmpty(previousSceneName))
            SceneManager.LoadSceneAsync("MainMenuScene");
    }
    public void LoadNextScene(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName);
    }
}

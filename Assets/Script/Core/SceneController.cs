using UnityEngine;
using Assets.Core;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    /// <summary>
    /// We do not yet have a loading scene, the Persistent Scene and Main Menu Scene are present at runtime.
    /// Galaxy scene is added as we load up the user game choices
    /// Combat hides Main Menu including what really are Galaxy elements it contains. 
    /// </summary>
    public static SceneController Instance { get; private set; }
    //public GameObject CombatUIPrefab { get; internal set; }
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


    //private void OnEnable()
    //{
    //    SceneManager.sceneLoaded += OnSceneLoaded;
    //}

    //private void OnDisable()
    //{
    //    SceneManager.sceneLoaded -= OnSceneLoaded;
    //}
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject parent = GameObject.FindGameObjectWithTag("CombatUIParent");
        CombatManager.Instance.InstantiateCombatUIGameObject(parent);
    }

    public void LoadCombatScene(DiplomacyController diplomacyController)
    {
        if (GameController.Instance.AreWeLocalPlayer(diplomacyController.DiplomacyData.CivMajor.CivData.CivEnum) ||
            GameController.Instance.AreWeLocalPlayer(diplomacyController.DiplomacyData.CivOther.CivData.CivEnum))
        {
            previousSceneName = "MainMenuScene";//SceneManager.GetActiveScene().name; 
                                                // TimeManager.Instance.PauseTime(); does not work
            SceneManager.LoadSceneAsync("CombatScene", LoadSceneMode.Additive);

            HideScene(previousSceneName);
            OnSceneLoaded(SceneManager.GetSceneByName("CombatScene"), LoadSceneMode.Additive); // Call OnSceneLoaded to initialize Combat UI
            CombatManager.Instance.SetDiplomacyController(diplomacyController); // Set the diplomacy controller for the combat scene
        }
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
        //SceneManager.sceneLoaded -= OnSceneLoaded;

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

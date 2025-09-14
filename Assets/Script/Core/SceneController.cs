using Assets.Core;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;


public class SceneController : MonoBehaviour
{
    /// <summary>
    /// We do not yet have a loading scene, the Persistent Scene and Main Menu Scene are present at runtime.
    /// Galaxy scene is added as we load up the user game choices
    /// Combat hides Main Menu including what really are Galaxy elements contained in Main Menu scene. 
    /// </summary>
    public static SceneController Instance { get; private set; }

    private static string previousSceneName;
    public List<GameObject> persistentObjects;
    private GameObject galaxyCameraDragNDrop; // Reference to the Galaxy Camera Drag and Drop GameObject
    public GameObject ShipCombatCameraGO;

    private void Awake()
    {

        //persistentObjects.AddRange(galaxyCameraDragNDrop.GetComponents<Transform>()); // Add the Galaxy Camera Drag and Drop GameObject itself to persistentObjects
        //ersistentObjects.AddRange(galaxyCameraDragNDrop.GetComponentsInChildren<Transform>(true)); // Add all children of the Galaxy Camera Drag and Drop GameObject to persistentObjects
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps it across scenes
            MarkPeristentObject(); // Mark this object as persistent
        }
        else
        {
            CleanUPAndDistroy();
           //Destroy(gameObject);
        }
        galaxyCameraDragNDrop = GameObject.Find("GalaxyCameraDragMoveZoom");
    }

    private void CleanUPAndDistroy()
    {
        //if (persistentObjects != null)
        //{
        //    for (int i = 0; i < persistentObjects.Length; i++)
        //    {
        //        Destroy(persistentObjects[i]);
        //    }
        //}
        Destroy(gameObject); // Destroy the duplicate instance
    }

    private void MarkPeristentObject()
    {
        for (int i = 0; i < persistentObjects.Count; i++)
        {

            if (persistentObjects[i] != null)
            {
                DontDestroyOnLoad(persistentObjects[i]); 
            }
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
        //GameObject parent = GameObject.FindGameObjectWithTag("CombatUIParent");
        //CombatManager.Instance.SetUpCombatUIGameObject(parent);

    }

    public void LoadCombatScene(DiplomacyController diplomacyController)
    {
        if (galaxyCameraDragNDrop == null)
        {
            galaxyCameraDragNDrop = GameObject.Find("GalaxyCameraDragMoveZoom");
        }
        galaxyCameraDragNDrop.SetActive(false); // Hide the Galaxy Camera Drag and Drop GameObject
        if (diplomacyController.DiplomacyData.CivSideOne <= CivEnum.TERRAN ||
            diplomacyController.DiplomacyData.CivSideTwo <= CivEnum.TERRAN)
        {
            for (int i = 0; i < persistentObjects.Count; i++)
            {
                if (persistentObjects[i] != null && persistentObjects[i].name == "FogPlaneParent")
                {
                    persistentObjects[i].SetActive(false); // Hide the FogPlaneParent object
                }
            }
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
        previousSceneName = "CombatScene";
        ExposeScene("MainMenuScene"); // Re-enable the previous scene
        galaxyCameraDragNDrop.SetActive(true); // Show the Galaxy Camera Drag and Drop GameObject again
        for (int i = 0; i < persistentObjects.Count; i++)
        {
            if (persistentObjects[i] != null && persistentObjects[i].name == "FogPlaneParent")
            {
                persistentObjects[i].SetActive(true); 
            }
        }
        //HideScene(previousSceneName);
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

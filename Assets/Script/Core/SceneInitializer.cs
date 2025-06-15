using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneInitializer : MonoBehaviour
{
    public GameObject prefabToInstantiate; // Assign in Inspector
    public string parentObjectName;        // Name of the GameObject in the new scene

    //private void OnEnable()
    //{
    //    SceneManager.sceneLoaded += OnSceneLoaded;
    //}

    //private void OnDisable()
    //{
    //    SceneManager.sceneLoaded -= OnSceneLoaded;
    //}

    //void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    //{
    //    GameObject parent = GameObject.Find(parentObjectName);

    //    if (parent != null && prefabToInstantiate != null)
    //    {
    //        GameObject instance = Instantiate(prefabToInstantiate);
    //        instance.transform.SetParent(parent.transform, false); // false keeps local transform values
    //    }
    //    else
    //    {
    //        Debug.LogWarning("Parent object or prefab is missing.");
    //    }
    //}
}


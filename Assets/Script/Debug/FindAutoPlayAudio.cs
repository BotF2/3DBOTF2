#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class FindAutoPlayAudio : MonoBehaviour
{
    [MenuItem("Tools/Find Auto-Play AudioSources in Scene")]
    public static void FindAllAutoPlayAudio()
    {
        Debug.Log("=== Searching for Auto-Play AudioSources ===");
        
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        
        int foundCount = 0;
        
        foreach (var rootObj in rootObjects)
        {
            AudioSource[] sources = rootObj.GetComponentsInChildren<AudioSource>(true);
            
            foreach (var source in sources)
            {
                if (source.playOnAwake)
                {
                    foundCount++;
                    Debug.LogWarning($"❌ AUTO-PLAY FOUND: GameObject '{GetPath(source.transform)}' " +
                                   $"Clip: '{source.clip?.name ?? "NONE"}' " +
                                   $"Volume: {source.volume}", source.gameObject);
                }
            }
        }
        
        Debug.Log($"=== Found {foundCount} auto-play AudioSources ===");
    }
    
    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
#endif
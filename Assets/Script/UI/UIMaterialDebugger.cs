using UnityEngine;
using UnityEngine.UI;

public class UIMaterialDebugger : MonoBehaviour
{
    void Start()
    {
        var graphics = FindObjectsByType<Graphic>(FindObjectsSortMode.None);

        foreach (var g in graphics)
        {
            if (g.material != null && g.material.name.Contains("RedGlow"))
            {
                Debug.LogWarning($"❌❌❌❌ UI using A_RedGlow: {g.name}", g);
            }
        }
    }
}

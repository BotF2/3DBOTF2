using UnityEngine;
using UnityEngine.UI;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



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

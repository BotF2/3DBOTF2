using System.Collections;
using UnityEngine;

/// <summary>
/// Small singleton MonoBehaviour that can run coroutines even when other controllers/gameobjects are inactive.
/// Created on first access and marked DontDestroyOnLoad.
/// </summary>
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner instance;
    public static CoroutineRunner Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("CoroutineRunner");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<CoroutineRunner>();
            }
            return instance;
        }
    }
    public void FlashPowerOverload()
    {
        if (StarSysMenuUIController.Instance.PowerOverloadImage == null) return;
        StartCoroutine(FlashRoutine());
    }
    IEnumerator FlashRoutine()
    {
        for (int i = 0; i < 3; i++)
        {
            StarSysMenuUIController.Instance.PowerOverloadImage.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            StarSysMenuUIController.Instance.PowerOverloadImage.SetActive(false);
            yield return new WaitForSeconds(0.5f);
        }
    }
    public Coroutine RunCoroutine(IEnumerator routine)
    {
        if (routine == null) return null;
        return Instance.StartCoroutine(routine);
    }
}

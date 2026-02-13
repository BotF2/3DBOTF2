using Assets.UI;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner instance;
    private GameObject goForFlash;
    public static CoroutineRunner Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("CoroutineRunner");
                instance = go.AddComponent<CoroutineRunner>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ✅ STATIC API — safest usage
    public static void FlashPowerOverload(GameObject goToActivateForFlash)
    {
        Instance.goForFlash = goToActivateForFlash;
        Instance.StartCoroutine(Instance.WaitAndFlash());
    }

    private IEnumerator WaitAndFlash()
    {
        yield return new WaitUntil(() =>
            StarSysMenuUIController.Instance != null); // &&
                                                       //StarSysMenuUIController.Instance.PowerOverloadImage != null);

        yield return FlashRoutine();
    }


    private IEnumerator FlashRoutine()
    {
        if (StarSysMenuUIController.Instance == null)
        {
            Debug.LogWarning("FlashRoutine: StarSysMenuUIController.Instance is null");
            yield break;
        }

        var powerOverload = StarSysMenuUIController.Instance.PowerOverloadImage;

        if (powerOverload == null)
        {
            Debug.LogWarning("FlashRoutine: PowerOverloadImage is null - cannot flash warning");
            yield break;
        }

        for (int i = 0; i < 3; i++)
        {
            powerOverload.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            powerOverload.SetActive(false);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public static Coroutine RunCoroutine(IEnumerator routine)
    {
        if (routine == null) return null;
        return Instance.StartCoroutine(routine);
    }
}

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
        for (int i = 0; i < 3; i++)
        {
            //StarSysMenuUIController.Instance.PowerOverloadImage.SetActive(true);
            goForFlash.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            //StarSysMenuUIController.Instance.PowerOverloadImage.SetActive(false);
            goForFlash.SetActive(false);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public static Coroutine RunCoroutine(IEnumerator routine)
    {
        if (routine == null) return null;
        return Instance.StartCoroutine(routine);
    }
}

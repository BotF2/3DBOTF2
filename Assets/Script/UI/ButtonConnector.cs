using Assets.Core;
using UnityEngine;
using UnityEngine.UI;
using Assets.Core;

public class ButtonConnector : MonoBehaviour
{
    public Button myButton;

    private void Start()
    {
        if (SceneController.Instance != null)
        {
            myButton.onClick.RemoveAllListeners();
            myButton.onClick.AddListener(SceneController.Instance.LoadMainMenuScene);
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }
}

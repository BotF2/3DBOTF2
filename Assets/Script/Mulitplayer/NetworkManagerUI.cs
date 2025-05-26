using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        serverButton.onClick.AddListener(() => { NetworkManager.Singleton.StartServer(); });
        hostButton.onClick.AddListener(() => { NetworkManager.Singleton.StartHost(); });
        clientButton.onClick.AddListener(() => { NetworkManager.Singleton.StartClient(); });
    }
}

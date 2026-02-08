using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    /// <summary>
    /// Frameworks That could support this game well
    /// Unity Netcode for GameObjects(NGO) but more for First-party first person shooter, it is integrated, customizable
    /// *Mirror (open source networking for Unity)  Easier to use, mature, great for RTS
    /// https://mirror-networking.com/
    /// *Fish-Networking ( also networking solution for Unity) High performance, tick-based, flexible
    /// https://fish-networking.gitbook.io/docs
    /// *Photon Fusion (Multiplayer solution) Powerful, good for fast-paced games, may be overkill for 4X
    /// </summary>

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

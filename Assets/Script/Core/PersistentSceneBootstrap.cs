using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Core
{

    public class PersistentSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private string menuSceneName = "MainMenuScene";


        private void Start()
        {
            Debug.Log("=== PersistentSceneBootstrap: Start ===");

            // Verify all critical managers are present
            VerifyManagers();

            // Dedicated headless server (Edgegap deployment, or any -batchmode launch): there's
            // no one present to click Host in the menu, so skip the interactive menu entirely
            // and start listening immediately. Application.isBatchMode is Unity's standard way
            // to detect this at runtime.
            if (Application.isBatchMode)
            {
                StartDedicatedServer();
                return;
            }

            // Check if MenuScene is already loaded
            Scene menuScene = SceneManager.GetSceneByName(menuSceneName);
            if (menuScene.IsValid() && menuScene.isLoaded)
            {
                Debug.Log($"  {menuSceneName} already loaded");
                return;
            }

            // Load MenuScene additively
            Debug.Log($"  Loading {menuSceneName}...");
            SceneManager.LoadSceneAsync(menuSceneName, LoadSceneMode.Additive);
        }

        private void StartDedicatedServer()
        {
            if (NetworkManager.singleton == null)
            {
                Debug.LogError("PersistentSceneBootstrap: -batchmode server cannot start - NetworkManager.singleton is null.");
                return;
            }

            // StartServer() (not StartHost()) - a dedicated server has no local player.
            Debug.Log("PersistentSceneBootstrap: -batchmode detected - starting dedicated server (no local player)...");
            NetworkManager.singleton.StartServer();

            if (NetworkServer.active)
                Debug.Log($"PersistentSceneBootstrap: dedicated server listening on port {(Transport.active is PortTransport pt ? pt.Port.ToString() : "?")}");
            else
                Debug.LogError("PersistentSceneBootstrap: StartServer() returned but NetworkServer.active is false - check for a port conflict or transport error.");
        }

        private void VerifyManagers()
        {
            Debug.Log("  Verifying critical managers:");
            Debug.Log($"    GameController: {(GameController.Instance != null ? "✅" : "❌ NULL")}");
            Debug.Log($"    GameManager: {(GameManager.Instance != null ? "✅" : "❌ NULL")}");

            if (BOTF3D.Audio.AudioManager.Instance != null)
            {
                Debug.Log("    AudioManager: ✅");
            }
            else
            {
                Debug.LogWarning("    AudioManager: ⚠️ NULL (non-critical)");
            }

            if (TimeManager.Instance != null)
            {
                Debug.Log("    TimeManager: ✅");
            }
            else
            {
                Debug.LogWarning("    TimeManager: ⚠️ NULL (non-critical)");
            }
        }
    }
}
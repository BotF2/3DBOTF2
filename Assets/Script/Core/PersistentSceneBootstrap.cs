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
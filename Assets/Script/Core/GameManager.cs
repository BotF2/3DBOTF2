// Ignore Spelling: Sys

using BOTF3D.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BOTF3D.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; } // a static singleton, no other script can instantiate a GameManager, must us the singleton

        public MainMenuUIController mainMenuUIController;
        public GameController GameController;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            Debug.Log($"=== GAME MANAGER BUILD DIAGNOSTICS ===");
            Debug.Log($"Active Scene: {SceneManager.GetActiveScene().name}");
            Debug.Log($"Build Index: {SceneManager.GetActiveScene().buildIndex}");
            Debug.Log($"Total Scenes in Build: {SceneManager.sceneCountInBuildSettings}");

            // List all loaded scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                Debug.Log($"  Loaded Scene [{i}]: {scene.name} (path: {scene.path})");
            }
        }
        private void Start()
        {
            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
            // Verify MainMenuUIController exists
            if (MainMenuUIController.Instance == null)
            {
                Debug.LogError("❌ CRITICAL: MainMenuUIController.Instance is NULL at Start!");
            }
            else
            {
                Debug.Log("✅ MainMenuUIController.Instance found");
            }
        }
        public void SetGameMode(GameMode mode) // single player vs multiplayer, set in the main menu, used by GameController to determine how to run the game
        {
            GameController.Instance.GameData.GameMode = mode;
        }
        public void InitializeGameManagerWithMainMenuUIController()
        {
            // Don't use GameObject.Find - use the singleton Instance instead
            if (MainMenuUIController.Instance != null)
            {
                mainMenuUIController = MainMenuUIController.Instance;
                Debug.Log("GameManager: Found MainMenuUIController via Instance");
            }
            else
            {
                Debug.LogWarning("GameManager: MainMenuUIController.Instance is null - will retry when MainMenu scene loads");
            }
        }

        public void RegisterMainMenu(MainMenuUIController controller)
        {
            mainMenuUIController = controller;
            Debug.Log("GameManager: MainMenuUIController registered.");
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"GameManager: Scene loaded: {scene.name}");

            // When MainMenu scene loads, find the controller
            if (scene.name.Contains("MainMenu") && mainMenuUIController == null)
            {
                InitializeGameManagerWithMainMenuUIController();
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
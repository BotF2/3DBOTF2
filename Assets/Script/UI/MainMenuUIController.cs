using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Assets.Core
{

    public class MainMenuUIController : MonoBehaviour
    {
        public GameObject mainMenuCanvas;
        public GameObject uiCameraGO;
        public GameObject eventSystemGO;
        public GameObject galaxyCenter;

        public GalaxyType selectedGalaxyType;
        public GalaxySize selectedGalaxySize;
        public TechLevel selectedTechLevel;
        public CivEnum selectedCivEnum;

        //private AsyncOperation _SceneAsync;
        //private bool _bGalaxyShow = false;
        //private Scene PrevScene;
        public void SetGalaxySize(int index)
        {
            selectedGalaxySize = (GalaxySize)index;
            //GalaxyContro
        }

        public void SetGalaxyType(int index)
        {
            selectedGalaxyType = (GalaxyType)index;
        }

        public void SetTechLevel(int index)
        {
            selectedTechLevel = (TechLevel)index;
        }

        public void SetCivilization(int index)
        {
            selectedCivEnum = (CivEnum)index;
        }


        public void LoadGalaxyScene()
        {
            mainMenuCanvas.SetActive(false);
            uiCameraGO.SetActive(false);
            eventSystemGO.SetActive(false);
            galaxyCenter.SetActive(true);

            SceneManager.LoadScene("GalaxyScene", LoadSceneMode.Additive);
            CivManager.instance.OnNewGameButtonClicked((int)selectedGalaxySize);
        }

        //IEnumerator loadScene(string SceneName)
        //{
        //    AsyncOperation nScene = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        //    nScene.allowSceneActivation = false;
        //    _SceneAsync = nScene;
        //    while (nScene.progress < 0.9f)
        //    {
        //        Debug.Log("Loading scene " + " [][] Progress: " + nScene.progress);
        //        yield return null;
        //    }

        //    //Activate the Scene
        //    _SceneAsync.allowSceneActivation = true;

        //    while (!nScene.isDone)
        //    {
        //        // wait until it is really finished
        //        yield return null;
        //    }
        //    //Debug.Log("Setting active scene..");
        //    //SceneManager.SetActiveScene(nScene);
        //}
    }
}


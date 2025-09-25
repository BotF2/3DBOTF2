using Assets.Core;
using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class DiplomacyUIController : MonoBehaviour
{
    private Camera galaxyEventCamera;
    [SerializeField]
    private Canvas parentCanvas;
    public DiplomacyController DiplomacyController;
    public GameObject DiplomacyUIToggle; // GameObject controlles this active UI on/off
    [SerializeField]
    private GameObject firstContatct;
    [SerializeField]
    private TMP_Text theirNameTMP;
    [SerializeField]
    private Image theirInsignia;
    [SerializeField]
    private Image theirRaceImage;
    [SerializeField]
    private TMP_Text relationTMP;
    [SerializeField]
    private TMP_Text relationPointsTMP;
    [SerializeField]
    private TMP_Text traitOneTMP;
    [SerializeField]
    private TMP_Text traitTwoTMP;
    [SerializeField]
    private TMP_Text traitThreeTMP;
    [SerializeField]
    private TMP_Text traitFourTMP;
    [SerializeField]
    private TMP_Text ourTraitOneTMP;
    [SerializeField]
    private TMP_Text ourTraitTwoTMP;
    [SerializeField]
    private TMP_Text ourTraitThreeTMP;
    [SerializeField]
    private TMP_Text ourTraitFourTMP;
    [SerializeField]
    private TMP_Text transmissionTMP;
    [SerializeField]
    private TMP_Text descriptionTMP;
    [SerializeField]
    private GameObject descriptionPanel;
    [SerializeField]
    private GameObject[] UI_PanelGOs;
    [SerializeField]
    private Image[] TabButtonMasks;


    private void Start()
    {

    }

    public void LoadDiplomacyUI(DiplomacyController ourDiplomacyController)
    {
        //if (GameController.Instance.AreWeLocalPlayer(ourDiplomacyController.DiplomacyData.CivMajor.CivData.CivEnum))
        //    LoadCivDataInUI(ourDiplomacyController.DiplomacyData.CivOther, ourDiplomacyController); // Fix: Changed 'CivTwo' to 'CivOther'
        //else if (GameController.Instance.AreWeLocalPlayer(ourDiplomacyController.DiplomacyData.CivOther.CivData.CivEnum))
        //    LoadCivDataInUI(ourDiplomacyController.DiplomacyData.CivMajor, ourDiplomacyController); // Fix: Changed 'CivOne' to 'CivMajor'
    }

    private void LoadCivDataInUI(CivController othersController, DiplomacyController ourDiplomacyController)
    {
        // Implementation for loading civilization data into the UI.
        // This method was missing, causing CS0103.
    }

    public void CloseUnLoadDiplomacyUI()
    {
        // Existing code...
    }

    public void OpenCloseDescritionPanel()
    {
        // Existing code...
    }

    public void CombatScene()
    {
        // Existing code...
    }

    // Existing code...
}

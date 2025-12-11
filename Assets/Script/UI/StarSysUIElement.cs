using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarSysUIElement : MonoBehaviour
{
    [Header("Misc")]
    public RectTransform redDot;
    //public RectTransform cancelShipManagerButton;
    public RectTransform shipContent;
    [Header("Buttons")]
    public Button buildButton;
    public Button shipButton;
    public Button factoryButtonOn;
    public Button factoryButtonOff;
    public Button yardButtonOn;
    public Button yardButtonOff;
    public Button shieldButtonOn;
    public Button shieldButtonOff;
    public Button oBButtonOn;
    public Button oBButtonOff;
    public Button researchButtonOn;
    public Button researchButtonOff;
    public Button newFleetButton;
    public Button mergeFleetButton;
    public Button shipDeployButton;
    public Button cancelShipManagerButton;

    [Header("Text")]
    public TextMeshProUGUI sysName;
    public TextMeshProUGUI headerPowerUnitText;
    public TextMeshProUGUI numPUnits;
    public TextMeshProUGUI numTotalEOut;
    public TextMeshProUGUI numPLoad;
    public TextMeshProUGUI nameFactory;
    public TextMeshProUGUI numFactoryRatio;
    public TextMeshProUGUI factoryLoad;
    public TextMeshProUGUI shipyardName;
    public TextMeshProUGUI numYardsOnRatio;
    public TextMeshProUGUI yardLoad;
    public TextMeshProUGUI shieldName;
    public TextMeshProUGUI numShieldsRatio;
    public TextMeshProUGUI shieldLoad;
    public TextMeshProUGUI oBName;
    public TextMeshProUGUI numOBRatio;
    public TextMeshProUGUI oBLoad;
    public TextMeshProUGUI researchName;
    public TextMeshProUGUI numResearchRatio;
    public TextMeshProUGUI researchLoad;
    public TextMeshProUGUI powerOvarload;

    [Header("Images")]
    public Image powerUnitImage;
    public Image factoryImage;
    public Image shipyardImage;
    public Image shieldPlantImage;
    public Image orbitalBatteriesImage;
    public Image researchImage;
    public Image powerOverLoad;

    [Header("Sliders")]
    public Slider buildProgressSlider;
    public Slider shipBuildProgressSlider;

}

// Ignore Spelling: Sys

using Assets.Core;
using UnityEngine;

public class SysButtonOnOff : MonoBehaviour
{
    public SystemOnOffButtons button;

    public void OnButtonClicked()
    {
        switch (button)
        {
            case SystemOnOffButtons.FactoryOnButton:
                // Add logic for FactoryOn
                break;
            case SystemOnOffButtons.FactoryOffButton:
                // Add logic for FactoryOff
                break;
            case SystemOnOffButtons.ShipyardOnButton:
                break;
            case SystemOnOffButtons.ShipyardOffbutton:
                break;
            case SystemOnOffButtons.PowerPlantOnButton:
                break;
            case SystemOnOffButtons.PowerPlantOffButton:
                break;
            case SystemOnOffButtons.ShieldGeneratorOnButton:
                break;
            case SystemOnOffButtons.ShieldGeneratorOffbutton:
                break;
            case SystemOnOffButtons.OrbitalBatteryOnButton:
                break;
            case SystemOnOffButtons.OrbitalBatteryOffButton:
                break;
            case SystemOnOffButtons.ResearchCenterOnButton:
                break;
            case SystemOnOffButtons.ResearchCenterOffButton:
                break;

            // Add other cases as needed
            default:
                break;
        }
    }
}

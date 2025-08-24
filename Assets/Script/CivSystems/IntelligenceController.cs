using UnityEngine;
using Assets.Core;

public class IntelligenceController
{
    public IntelligenceData IntelligenceData;

    public GameObject IntelligenceUIGameObject; //The instantiated UI for this civ pair. a prefab clone, not a class but a game object
                                             // instantiated by DiplomacyManager from a prefab and added to DiplomacyController
    public IntelligenceController(IntelligenceData intelligenceData)
    {
        IntelligenceData = intelligenceData;
    }
}

using System;
using TMPro;
using UnityEngine;
using Mirror;

namespace Assets.Core
{
    /// <summary>
    /// Controlling a Civilization(faction) with the matching CivData class
    /// civData holds key info on status including for save game
    /// </summary>
    public class CivController : MonoBehaviour
    {
        //Fields
        private CivManager _manager;
        public CivData CivData;
        public string CivShortName;
        public static event Action<TrekStardateEventSO> TrekEventStardate;
        private DiplomacyManager diplomacyManager;

        public void Start()
        {
            TimeManager.Instance.OnStardateSpecialEvent = DoStardateEvent;
            diplomacyManager = DiplomacyManager.Instance;
        }
        public void Init(CivManager manager)
        {
            _manager = manager;
            //CivData = new CivData();
            //CivData.Init(this);
        }
        public void DoStardateEvent(TrekStardateEventSO specialEvent)
        {
            if (specialEvent != null)
            {
                Debug.Log("Special event reached StarSystemController: " + specialEvent.eventName + " on oneInXChance " +
                    specialEvent.stardate + " TrekRandomEvents: " + specialEvent.trekEventType +
                    " parameter: " + specialEvent.eventParameter);
                // Add your logic to handle the special event here
                switch (specialEvent.trekEventType)
                {
                    case TrekStardateEvents.FederartionEst:
                        {
                            // ToDo: Do Disaster code for each disaster 
                            Debug.Log("******** FedLocalPalyerToggle Established ***********"); ;
                            break;
                        }
                    case TrekStardateEvents.RomulanNeutralZoneEst:
                        {
                            Debug.Log("********** RomLocalPlayerToggle Neutral Zone established **********");
                            break;
                        }
                    case TrekStardateEvents.KhitomerRomulanAttack:
                        {
                            Debug.Log("********** Khitomer RomLocalPlayerToggle Attack **********");
                            break;
                        }
                    case TrekStardateEvents.QandTheBorg:
                        {
                            Debug.Log("********** Q and BorgLocalPlayerToggle **********");
                            break;
                        }
                    default:
                        break;
                }
            }
        }
    }
}

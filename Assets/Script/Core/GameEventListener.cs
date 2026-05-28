using UnityEngine;
using UnityEngine.Events;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



namespace BOTF3D.Core
{
    [System.Serializable]
    public class CustomGameEvent : UnityEvent<Component, object>
    {

    }
    public class GameEventListener : MonoBehaviour
    {
        //public TrekRandomEventSO trekEventSO;
        public UnityEvent response; // link method calls in editor by setting to gameobject, A Unity Event

        private void Start()
        {
            TimeManager.Instance.OnRandomSpecialEvent = Instance_OnRandomEvent;
        }
        private void OnEnable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnRandomSpecialEvent += Instance_OnRandomEvent;
        }

        private void Instance_OnRandomEvent(TrekRandomEventSO specialEvent)
        {
            #region Not here, it is sent to TimeManager
            //if (specialEvent != null)
            //{
            //    Debug.Log("Special event reached: " + specialEvent.eventName + " on oneInXChance " +
            //        specialEvent.oneInXChance + " TrekRandomEvents: " + specialEvent.trekEventType +
            //        " parameter: " + specialEvent.eventParameter);
            //    // Add your logic to handle the special event here
            //    switch (specialEvent.trekEventType)
            //    {
            //        case TrekRandomEvents.AsteroidHit:
            //            {
            //                //CALL METHOD FOR ASTEROID HIT HERE
            //                Debug.Log("******** Asteroid ***********"); ;
            //                break;
            //            }
            //        case TrekRandomEvents.Pandemic:
            //            {
            //                Debug.Log("********** PANDEMIC **********");
            //                break;
            //            }
            //        case TrekRandomEvents.SuperVolcano:
            //            {
            //                Debug.Log("********** SUPER VOLCANO **********");
            //                break;
            //            }
            //        case TrekRandomEvents.GamaRayBurst:
            //            {
            //                Debug.Log("********** GAMERAY BURST **********");
            //                break;
            //            }
            //        case TrekRandomEvents.SeismicEvent:
            //            {
            //                Debug.Log("********** SEISMEIC EVENT **********");
            //                break;
            //            }
            //        case TrekRandomEvents.Teribals:
            //            {
            //                break;
            //            }
            //        case TrekRandomEvents.RemoveTempTargets:
            //            {
            //                Debug.Log("********** REMOVE TEMP TARGET **********");
            //                break;
            //            }
            //        default:
            //            break;
            //    }
            //}
            #endregion
        }

        private void OnDisable()
        {
            TimeManager.Instance.OnRandomSpecialEvent -= Instance_OnRandomEvent;
        }
        public void OnEventRaised(Component sender, object data)
        {
            response.Invoke();
        }
    }

}


using UnityEngine;

public enum TrekRandomEvents { AsteroidHit, Pandemic, SuperVolcano, GamaRayBurst, SeismicEvent, Teribals }


[CreateAssetMenu(menuName = "Game Event/Random Trek Event")]
public class TrekRandomEventSO : ScriptableObject
{
    public string eventName;
    public int oneInXChance; // one in X chance of the event
    public TrekRandomEvents trekEventType;

    public string eventParameter;

}

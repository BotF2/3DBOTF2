using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



public enum TrekStardateEvents { QandTheBorg, FederartionEst, RomulanNeutralZoneEst, KhitomerRomulanAttack }

[CreateAssetMenu(menuName = "Game Event/Stardate Trek Event")]
public class TrekStardateEventSO : ScriptableObject
{
    public string eventName;
    public int stardate; // oneInXChance of the event
    public TrekStardateEvents trekEventType;

    public string eventParameter;
}

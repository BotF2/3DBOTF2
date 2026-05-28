
using BOTF3D.Core;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.UI;
using BOTF3D.Audio;



[CreateAssetMenu(menuName = "Galaxy/PlayerTargetSO")]
public class PlayerDefinedTargetSO : ScriptableObject
{

    public int CivIndex;
    public Sprite Insignia;
    public CivEnum CivOwnerEnum;
    public string Name;
    public string Description;
}

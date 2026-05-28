using BOTF3D.Combat;
using BOTF3D.Core;

using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.UI;
using BOTF3D.Audio;



public interface IPlayerController
{
    GamePlayerInfo PlayerInfo { get; set; }
    CivEnum PlayerCiv { get; }
    bool controllerIsLocalPlayer { get; }
    string PlayerName { get; }

    void GiveCombatOrder(CombatOrders order, CombatController combatCon, CivEnum civ);
    //void GiveCombatOrder(CombatOrders order, CombatController combatController, CivEnum civEnumLocalPlayer);
    void GiveDiplomacyOrder(NegotiationPloysEnum order, DiplomacyController diploCon, CivEnum civ);
    void GiveIntelOrder(SecretActionsEnum order, CivEnum civ);
    //......more orders as needed

}

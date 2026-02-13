using Assets.Core;
using Assets.GamePlay;

public interface IPlayerController
{
    GamePlayerInfo PlayerInfo { get; set; }
    CivEnum PlayerCiv { get; }
    bool controllerIsLocalPlayer { get; }
    string PlayerName { get; }

    void GiveCombatOrder(CombatOrders order, CombatController combatCon, CivEnum civ);
    void GiveDiplomacyOrder(NegotiationPloysEnum order, DiplomacyController diploCon, CivEnum civ);
    void GiveIntelOrder(SecretActionsEnum order, CivEnum civ);
    //......more orders as needed

}

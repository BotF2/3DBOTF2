using System;

namespace BOTF3D.Core
{
    /// <summary>
    /// Central event system for game-wide communication
    /// Reduces coupling between managers and controllers
    /// Usage: Subscribe in OnEnable, unsubscribe in OnDisable
    /// </summary>
    public static class GameEvents
    {
        #region Combat Events

        /// <summary>
        /// Fired when a new combat encounter begins
        /// Pass combat ID for listeners to look up data
        /// </summary>
        public static event Action<int> OnCombatStarted; // combatID

        /// <summary>
        /// Fired when combat ends with a victor
        /// </summary>
        public static event Action<CivEnum> OnCombatEnded;

        /// <summary>
        /// Fired when a ship is destroyed in combat
        /// </summary>
        public static event Action<int> OnShipDestroyed; // shipID

        public static void CombatStarted(int combatID) => OnCombatStarted?.Invoke(combatID);
        public static void CombatEnded(CivEnum victor) => OnCombatEnded?.Invoke(victor);
        public static void ShipDestroyed(int shipID) => OnShipDestroyed?.Invoke(shipID);

        #endregion

        #region Civilization Events

        /// <summary>
        /// Fired when a new civilization is created/instantiated
        /// </summary>
        public static event Action<CivEnum> OnCivCreated;

        /// <summary>
        /// Fired when diplomatic relations change between two civs
        /// </summary>
        public static event Action<CivEnum, CivEnum, DiplomaticState> OnDiplomacyChanged;

        /// <summary>
        /// Fired when a civilization is eliminated from the game
        /// </summary>
        public static event Action<CivEnum> OnCivEliminated;

        public static void CivCreated(CivEnum civ) => OnCivCreated?.Invoke(civ);
        public static void DiplomacyChanged(CivEnum civ1, CivEnum civ2, DiplomaticState newState) => OnDiplomacyChanged?.Invoke(civ1, civ2, newState);
        public static void CivEliminated(CivEnum civ) => OnCivEliminated?.Invoke(civ);

        #endregion

        #region Galaxy Events

        /// <summary>
        /// Fired when a star system ownership changes
        /// </summary>
        public static event Action<string, CivEnum, CivEnum> OnSystemOwnershipChanged; // systemName, previousOwner, newOwner

        /// <summary>
        /// Fired when a fleet moves to a new location
        /// </summary>
        public static event Action<int> OnFleetMoved; // fleetID

        public static void SystemOwnershipChanged(string systemName, CivEnum previousOwner, CivEnum newOwner) => OnSystemOwnershipChanged?.Invoke(systemName, previousOwner, newOwner);
        public static void FleetMoved(int fleetID) => OnFleetMoved?.Invoke(fleetID);

        #endregion

        #region Game State Events

        /// <summary>
        /// Fired when game is saved
        /// </summary>
        public static event Action OnGameSaved;

        /// <summary>
        /// Fired when game is loaded
        /// </summary>
        public static event Action OnGameLoaded;

        /// <summary>
        /// Fired when a new turn begins
        /// </summary>
        public static event Action<int> OnNewTurn; // turnNumber

        public static void GameSaved() => OnGameSaved?.Invoke();
        public static void GameLoaded() => OnGameLoaded?.Invoke();
        public static void NewTurn(int turnNumber) => OnNewTurn?.Invoke(turnNumber);

        #endregion

        /// <summary>
        /// Clear all event subscriptions - useful for scene transitions
        /// WARNING: Only call this during cleanup/scene unload
        /// </summary>
        public static void ClearAllEvents()
        {
            OnCombatStarted = null;
            OnCombatEnded = null;
            OnShipDestroyed = null;
            OnCivCreated = null;
            OnDiplomacyChanged = null;
            OnCivEliminated = null;
            OnSystemOwnershipChanged = null;
            OnFleetMoved = null;
            OnGameSaved = null;
            OnGameLoaded = null;
            OnNewTurn = null;
        }
    }

    // Supporting enums
    public enum DiplomaticState
    {
        War,
        Neutral,
        Peace,
        Allied
    }
}

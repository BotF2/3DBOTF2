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
        /// Fired when combat ends with a victor and a defeated civ
        /// </summary>
        public static event Action<CivEnum, CivEnum> OnCombatEnded; // winner, loser

        /// <summary>
        /// Fired when a ship is destroyed in combat
        /// </summary>
        public static event Action<int> OnShipDestroyed; // shipID

        public static void CombatStarted(int combatID) => OnCombatStarted?.Invoke(combatID);
        public static void CombatEnded(CivEnum winner, CivEnum loser) => OnCombatEnded?.Invoke(winner, loser);
        public static void ShipDestroyed(int shipID) => OnShipDestroyed?.Invoke(shipID);

        #endregion

        #region Civilization Events

        /// <summary>
        /// Fired when a new civilization is created/instantiated
        /// </summary>
        public static event Action<CivEnum> OnCivCreated;

        /// <summary>
        /// Fired when the diplomatic status between two civs changes (DiplomacyStatusEnum, not a
        /// coarse War/Peace summary - reports/UI can decide how much detail to surface)
        /// </summary>
        public static event Action<CivEnum, CivEnum, DiplomacyStatusEnum> OnDiplomacyChanged;

        /// <summary>
        /// Fired when a civilization is eliminated from the game (absorbed into another civ)
        /// </summary>
        public static event Action<CivEnum, CivEnum> OnCivEliminated; // eliminatedCiv, absorbedByCiv

        /// <summary>
        /// Fired once by CivManager.CheckForEliminatedCivs when a PLAYABLE civ is found to own zero
        /// star systems and zero fleets - unlike OnCivEliminated (a minor being absorbed by a
        /// major), there is no "winner" of this event; the civ simply has nothing left to command.
        /// See CivData.IsEliminated and TimeManager's auto-ready handling of eliminated civs.
        /// </summary>
        public static event Action<CivEnum> OnPlayableCivDefeated;

        /// <summary>
        /// Fired once by CivManager.CheckForVictoryCondition when a playable civ's owned-system
        /// count crosses the galaxy-wide victory threshold (a third of all systems in play - see
        /// CivManager.VictorySystemFraction). Game-ending: CivManager.GameHasEnded is set true in
        /// the same call, and TimeManager refuses any further AdvanceTurn once that flag is set.
        /// </summary>
        public static event Action<CivEnum, int, int> OnGameVictory; // victoriousCiv, systemsOwned, totalSystems

        public static void PlayableCivDefeated(CivEnum civ) => OnPlayableCivDefeated?.Invoke(civ);
        public static void GameVictory(CivEnum victor, int systemsOwned, int totalSystems) => OnGameVictory?.Invoke(victor, systemsOwned, totalSystems);

        /// <summary>
        /// Fired for each third-party civ whose relationship with `actor` shifted as a ripple effect
        /// of something `actor` did to/with `causingTarget` (see DiplomacyManager.ApplyDiplomaticRipple).
        /// Only fired once the ripple's target controller (actor to thirdParty) is confirmed to exist
        /// (i.e. they've had contact), so listeners never need their own contact-record gate.
        /// </summary>
        public static event Action<CivEnum, CivEnum, CivEnum, int, DiplomaticEventEnum> OnDiplomaticRipple; // actor, thirdParty, causingTarget, pointDelta, eventType

        public static void CivCreated(CivEnum civ) => OnCivCreated?.Invoke(civ);
        public static void DiplomacyChanged(CivEnum civ1, CivEnum civ2, DiplomacyStatusEnum newStatus) => OnDiplomacyChanged?.Invoke(civ1, civ2, newStatus);
        public static void CivEliminated(CivEnum eliminatedCiv, CivEnum absorbedByCiv) => OnCivEliminated?.Invoke(eliminatedCiv, absorbedByCiv);
        public static void DiplomaticRipple(CivEnum actor, CivEnum thirdParty, CivEnum causingTarget, int pointDelta, DiplomaticEventEnum eventType)
            => OnDiplomaticRipple?.Invoke(actor, thirdParty, causingTarget, pointDelta, eventType);

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

        /// <summary>
        /// Fired when a star system's habitability flips (e.g. terraforming completes)
        /// </summary>
        public static event Action<string, bool> OnSystemHabitabilityChanged; // systemName, isHabitable

        public static void SystemOwnershipChanged(string systemName, CivEnum previousOwner, CivEnum newOwner) => OnSystemOwnershipChanged?.Invoke(systemName, previousOwner, newOwner);
        public static void FleetMoved(int fleetID) => OnFleetMoved?.Invoke(fleetID);
        public static void SystemHabitabilityChanged(string systemName, bool isHabitable) => OnSystemHabitabilityChanged?.Invoke(systemName, isHabitable);

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

        /// <summary>
        /// Fired when the InterTurn event queue finishes draining — all queued contact events have
        /// been presented to the player. GameControlOverlay uses this to re-enable the Advance Turn
        /// button (see TurnEventQueue.StartDraining / DrainCoroutine).
        /// </summary>
        public static event Action OnTurnEventQueueDrained;

        public static void GameSaved() => OnGameSaved?.Invoke();
        public static void GameLoaded() => OnGameLoaded?.Invoke();
        public static void NewTurn(int turnNumber) => OnNewTurn?.Invoke(turnNumber);
        public static void TurnEventQueueDrained() => OnTurnEventQueueDrained?.Invoke();

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
            OnPlayableCivDefeated = null;
            OnGameVictory = null;
            OnDiplomaticRipple = null;
            OnSystemOwnershipChanged = null;
            OnFleetMoved = null;
            OnGameSaved = null;
            OnGameLoaded = null;
            OnNewTurn = null;
            OnTurnEventQueueDrained = null;
        }
    }
}

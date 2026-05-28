namespace BOTF3D.Core
{
    /// <summary>
    /// Interface for all Data classes (serialization/persistence)
    /// Data classes hold state that needs to be saved/loaded
    /// </summary>
    public interface IGameData
    {
        /// <summary>
        /// Prepare data for serialization and save to persistent storage
        /// </summary>
        void SaveState();

        /// <summary>
        /// Load data from persistent storage and restore state
        /// </summary>
        void LoadState();

        /// <summary>
        /// Validate that the data is in a consistent state
        /// </summary>
        bool ValidateData();
    }
}

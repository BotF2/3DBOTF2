namespace BOTF3D.Core
{
    /// <summary>
    /// Interface for all Manager classes (singleton pattern managers)
    /// Provides standard lifecycle methods for initialization and cleanup
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// Initialize the manager - called during Awake/Start
        /// </summary>
        void Initialize();

        /// <summary>
        /// Cleanup resources when manager is destroyed
        /// </summary>
        void Cleanup();
    }
}

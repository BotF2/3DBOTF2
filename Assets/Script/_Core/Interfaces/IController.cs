namespace BOTF3D.Core
{
    /// <summary>
    /// Interface for all Controller classes (game object logic)
    /// Controllers manage individual entity behavior
    /// </summary>
    public interface IController
    {
        /// <summary>
        /// Initialize the controller with required data
        /// </summary>
        void Initialize();

        /// <summary>
        /// Update the controller state - called each frame or as needed
        /// </summary>
        void UpdateState();
    }
}

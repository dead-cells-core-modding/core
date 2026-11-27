namespace ModCore.Events.Interfaces.Game.Save
{
    /// <summary>
    /// An event triggered when deleting a save
    /// </summary>
    [Event]
    public interface IOnDeleteSave
    {
        /// <summary>
        /// Raised when a save is deleted.
        /// </summary>
        /// <param name="slot">The save slot being deleted, or <see langword="null"/> to delete all slots.</param>
        void OnDeleteSave( int? slot );
    }
}

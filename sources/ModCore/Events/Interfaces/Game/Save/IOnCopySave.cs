namespace ModCore.Events.Interfaces.Game.Save
{
    /// <summary>
    /// An event is triggered when a save is moved
    /// </summary>
    [Event]
    public interface IOnCopySave
    {
        /// <summary>
        /// Contains the source and destination save slots.
        /// </summary>
        /// <param name="SlotFrom">Original save slot ID</param>
        /// <param name="SlotTo">Destination save slot id</param>
        public record class EventData( int SlotFrom, int SlotTo );
        /// <summary>
        /// Raised when a save is copied.
        /// </summary>
        /// <param name="data">The copy event data.</param>
        void OnCopySave( EventData data );
    }
}

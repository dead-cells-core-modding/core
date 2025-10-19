namespace ModCore.Events
{
    /// <summary>
    /// All event receivers should implement this interface
    /// </summary>
    public interface IEventReceiver
    {
        /// <summary>
        /// The priority of the event receiver
        /// </summary>
        int Priority
        {
            get;
        }
    }
}

namespace ModCore.Events
{
    /// <summary>
    /// All events should contain this attribute.
    /// </summary>
    /// <param name="once">A value indicating whether the event is one-time.</param>
    [AttributeUsage(AttributeTargets.Interface)]
    public class EventAttribute( bool once = false ) : Attribute
    {
        /// <summary>
        /// A value indicating whether the event is one-time.
        /// </summary>
        public bool Once { get; } = once;
    }
}

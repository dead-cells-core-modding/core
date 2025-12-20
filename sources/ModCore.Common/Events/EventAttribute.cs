namespace ModCore.Events
{
    /// <summary>
    /// All events should contain this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class EventAttribute: Attribute
    {
        [Flags]
        public enum EventKind
        {
            None = 0,
            Once = 1,
            ShowInLog = 2
        }
        public EventAttribute(bool once ) : this(once ? (EventKind.Once | EventKind.ShowInLog) : EventKind.None)
        {
        }
        public EventAttribute( EventKind kind = EventKind.None )
        {
            Kind = kind;
        }
        public EventKind Kind
        {
            get; 
        }
        /// <summary>
        /// A value indicating whether the event is one-time.
        /// </summary>
        public bool Once => (Kind & EventKind.Once) == EventKind.Once;
    }
}

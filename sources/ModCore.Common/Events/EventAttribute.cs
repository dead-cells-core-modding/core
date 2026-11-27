namespace ModCore.Events
{
    /// <summary>
    /// All events should contain this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class EventAttribute : Attribute
    {
        /// <summary>
        /// Describes how an event is dispatched.
        /// </summary>
        [Flags]
        public enum EventKind
        {
            /// <summary>
            /// No special behavior.
            /// </summary>
            None = 0,
            /// <summary>
            /// The event can only be raised once.
            /// </summary>
            Once = 1,
            /// <summary>
            /// The event is written to the log when raised.
            /// </summary>
            ShowInLog = 2
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="EventAttribute"/> class.
        /// </summary>
        /// <param name="once">Whether the event can only be raised once.</param>
        public EventAttribute( bool once ) : this(once ? (EventKind.Once | EventKind.ShowInLog) : EventKind.None)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="EventAttribute"/> class.
        /// </summary>
        /// <param name="kind">The event dispatch behavior.</param>
        public EventAttribute( EventKind kind = EventKind.None )
        {
            Kind = kind;
        }
        /// <summary>
        /// Gets the event dispatch behavior.
        /// </summary>
        public EventKind Kind {
            get;
        }
        /// <summary>
        /// A value indicating whether the event is one-time.
        /// </summary>
        public bool Once => (Kind & EventKind.Once) == EventKind.Once;
    }
}

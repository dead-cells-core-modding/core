namespace ModCore.Events
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class EventAttribute( bool once = false ) : Attribute
    {
        public bool Once { get; } = once;
    }
}

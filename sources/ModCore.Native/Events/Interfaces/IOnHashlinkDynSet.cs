using ModCore.Events;

namespace ModCore.Native.Events.Interfaces
{
    [Event]
    internal interface IOnHashlinkDynSet
    {
        public record struct Data( nint ptr, int hfield, object val, nint? extraTypePtr );
        public EventResult<bool> OnHashlinkDynSet( Data data );
    }
}

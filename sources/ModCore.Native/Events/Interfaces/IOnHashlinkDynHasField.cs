using ModCore.Events;

namespace ModCore.Native.Events.Interfaces
{
    [Event]
    internal interface IOnHashlinkDynHasField
    {
        public record struct Data( nint ptr, int hfield );
        public EventResult<bool> OnHashlinkDynHasField( Data data );
    }
}

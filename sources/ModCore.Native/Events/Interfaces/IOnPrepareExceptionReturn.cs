using ModCore.Events;

namespace ModCore.Native.Events.Interfaces
{
    [Event]
    internal interface IOnPrepareExceptionReturn
    {
        public EventResult<nint> OnPrepareExceptionReturn( nint data );
    }
}

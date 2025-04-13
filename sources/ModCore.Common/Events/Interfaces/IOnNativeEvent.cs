using System.Runtime.InteropServices;

namespace ModCore.Events.Interfaces
{
    [Event]
    public unsafe interface IOnNativeEvent
    {
        public enum EventId
        {
            HL_EV_BEGORE_GC = 1,
            HL_EV_AFTER_GC = 2,
            HL_EV_VM_READY = 3,
            HL_EV_GC_CALL_FINALIZER = 4,
            HL_EV_GC_FREE_PAGE = 5,
            HL_EV_GC_BEFORE_MARK = 6,
            HL_EV_GC_AFTER_MARK = 7,
            [Obsolete] HL_EV_GC_CS_NO_MARKED = 8,
            HL_EV_ERR_NET_CAUGHT = 9,
            HL_EV_START_GAME = 10,
            HL_EV_RESOLVE_NATIVE = 11,
            HL_EV_GC_SEARCH_ROOT = 12
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Event_gc_roots
        {
            public void** roots;
            public int nroots;
        }
        
        public record class Event( EventId EventId, nint Data );
        public void OnNativeEvent( Event ev );
    }
}

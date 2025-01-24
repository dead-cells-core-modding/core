using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            HL_EV_GC_CS_NO_MARKED = 8,
            HL_EV_ERR_NET_CAUGHT = 9,
            HL_EV_START_GAME = 10
        }
        public record class Event(EventId EventId, nint Data);
        public void OnNativeEvent(Event ev);
    }
}

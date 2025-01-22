using ModCore.Events.Interfaces;
using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    internal unsafe static class NativeExport
    {



        public enum HashlinkEvent
        {
            BeforeGC = 1,
            AfterGC = 2,

            HashlinkVMReady = 3,
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        public static void OnHLEvent(HashlinkEvent eventId, void* data)
        {
            if (eventId == HashlinkEvent.BeforeGC)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            else if (eventId == HashlinkEvent.AfterGC)
            {

            }
            else if (eventId == HashlinkEvent.HashlinkVMReady)
            {
                EventSystem.BroadcastEvent<IOnHashlinkVMReady>();
            }
        }
    }
}

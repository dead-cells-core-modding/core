using Hashlink;
using ModCore.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hashlink
{
    public unsafe static class VMTrace
    {

        private class CallFromHLTrace : IDisposable
        {
            public void Dispose()
            {
                
            }
        }

        public static IDisposable CallFromHL()
        {
            var trace = new CallFromHLTrace();

            var threadinfo = HashlinkNative.hl_get_thread();
            int a = 0;

            var curStack = Unsafe.AsPointer(ref a);

            return trace;
        }
    }
}

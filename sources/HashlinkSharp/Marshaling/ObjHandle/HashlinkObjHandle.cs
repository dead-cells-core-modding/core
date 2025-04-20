using Hashlink.Proxy;
using ModCore.Events;
using ModCore.Events.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hashlink.Marshaling.ObjHandle
{
    public unsafe class HashlinkObjHandle
    {
        private HashlinkObj? obj;

        internal int handleIndex = 0;
        internal readonly nint hlPtr;
        internal HashlinkObjHandle( nint objPtr, int index )
        {
            hlPtr = objPtr;
            handleIndex = index;
        }
        public HashlinkObj? Target
        {
            get => obj;
            set
            {
                obj = value;
            }
        }
    }
}

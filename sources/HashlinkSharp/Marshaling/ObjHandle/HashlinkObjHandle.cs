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
        private bool isStateless = true;

        internal int handleIndex = 0;
        internal readonly nint nativeHLPtr;
        internal HashlinkObjHandle( nint objPtr, int index )
        {
            nativeHLPtr = objPtr;
            handleIndex = index;
        }

        public bool IsStateless
        {
            get => isStateless;
            set
            {
                if (isStateless != value)
                {
                    if (!isStateless)
                    {
                        throw new InvalidOperationException();
                    }
                    isStateless = value;
                    _ = HashlinkObjManager.GetHandle(nativeHLPtr);
                }
            }
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

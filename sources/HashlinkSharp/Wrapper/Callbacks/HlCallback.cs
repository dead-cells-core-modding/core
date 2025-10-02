using Hashlink.UnsafeUtilities;
using ModCore.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Wrapper.Callbacks
{
    public unsafe class HlCallback
    {
        private nint routerPtr;
        private Delegate? callback;
        private Delegate? target;
        private readonly MethodInfo callbackMI;
        private readonly HlCallbackInfo info = new();
        internal HlCallback(MethodInfo callbackMI)
        {
            this.callbackMI = callbackMI;
            info.callback = this;
        }

        public nint RedirectTarget
        {
            get => info.directRoute;
            set => info.directRoute = value;
        }

        public Delegate? Target
        {
            get => target;
            set => info.entry = new(target = value!);
        }

        private static void PatchEntry( nint ptr )
        {
            var helperPtr = (ulong*)(ptr + 10 + 2);

            Debug.Assert(Native.Current.Data->dotnet_runtime_pinvoke_wrapper == 0 ||
                        (ulong)Native.Current.Data->dotnet_runtime_pinvoke_wrapper == * helperPtr);

            Native.Current.Data->dotnet_runtime_pinvoke_wrapper = (nint)(*helperPtr);

            Native.Current.MakePageWritable(ptr, out var oldFlag);

            *helperPtr = (ulong)Native.Current.asm_hl2cs_store_return_ptr;

            Native.Current.RestorePageProtect(ptr, oldFlag);
        }

        public nint NativePointer
        {
            get
            {
                if (routerPtr == 0)
                {
                    callback = callbackMI.CreateAnonymousDelegate(info, true);
                    routerPtr = Marshal.GetFunctionPointerForDelegate(callback);

                    PatchEntry(routerPtr);
                }
                return routerPtr;
            }
        }
    }
}

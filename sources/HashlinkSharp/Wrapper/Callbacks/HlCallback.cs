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
using static Hashlink.Wrapper.WrapperHelper;

namespace Hashlink.Wrapper.Callbacks
{
    public unsafe class HlCallback
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PreCode
        {
            public static readonly byte[] call_code_x64 = [
                0x48, 0xB8, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, //mov rax, 0xffffffffffffffff
                0xFF, 0xD0 //call rax
             ];
            public fixed byte shellcode[12];
            public nint realTarget;
        }
        private static readonly ExecutableMemoryManager<PreCode> memoryManager = new();

        private ExecutableMemoryManager<PreCode>.Cell* precode;
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

        public nint NativePointer
        {
            get
            {
                if (routerPtr == 0)
                {
                    callback = callbackMI.CreateAnonymousDelegate(info, true);

                    precode = memoryManager.Alloc();
                    PreCode.call_code_x64.CopyTo(new Span<byte>(precode->value.shellcode, 12));
                    *(long*)&precode->value.shellcode[2] = Native.Current.asm_hl2cs_store_return_ptr;
                    precode->value.realTarget = Marshal.GetFunctionPointerForDelegate(callback);

                    routerPtr = (nint)precode->value.shellcode;
                }
                return routerPtr;
            }
        }
    }
}

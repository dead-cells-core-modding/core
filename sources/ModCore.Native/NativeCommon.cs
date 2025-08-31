using Hashlink;
using ModCore.Events;
using ModCore.Events.Interfaces;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static Hashlink.HashlinkNative;

namespace ModCore.Native
{
    internal unsafe static class NativeCommon
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct VMContext
        {
            public HL_code* code;
            public HL_module* m;
            public HL_vdynamic* ret;
            public HL_vclosure c;
        }

        private static void NativeEventHandler(int eventId, nint data)
        {
            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                   new((IOnNativeEvent.EventId)eventId, data));

        }
        private static readonly NativeEventHandleDelegate del_NativeEventHandler = NativeEventHandler;
        public static void InitGame(ReadOnlySpan<byte> hlboot, out VMContext context)
        {
            HL_code* code;
            byte* err;
            context = new();
            var ctx = (VMContext*) Unsafe.AsPointer(ref context);

            //

            hl_event_set_handler(del_NativeEventHandler);

            //

            hl_global_init();
            fixed (byte* data = hlboot)
            {
                ctx->code = code = (HL_code*) hl_code_read(data, hlboot.Length, &err);
            }
           
            if (err != null)
            {
                throw new InvalidProgramException($"An error occurred while loading bytecode: {Marshal.PtrToStringAnsi((nint)err)}");
            }

            hl_sys_init((void**)Marshal.StringToHGlobalAnsi(""), 0,
                (void*)Marshal.StringToHGlobalAnsi("hlboot.dat"));
            hl_register_thread(ctx);
            ctx->m = hl_module_alloc(code);
            if (ctx->m == null)
            {
                throw new InvalidProgramException("Failed to alloc module");
            }
            if (hl_module_init(ctx->m, 0) == 0)
            {
                throw new InvalidProgramException("Failed to init module");
            }

            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                    new(IOnNativeEvent.EventId.HL_EV_VM_READY, (nint)ctx));

            ctx->c.type = ctx->code->functions[ctx->m->functions_indexes[ctx->m->code->entrypoint]].type;
            ctx->c.fun = ctx->m->functions_ptrs[ctx->m->code->entrypoint];
            ctx->c.hasValue = 0;


        }

    }
}

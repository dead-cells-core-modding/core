using Hashlink;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Native.Events.Interfaces;
using MonoMod.Core;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static Hashlink.HashlinkNative;

namespace ModCore.Native
{
    internal unsafe abstract partial class Native
    {
        public static Native Current
        {
            get;
        } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new NativeWin() : 
            throw new PlatformNotSupportedException();


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

        #region Hooks
        private static ICoreNativeDetour? detourBreakOnTrap;

        [UnmanagedCallersOnly]
        private static nint Hook_trap_filter( nint t, HL_trap_ctx* ctx, nint v )
        {
            if ((nint)ctx->tcheck != 0x4e455445)
            {
                return 0;
            }
            var result = EventSystem.BroadcastEvent<IOnPrepareExceptionReturn, nint, nint>(v);
            Debug.Assert(result.HasValue);
            return result.Value;
        }
        [UnmanagedCallersOnly]
        private static void Return_From_Managed()
        {
            return;
        }
        [UnmanagedCallersOnly]
        private static void Capture_Current_Frame(nint ptr)
        {
            
        }

        public Native()
        {
            InitializeAsm();
            
        }

        protected virtual void InitNativeHooks()
        {
            var phLibhl = NativeLibrary.Load("libhl");

            detourBreakOnTrap = DetourFactory.Current.CreateNativeDetour(
                    NativeLibrary.GetExport(phLibhl, "break_on_trap"), asm_hook_break_on_trap_Entry, true);
            Data->orig_break_on_trap = detourBreakOnTrap.OrigEntrypoint;
            Data->trap_filter = (nint)(delegate* unmanaged< nint, HL_trap_ctx*, nint, nint >)&Hook_trap_filter;

            Data->return_from_managed = (nint)(delegate* unmanaged< void >)&Return_From_Managed;
            Data->capture_current_frame = (nint)(delegate* unmanaged< nint, void >)&Capture_Current_Frame;
        }
        #endregion
        public virtual void InitGame(ReadOnlySpan<byte> hlboot, out VMContext context)
        {
            InitNativeHooks();
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

        public abstract void MakePageWritable( nint ptr, out int old );
        public abstract void RestorePageProtect( nint ptr, int val );
    }
}

using Iced.Intel;
using ModCore.Events;
using ModCore.Hashlink;
using ModCore.Modules.Events;
using MonoMod.RuntimeDetour;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static ModCore.Hashlink.HL_module;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class HashlinkVM : CoreModule<HashlinkVM>, IOnModCoreInjected
    {
        public override int Priority => ModulePriorities.HashlinkModule;
        public nint LibhlHandle { get; private set; }
        public Thread MainThread { get; private set; } = null!;

        [StructLayout(LayoutKind.Sequential)]
        public struct VMContext
        {
            public HL_code* code;
            public HL_module* m;
            public HL_vdynamic* ret;
            public HL_vclosure c;
        }

        public VMContext* Context { get; private set; }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]

        private delegate HL_vdynamic* hl_dyn_call_safe_handler(HL_vclosure* c, HL_vdynamic** args, int nargs, bool* isException);
        private static hl_dyn_call_safe_handler orig_hl_dyn_call_safe = null!;

        private static HL_vdynamic* Hook_hl_dyn_call_safe(HL_vclosure* c, HL_vdynamic** args, int nargs, bool* isException)
        {
            using (VMTrace.CallFromHL())
            {
                NativeHook.Instance.DisableHook(orig_hl_dyn_call_safe);

                Logger.Information("Initializing Hashlink VM");
                Instance.InitializeModule();

                EventSystem.BroadcastEvent<IOnBeforeGameStartup>();
                return orig_hl_dyn_call_safe(c, args, nargs, isException);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]

        private delegate HL_array* hl_exception_stack_handler();
        private static hl_exception_stack_handler orig_hl_exception_stack = null!;

        private static readonly byte* exception_stack_msg_split0 = (byte*)Marshal.StringToHGlobalUni("==Below is the .Net Stack==\n");
        private static readonly byte* exception_stack_msg_split1 = (byte*) Marshal.StringToHGlobalUni("==Below is the Hashlink Stack==\n");
        private static HL_array* Hook_hl_exception_stack()
        {
            HL_array* stack = orig_hl_exception_stack();
            byte** stack_val = (byte**)(stack + 1);
            var net_stack = new StackTrace(1, true);

            var new_stack = HashlinkNative.hl_alloc_array(HashlinkNative.InternalTypes.hlt_bytes,
                stack->size + 1 +net_stack.FrameCount
                );
            byte** new_stack_val = (byte**)(new_stack + 1);

            int index = 0;
            new_stack_val[index++] = exception_stack_msg_split0;
            for (int i = 0; i < net_stack.FrameCount; i++)
            {
                var frame = net_stack.GetFrame(i);
                if (frame == null)
                {
                    continue;
                }

                new_stack_val[index++] = (byte*) Marshal.StringToHGlobalUni(frame.ToString());
            }
            new_stack_val[index++] = exception_stack_msg_split1;
            Buffer.MemoryCopy(stack_val, new_stack_val + index, stack->size * sizeof(byte*), stack->size * sizeof(byte*));

            return new_stack;
        }

        private void InitializeModule()
        {
            var tinfo = HashlinkNative.hl_get_thread();

            Context = (VMContext*)tinfo->stack_top;

            Logger.Information("VM Context ptr: {ctxptr:x}h", (nint)Context);
            Logger.Information("VM Code Version: {version}", Context->code->version);

            for(int i = 0; i < Context->code->nnatives; i++)
            {
                var n = Context->code->natives[i];
                Logger.Verbose("VM Native: {name} from {lib}", 
                    Marshal.PtrToStringAnsi((nint)n.name),
                    Marshal.PtrToStringAnsi((nint)n.lib)
                    );
            }

        }

        void IOnModCoreInjected.OnModCoreInjected()
        {
            Logger.Information("Initalizing HashlinkModule");

            MainThread = Thread.CurrentThread;

            LibhlHandle = NativeLibrary.Load("libhl");

            Logger.Information("Hooking functions");


            orig_hl_dyn_call_safe = NativeHook.Instance.CreateHook<hl_dyn_call_safe_handler>(
                NativeLibrary.GetExport(LibhlHandle, "hl_dyn_call_safe"), Hook_hl_dyn_call_safe);

            orig_hl_exception_stack = NativeHook.Instance.CreateHook<hl_exception_stack_handler>(
                 NativeLibrary.GetExport(LibhlHandle, "hl_exception_stack"), Hook_hl_exception_stack);
        }
    }
}

using Hashlink;
using Iced.Intel;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Hashlink;
using ModCore.Track;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using Serilog;
using Serilog.Core;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class HashlinkVM : CoreModule<HashlinkVM>, IOnModCoreInjected
    {
        public override int Priority => ModulePriorities.HashlinkVM;

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

        private static bool isFristCall_DynCallSafe = true;

        [WillCallHL]
        [StackTraceHidden]
        private static HL_vdynamic* Hook_hl_dyn_call_safe(HL_vclosure* c, HL_vdynamic** args, int nargs, bool* isException)
        {
            try
            {
                if (isFristCall_DynCallSafe)
                {
                    isFristCall_DynCallSafe = false;
                    Logger.Information("Initializing Hashlink VM");
                    Instance.InitializeModule();

                    EventSystem.BroadcastEvent<IOnBeforeGameStartup>();
                }

                MixTrace.MarkEnteringHL();
                return orig_hl_dyn_call_safe(c, args, nargs, isException);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Uncaught .NET Exception crossing the HashlinkVM-.NET runtime boundary.");
                if(Debugger.IsAttached)
                {
                    throw;
                }
                return null;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void hl_throw_handler(HL_vdynamic* val);
        private static hl_throw_handler orig_hl_throw = null!;

        [WillCallHL]
        [CallFromHLOnly]
        [StackTraceHidden]
        private static void Hook_hl_throw(HL_vdynamic* val)
        {
            Logger.Verbose("Hashlink throw an error");
            orig_hl_throw(val);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]

        private delegate HL_array* hl_exception_stack_handler();
        private static hl_exception_stack_handler orig_hl_exception_stack = null!;

        private static readonly byte* exception_stack_msg_split0 = (byte*)Marshal.StringToHGlobalUni("\n==Below is the .Net Stack==\n");
        private static readonly byte* exception_stack_msg_split1 = (byte*) Marshal.StringToHGlobalUni("\n==Below is the Hashlink Stack==\n");

        private static HL_array* GetExceptionStackFallback(HL_array* hlstack, StackTrace trace)
        {
            
            byte** stack_val = (byte**)(hlstack + 1);
            
            
            var new_stack = HashlinkObject.CreateArray(HashlinkNative.InternalTypes.hlt_bytes,
                hlstack->size + 1 + trace.FrameCount
                );
            byte** new_stack_val = (byte**)new_stack.ValuePointer;

            int index = 0;
            new_stack_val[index++] = exception_stack_msg_split0;
            for (int i = 0; i < trace.FrameCount; i++)
            {
                var frame = trace.GetFrame(i);
                if (frame != null)
                {
                    new_stack_val[index++] = (byte*)Marshal.StringToHGlobalUni(frame.GetDisplay());
                }
            }
            new_stack_val[index++] = exception_stack_msg_split1;
            Buffer.MemoryCopy(stack_val, new_stack_val + index, hlstack->size * sizeof(byte*), hlstack->size * sizeof(byte*));

            return new_stack.AsArray;
        }

        [CallFromHLOnly]
        private static HL_array* Hook_hl_exception_stack()
        {
            try
            {
                var stack = new MixStackTrace(0, true);
                var new_stack = HashlinkObject.CreateArray(HashlinkNative.InternalTypes.hlt_bytes,
                    stack.FrameCount
                );

                for(int i = 0; i < stack.FrameCount; i++)
                {
                    new_stack.Dynamic[i] = Marshal.StringToHGlobalUni(stack.GetFrame(i)!.GetDisplay());
                }

                return new_stack.AsArray;
            }
            catch(Exception ex)
            {

                Logger.Error(ex, "Failed to invoke Hook_hl_exception_stack");
                return GetExceptionStackFallback(orig_hl_exception_stack(), new(0, true));
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void hl_sys_print_handler(char* msg);
        private static hl_sys_print_handler orig_hl_sys_print = null!;
        private readonly static ILogger hlprintLogger = Log.ForContext("SourceContext", "Game");
        private readonly static StringBuilder hlprintBuffer = new();
        [CallFromHLOnly]
        private static void Hook_hl_sys_print(char* msg)
        {

            char ch;
            while((ch = *(msg++)) != 0)
            {
                if (ch == '\n')
                {
                    hlprintLogger.Information(hlprintBuffer.ToString());
                    hlprintBuffer.Clear();
                }
                else
                {
                    hlprintBuffer.Append(ch);
                }
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void hl_sys_exit_handler(int code);
        private static hl_sys_exit_handler orig_hl_sys_exit = null!;
        [CallFromHLOnly]
        private static void Hook_hl_sys_exit(int code)
        {
            EventSystem.BroadcastEvent<IOnSaveConfig>();
            EventSystem.BroadcastEvent<IOnGameExit>();

            Logger.Information("Game is exiting");

            Environment.Exit(code);
        }
        private void InitializeModule()
        {
            var tinfo = HashlinkNative.hl_get_thread();

            Context = (VMContext*)tinfo->stack_top;

            Logger.Information("VM Context ptr: {ctxptr:x}h", (nint)Context);
            Logger.Information("VM Code Version: {version}", Context->code->version);

            Logger.Information("Initializing HashlinkVM Utils");
            HashlinkUtils.Initialize(Context->code, Context->m);


        }

        void IOnModCoreInjected.OnModCoreInjected()
        {
            Logger.Information("Initalizing HashlinkModule");

            MainThread = Thread.CurrentThread;

            LibhlHandle = NativeLibrary.Load("libhl");
            NativeLibrary.GetExport(LibhlHandle, "hl_modcore_native_was_here");

            Logger.Information("Hooking functions");


            orig_hl_dyn_call_safe = NativeHook.Instance.CreateHook<hl_dyn_call_safe_handler>(
                NativeLibrary.GetExport(LibhlHandle, "hl_dyn_call_safe"), Hook_hl_dyn_call_safe);

            orig_hl_exception_stack = NativeHook.Instance.CreateHook<hl_exception_stack_handler>(
                 NativeLibrary.GetExport(LibhlHandle, "hl_exception_stack"), Hook_hl_exception_stack);

            orig_hl_throw = NativeHook.Instance.CreateHook<hl_throw_handler>(
                NativeLibrary.GetExport(LibhlHandle, "hl_throw"), Hook_hl_throw);

            orig_hl_sys_print = NativeHook.Instance.CreateHook<hl_sys_print_handler>(
                NativeLibrary.GetExport(LibhlHandle, "hl_sys_print"), Hook_hl_sys_print);

            orig_hl_sys_exit = NativeHook.Instance.CreateHook<hl_sys_exit_handler>(
               NativeLibrary.GetExport(LibhlHandle, "hl_sys_exit"), Hook_hl_sys_exit);
        }
    }
}

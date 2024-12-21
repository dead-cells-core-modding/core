using Hashlink;
using Iced.Intel;
using ModCore.Events;
using ModCore.Generator;
using ModCore.Hashlink;
using ModCore.Modules.Events;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using Serilog.Core;
using System;
using System.Buffers;
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
        public const string HLASSEMBLY_NAME = "DeadCellsGame.dll";

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

                return orig_hl_dyn_call_safe(c, args, nargs, isException);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Uncaught .NET Exception crossing the HashlinkVM-.NET runtime boundary.");
                Utils.ExitGame();
                return null;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void hl_throw_handler(HL_vdynamic* val);
        private static hl_throw_handler orig_hl_throw = null!;

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
    
            var new_stack = HashlinkNative.hl_alloc_array(HashlinkNative.InternalTypes.hlt_bytes,
                hlstack->size + 1 + trace.FrameCount
                );
            byte** new_stack_val = (byte**)(new_stack + 1);

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

            return new_stack;
        }

        private static HL_array* GetExceptionStackX86(StackTrace trace)
        {
            var ti = HashlinkNative.hl_get_thread();
            void** buf = stackalloc void*[512];
            char* strbuf = stackalloc char[512];
            int strSize = 0;

            var count = Native.modcore_x86_load_stacktrace(buf, 512, ti->stack_top);

            var str = new List<string>(count);

            var curFrameId = 0;
            var curFrame = trace.GetFrame(curFrameId++);

            foreach(var v in trace.GetFrames())
            {
                Logger.Information("IP: {ip:x} {str}", v.GetFrameIP(), v);
            }

            for(int i = 0; i < count; i++)
            {
                var eip = buf[i];
                if(eip == null)
                {
                    break;
                }
                var hlstr = HashlinkNative.hl_resolve_symbol(eip, strbuf, &strSize);
                if(hlstr != null)
                {
                    str.Add(new(hlstr));
                }
                else
                {
                    if (curFrame != null)
                    {
                        var fip = curFrame.GetFrameIP();
                        var ofs = fip - (nint)eip;
                        Logger.Information("Eip {eip:x} Fip {fip:x} {ofs}", (nint)eip, fip, ofs);

                        if(ofs < 8 && ofs > -8)
                        {
                            str.Add(curFrame.GetDisplay());
                            curFrame = trace.GetFrame(curFrameId++);
                        }
                    }
                }
            }

            var stack = HashlinkNative.hl_alloc_array(HashlinkNative.InternalTypes.hlt_bytes, str.Count);
            byte** stack_val = (byte**)(stack + 1);

            for(int i = 0; i < str.Count; i++)
            {
                stack_val[i] = (byte*)Marshal.StringToHGlobalUni(str[i]);
            }
            return stack;
        }

        private static HL_array* Hook_hl_exception_stack()
        {
            try
            {
                var trace = new StackTrace(0, true);
                if (Environment.Is64BitProcess)
                {
                    HL_array* stack = orig_hl_exception_stack();
                    return GetExceptionStackFallback(stack, trace);
                }
                else
                {
                    return GetExceptionStackX86(trace);
                }
            }catch(Exception ex)
            {
                Logger.Error(ex, "Failed to invoke Hook_hl_exception_stack");
                throw;
            }
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

            Logger.Information("Hooking functions");


            orig_hl_dyn_call_safe = NativeHook.Instance.CreateHook<hl_dyn_call_safe_handler>(
                NativeLibrary.GetExport(LibhlHandle, "hl_dyn_call_safe"), Hook_hl_dyn_call_safe);

            orig_hl_exception_stack = NativeHook.Instance.CreateHook<hl_exception_stack_handler>(
                 NativeLibrary.GetExport(LibhlHandle, "hl_exception_stack"), Hook_hl_exception_stack);

            orig_hl_throw = NativeHook.Instance.CreateHook<hl_throw_handler>(
                NativeLibrary.GetExport(LibhlHandle, "hl_throw"), Hook_hl_throw);
        }


        public HL_vdynamic* CallMethod(HL_vclosure* c, Span<nint> args)
        {
            bool isException = false;
            var result = HashlinkNative.hl_dyn_call_safe(c, 
                (HL_vdynamic**) Unsafe.AsPointer(ref args.GetPinnableReference()), args.Length, &isException);

            if (isException)
            {
                throw new HashlinkException(result);
            }
            return result;
        }

     
    }
}

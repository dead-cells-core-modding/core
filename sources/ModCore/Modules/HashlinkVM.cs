﻿using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Trace;
using Hashlink.Wrapper;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.VM;
using ModCore.Storage;
using Serilog;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;


namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    public unsafe class HashlinkVM : CoreModule<HashlinkVM>,
        IOnCoreModuleInitializing,
        IOnHashlinkVMReady,
        IOnNativeEvent,
        IOnResolveNativeFunction
    {
        public override int Priority => ModulePriorities.HashlinkVM;

        public Thread MainThread { get; private set; } = null!;

        [StructLayout(LayoutKind.Sequential)]
        public struct VMContext
        {
            public HL_code* code;
            public HL_module* m;
            public HL_vdynamic* ret;
            public HL_vclosure c;
        }

        public VMContext* Context
        {
            get; private set;
        }


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void hl_sys_print_handler( char* msg );
        private static readonly hl_sys_print_handler hl_sys_print_del = Hook_hl_sys_print;
        private static readonly ILogger hlprintLogger = Log.ForContext("SourceContext", "Game");
        private static readonly StringBuilder hlprintBuffer = new();

        private static void Hook_hl_sys_print( char* msg )
        {
            char ch;
            while ((ch = *msg++) != 0)
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
        private delegate void hl_sys_exit_handler( int code );
        private readonly static hl_sys_exit_handler hl_sys_exit_del = Hook_hl_sys_exit;

        private static void Hook_hl_sys_exit( int code )
        {
            EventSystem.BroadcastEvent<IOnSaveConfig>();
            EventSystem.BroadcastEvent<IOnGameExit>();

            Logger.Information("Game is exiting");

            Environment.Exit(code);
        }

        void IOnCoreModuleInitializing.OnCoreModuleInitializing()
        {
            Logger.Information("Initalizing HashlinkModule");

            MainThread = Thread.CurrentThread;

            Logger.Information("Hooking functions");

            //orig_hl_exception_stack = NativeHooks.Instance.CreateHook<hl_exception_stack_handler>(
            //    NativeLibrary.GetExport(LibhlHandle, "hl_exception_stack"), Hook_hl_exception_stack);
        }

        void IOnHashlinkVMReady.OnHashlinkVMReady()
        {
            var tinfo = hl_get_thread();

            Context = (VMContext*)tinfo->stack_top;

            Logger.Information("Initializing Haxe Utils Utils");

            HashlinkMarshal.Initialize(Context->m);
        }

        void IOnNativeEvent.OnNativeEvent( IOnNativeEvent.Event ev )
        {
            if (ev.EventId == IOnNativeEvent.EventId.HL_EV_VM_READY)
            {
                Context = (VMContext*)ev.Data;
                EventSystem.BroadcastEvent<IOnHashlinkVMReady>();
            }
            else if (ev.EventId == IOnNativeEvent.EventId.HL_EV_RESOLVE_NATIVE)
            {
                var data = (HLEV_native_resolve_event*)ev.Data;
                if (data->functionName == null)
                {
                    var result = EventSystem.BroadcastEvent<IOnResolveNativeLib, string, nint>(
                        Marshal.PtrToStringAnsi((nint)data->libName)!
                        );
                    if (result.HasValue)
                    {
                        data->result = (void*)result.Value;
                        return;
                    }
                }
                else
                {
                    var result = EventSystem.BroadcastEvent<IOnResolveNativeFunction, 
                        IOnResolveNativeFunction.NativeFunctionInfo, nint>(
                            new(){
                                libname = Marshal.PtrToStringAnsi((nint)data->libName)!,
                                name = Marshal.PtrToStringAnsi((nint)data->functionName)!
                            }
                            );
                    if (result.HasValue)
                    {
                        data->result = (void*)result.Value;
                        return;
                    }
                }
            }
        }

        EventResult<nint> IOnResolveNativeFunction.OnResolveNativeFunction( IOnResolveNativeFunction.NativeFunctionInfo info )
        {
            if(info.libname == "std")
            {
                if (info.name == "sys_print")
                {
                    return Marshal.GetFunctionPointerForDelegate(hl_sys_print_del);
                }
                if (info.name == "sys_exit")
                {
                    return Marshal.GetFunctionPointerForDelegate(hl_sys_exit_del);
                }
            }
            return default;
        }

        
    }
}

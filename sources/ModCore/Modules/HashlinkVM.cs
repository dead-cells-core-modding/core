using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Trace;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Trace;
using Serilog;
using System.Runtime.InteropServices;
using System.Text;


namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class HashlinkVM : CoreModule<HashlinkVM>,
        IOnCoreModuleInitializing,
        IOnHashlinkVMReady,
        IOnNativeEvent
    {
        public override int Priority => ModulePriorities.HashlinkVM;

        public nint LibhlHandle
        {
            get; private set;
        }
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

        private delegate HL_array* hl_exception_stack_handler();
        private static hl_exception_stack_handler orig_hl_exception_stack = null!;
        private static HL_array* Hook_hl_exception_stack()
        {
            try
            {
                var st = new MixStackTrace(0, true);
                var result = new HashlinkArray(hlt_bytes, st.FrameCount);
                for (var i = 0; i < st.FrameCount; i++)
                {
                    var f = st.GetFrame(i);
                    if (f == null)
                    {
                        continue;
                    }
                    result[i] = f.GetDisplayName();
                }
                return (HL_array*)result.HashlinkPointer;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error on Hook_hl_exception_stack");
                return orig_hl_exception_stack();
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void hl_sys_print_handler( char* msg );
        private static hl_sys_print_handler orig_hl_sys_print = null!;
        private static readonly ILogger hlprintLogger = Log.ForContext("SourceContext", "Game");
        private static readonly StringBuilder hlprintBuffer = new();

        [CallFromHLOnly]
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
        private static hl_sys_exit_handler orig_hl_sys_exit = null!;
        [CallFromHLOnly]
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

            LibhlHandle = NativeLibrary.Load("libhl");
            NativeLibrary.GetExport(LibhlHandle, "hl_modcore_native_was_here");

            Logger.Information("Hooking functions");

            orig_hl_exception_stack = NativeHook.Instance.CreateHook<hl_exception_stack_handler>(
                NativeLibrary.GetExport(LibhlHandle, "hl_exception_stack"), Hook_hl_exception_stack);

            orig_hl_sys_print = NativeHook.Instance.CreateHook<hl_sys_print_handler>(
                NativeLibrary.GetExport(LibhlHandle, "hl_sys_print"), Hook_hl_sys_print);

            orig_hl_sys_exit = NativeHook.Instance.CreateHook<hl_sys_exit_handler>(
               NativeLibrary.GetExport(LibhlHandle, "hl_sys_exit"), Hook_hl_sys_exit);
        }

        void IOnHashlinkVMReady.OnHashlinkVMReady()
        {
            var tinfo = hl_get_thread();

            Context = (VMContext*)tinfo->stack_top;

            Logger.Information("Initializing HashlinkVM Utils");

        }

        void IOnNativeEvent.OnNativeEvent( IOnNativeEvent.Event ev )
        {
            if (ev.EventId == IOnNativeEvent.EventId.HL_EV_ERR_NET_CAUGHT)
            {
                var hlerr = new HashlinkError(ev.Data, new MixStackTrace(0, true).ToString());

                throw new EventBreakException(hlerr);
            }
            else if (ev.EventId == IOnNativeEvent.EventId.HL_EV_VM_READY)
            {
                Context = (VMContext*)ev.Data;
                EventSystem.BroadcastEvent<IOnHashlinkVMReady>();
            }
        }
    }
}

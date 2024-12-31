using ModCore.Hashlink.Hooks;
using ModCore.Track;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    internal unsafe partial class Native
    {
        public static nint ModcorenativeHandle { get; } = NativeLibrary.Load(MODCORE_NATIVE_NAME);
        static Native()
        {
            foreach (var m in typeof(Native).GetMethods(BindingFlags.NonPublic | BindingFlags.Public |
                BindingFlags.Static))
            {
                var attr = m.GetCustomAttribute<UnmanagedCallersOnlyAttribute>();
                if (attr == null)
                {
                    continue;
                }
                var name = string.IsNullOrEmpty(attr.EntryPoint) ? m.Name : attr.EntryPoint;
                if (NativeLibrary.TryGetExport(ModcorenativeHandle, "csapi_" + name, out var addr))
                {
                    *(nint*)addr = m.MethodHandle.GetFunctionPointer();
                }
            }
        }
        #region Exports

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void PrintStackDebug()
        {
            var stack = new MixStackTrace(0, true);
            for (int i = 0; i < stack.FrameCount; i++)
            {
                Console.WriteLine(stack.GetFrame(i)!.ToString());
            }
            
        }

        private enum HashlinkEvent
        {
            BeforeGC = 1,
            AfterGC = 2
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnHLEvent(HashlinkEvent eventId, void* data)
        {
            if (eventId == HashlinkEvent.BeforeGC)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static readonly Dictionary<nint, string> knownLogSources = new()
        {
            [0x01] = "HL-GC",
            [0x02] = "HL-Code"
        };

        private static readonly ConcurrentDictionary<string, ILogger> nativeLoggers = [];

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void LogPrint(nint source, int level, byte* msg)
        {
            if(!knownLogSources.TryGetValue(source, out var sourceStr))
            {
                if(source < 0xff || !mcn_memory_readable((void*)source))
                {
                    sourceStr = "Native";
                }
                else
                {
                    sourceStr = Marshal.PtrToStringAnsi(source) ?? "Native";
                }
            }

            nativeLoggers.GetOrAdd(
                sourceStr,
                source => Log.Logger.ForContext("SourceContext", sourceStr)
                ).Write((Serilog.Events.LogEventLevel)level, Marshal.PtrToStringAnsi((nint)msg)?.Trim() ?? "null");
        }
        #endregion
    }
}

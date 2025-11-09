using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DCCMShell
{
    unsafe partial class Shell
    {
        private static void InitializeManagedAPIs( ManagedAPIInfo* info )
        {
            for (int i = 0; i < info->count; i++)
            {
                if (info->names[i] == null)
                {
                    continue;
                }
                var name = Marshal.PtrToStringAnsi((nint)info->names[i]);

                var method = typeof(Shell).GetMethod("MAPI_" + name, System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public) ??
                    throw new MissingMethodException("", name);

                *info->ptr[i] = (void*) method.MethodHandle.GetFunctionPointer();
            }
        }

        [UnmanagedCallersOnly]
        public static void MAPI_add_event_receiver( nint callback )
        {
        
        }

        [UnmanagedCallersOnly]
        public static void MAPI_remove_event_receiver( nint callback )
        {

        }

        [UnmanagedCallersOnly]
        public static void MAPI_print( char* text )
        {
            Log.Logger.Information(Marshal.PtrToStringUTF8((nint)text) ?? "");
        }
    }
}

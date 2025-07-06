using Hashlink.Marshaling;
using Hashlink.Marshaling.ObjHandle;
using Hashlink.Proxy.Objects;
using ModCore.Events;
using ModCore.Events.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Wrapper
{
    internal unsafe partial class WrapperHelper
    {
        private class ExceptionEventHandler : IEventReceiver, IOnNativeEvent
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct EventData
            {
                public void* exception;
                public void* outErrorTable;
            }
            public int Priority => 0;

            public void OnNativeEvent( IOnNativeEvent.Event ev )
            {
                if (ev.EventId == IOnNativeEvent.EventId.HL_EV_ERR_NET_CAUGHT)
                {
                    var data = (EventData*)ev.Data;
                    var type = *(void**)data->exception;
                    if (type == NETExcepetionError.ErrorType)
                    {
                        var ex = (HashlinkNETExceptionObj) HashlinkMarshal.ConvertHashlinkObject(data->exception)!;
                        last_exception = ex.Exception;
                    }
                    else
                    {
                        var st = HashlinkMarshal.ConvertHashlinkObject<HashlinkArray>(hl_exception_stack())!;
                        var sb = new StringBuilder();
                        for (int i = 0; i < st.Count; i++)
                        {
                            sb.AppendLine("at " + Marshal.PtrToStringUni((nint) st[i]!));
                        }
                        sb.AppendLine("=====================");
                        sb.AppendLine(new StackTrace(true).ToString());
                        last_exception = new HashlinkError((nint)data->exception, sb.ToString());
                    }
                    data->outErrorTable = &prepare_exception_handle_data->buffer;
                    if (prepare_exception_handle_data->stack_area == null)
                    {
                        prepare_exception_handle_data->stack_area = prepare_exception_handle_data->current->stack_area;
                    }
                }
            }
        }

        private static readonly ConditionalWeakTable<Exception, HashlinkObjHandle> caughtException = [];
        private static readonly nint pasm_prepare_exception_handle = hlu_get_exception_handle_helper();
        private static readonly AsmHelperData* asmhelper_data_pool =
            (AsmHelperData*)hl_alloc_executable_memory(81920); //TODO: 
        private static int asmhelper_data_id = 0;

        [ThreadStatic]
        private static AsmHelperData* prepare_exception_handle_data;
        [ThreadStatic]
        private static Exception? last_exception;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct AsmHelperData
        {
            public static readonly byte[] call_code_x64 = [
                0x48, 0xB8, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, //mov rax, 0xffffffffffffffff
                0xFF, 0xD0 //call rax
             ];
            public fixed byte shellcode[12];
            public void* buffer;
            public nint target;
            public void* stack_area;
            public ErrorHandle* current;
        }
        public struct ErrorHandle
        {
            public HL_trap_ctx trap_ctx;
            public ErrorHandle* prev;
            public void* stack_area;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowNetException( Exception ex )
        {
            if (ex is HashlinkError err)
            {
                hl_rethrow((HL_vdynamic*)err.Error);
            }
            if (caughtException.TryGetValue(ex, out var eh))
            {
                hl_rethrow((HL_vdynamic*)eh.nativeHLPtr);
            }
            hl_throw((HL_vdynamic*)new HashlinkNETExceptionObj(ex).HashlinkPointer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint InitErrorHandler( nint target, ref ErrorHandle handle )
        {
            if (prepare_exception_handle_data == null)
            {
                var id = Interlocked.Increment(ref asmhelper_data_id);
                if (id >= 2222)
                {
                    throw new InvalidProgramException(); //TODO: 
                }
                prepare_exception_handle_data = asmhelper_data_pool + id;
                AsmHelperData.call_code_x64.CopyTo(new Span<byte>(prepare_exception_handle_data->shellcode, 12));
                *(long*)&prepare_exception_handle_data->shellcode[2] = pasm_prepare_exception_handle;
            }
            var ti = hl_get_thread();
            if (ti == null)
            {
                return target;
            }
            handle.trap_ctx.prev = ti->trap_current;
            handle.trap_ctx.tcheck = (HL_vdynamic*)0x4e455445;
            ti->trap_current = (HL_trap_ctx*)Unsafe.AsPointer(ref handle.trap_ctx);

            prepare_exception_handle_data->buffer = (byte*)Unsafe.AsPointer(ref handle.trap_ctx.buf) + sizeof(C_jmpbuf);
            prepare_exception_handle_data->target = target;

            handle.prev = prepare_exception_handle_data->current;
            if (handle.prev != null)
            {
                handle.stack_area = prepare_exception_handle_data->stack_area;
            }
            prepare_exception_handle_data->current = (ErrorHandle*)Unsafe.AsPointer(ref handle);

            last_exception = null;

            return (nint)prepare_exception_handle_data->shellcode;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnInitErrorHandler( ref ErrorHandle handle )
        {
            var ti = hl_get_thread();
            if (ti == null)
            {
                return;
            }
            if (ti->trap_current == (HL_trap_ctx*)Unsafe.AsPointer(ref handle.trap_ctx))
            {
                ti->trap_current = handle.trap_ctx.prev;
            }
            prepare_exception_handle_data->current = handle.prev;
            prepare_exception_handle_data->stack_area = null;
            if (last_exception != null)
            {
                ExceptionDispatchInfo.Throw(last_exception);
                last_exception = null;
            }
        }
    }
}

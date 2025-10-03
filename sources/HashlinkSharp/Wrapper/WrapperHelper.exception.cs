using Hashlink.Marshaling;
using Hashlink.Marshaling.ObjHandle;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Members;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Native;
using ModCore.Native.Events.Interfaces;
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
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_stackTrace")]
        static extern ref object GetStackTrace( Exception ex );
        [StructLayout(LayoutKind.Sequential)]
        private struct StackTraceElement
        {
            public nint ip;
            public nint sp;
            public nint pFunc;
            public int flags;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct ArrayHeader
        {
            public int m_size;
            public int m_keepAliveItemsCount;
            public nint m_thread;
        };
        [StructLayout(LayoutKind.Sequential)]
        private struct EventData
        {
            public void* exception;
            public void* outErrorTable;
        }
        private class ExceptionEventHandler : IEventReceiver, 
            IOnNativeEvent,
            IOnPrepareExceptionReturn
        {
            
            public int Priority => 0;
            [StackTraceHidden]
            public void OnNativeEvent( IOnNativeEvent.Event ev )
            {
                if (ev.EventId == IOnNativeEvent.EventId.HL_EV_ERR_NET_CAUGHT)
                {
                    throw new NotImplementedException();
                }
            }
            [StackTraceHidden]
            public EventResult<nint> OnPrepareExceptionReturn( nint data )
            {
                return PrepareExceptionReturn((void*)data);
            }
        }

        private static readonly ConditionalWeakTable<Exception, HashlinkObjHandle> caughtException = [];
        private static readonly ExecutableMemoryManager<AsmHelperData> asmhelper_data_pool = new();
        [ThreadStatic]
        private static Dictionary<nint, HashlinkError>? hlerrorCache;

        [ThreadStatic]
        private static AsmHelperData* prepare_exception_handle_data;
        [ThreadStatic]
        internal static Exception? last_exception;

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

        private static nint PrepareExceptionReturn(void* exc)
        {
            
            var type = *(void**)exc;
            if (type == NETExcepetionError.ErrorType)
            {
                var ex = (HashlinkNETExceptionObj)HashlinkMarshal.ConvertHashlinkObject(exc)!;
                last_exception = ex.Exception;
            }
            else
            {
                hlerrorCache ??= [];
                if (hlerrorCache.TryGetValue((nint)exc, out var le))
                {
                    last_exception = le;
                }
                else
                {

                    try
                    {
                        throw new HashlinkError((nint)exc);
                    }
                    catch (HashlinkError ex)
                    {
                        last_exception = ex;
                        hlerrorCache[(nint)exc] = ex;
                    }
                    var st = HashlinkMarshal.ConvertHashlinkObject<HashlinkArray>(hl_exception_stack())!;
                    var sb = new StringBuilder();
                    var hasNetStack = false;
                    for (int i = 0; i < st.Count; i++)
                    {
                        if (i == 0 &&
                            (nint)st[i]! == 0)
                        {
                            hasNetStack = true;
                            continue;
                        }
                        sb.AppendLine(" at " + Marshal.PtrToStringUni((nint)st[i]!));
                    }
                    if (!hasNetStack)
                    {
                        sb.AppendLine("=====================");
                        sb.AppendLine(new StackTrace(true).ToString());
                    }
                    ((HashlinkError)last_exception).stackTrace = sb.ToString();
                }
            }

            var outErrorTable = &prepare_exception_handle_data->buffer;
            if (prepare_exception_handle_data->stack_area == null)
            {
                Debug.Assert(prepare_exception_handle_data->current->stack_area != null);
                prepare_exception_handle_data->stack_area = prepare_exception_handle_data->current->stack_area;
            }
            Debug.Assert(*(long*)prepare_exception_handle_data->stack_area == Native.STACK_CHUCK_SUM);
            return (nint)outErrorTable;
        }

        public static void CallbackCleanup()
        {
            var th = HashlinkThread.Current;
            int t = 0;

            th.CleanupInvalidReturnPointers((nint)Unsafe.AsPointer(ref t));
            th.NativeData.hl2cs_return_pointers -= sizeof(nint);
            Debug.Assert(th.ReturnPointerCount >= 0);
        }
        public static void ThrowNetException( Exception ex )
        {
            var th = HashlinkThread.Current;

            th.CleanupInvalidReturnPointers((nint)Unsafe.AsPointer(ref ex));
            *(nint*)th.ReturnPointers[^1] = Native.Current.asm_hl2cs_throw_exception; //Hijacking the return address

            th.NativeData.hl_throw_ptr = Native.Current.phl_rethrow;

            if (ex is HashlinkError err)
            {
                th.NativeData.prev_hl_error_ptr = err.Error;
            }
            else if (caughtException.TryGetValue(ex, out var eh))
            {
                th.NativeData.prev_hl_error_ptr = eh.nativeHLPtr;
            }
            else
            {
                th.NativeData.hl_throw_ptr = Native.Current.phl_throw;
                th.NativeData.prev_hl_error_ptr = new HashlinkNETExceptionObj(ex).HashlinkPointer;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static nint InitErrorHandler( nint target, ref ErrorHandle handle )
        {
            if (prepare_exception_handle_data == null)
            {
                prepare_exception_handle_data = &asmhelper_data_pool.Alloc()->value;
                AsmHelperData.call_code_x64.CopyTo(new Span<byte>(prepare_exception_handle_data->shellcode, 12));
                *(long*)&prepare_exception_handle_data->shellcode[2] = Native.Current.asm_cs_hl_store_context;
            }

            HashlinkMarshal.EnsureThreadRegistered();

            var ti = hl_get_thread();
            if (ti == null)
            {
                return target;
            }
            HashlinkThread.Current.CleanupInvalidReturnPointers((nint)Unsafe.AsPointer(ref handle));

            handle.trap_ctx.prev = ti->trap_current;
            handle.trap_ctx.tcheck = (HL_vdynamic*)0x4e455445;
            ti->trap_current = (HL_trap_ctx*)Unsafe.AsPointer(ref handle.trap_ctx);

            prepare_exception_handle_data->buffer = (byte*)Unsafe.AsPointer(ref handle.trap_ctx.buf) + sizeof(C_jmpbuf);
            prepare_exception_handle_data->target = target;

            handle.prev = prepare_exception_handle_data->current;
            if (handle.prev != null)
            {
                Debug.Assert(prepare_exception_handle_data->stack_area != null);
                handle.prev->stack_area = prepare_exception_handle_data->stack_area;
            }
            prepare_exception_handle_data->current = (ErrorHandle*)Unsafe.AsPointer(ref handle);

            last_exception = null;

            return (nint)prepare_exception_handle_data->shellcode;
        }
        [StackTraceHidden]
        private static void FixExceptionTrace( Exception ex, nint stackTop )
        {
            static ref sbyte[] GetStackTraceData( ref object stackTrace )
            {
                if (stackTrace is sbyte[])
                {
                    return ref Unsafe.As<object, sbyte[]>(ref stackTrace);
                }
                else
                {
                    return ref Unsafe.As<object, sbyte[]>(ref ((object[])(stackTrace))[0]);
                }
            }

            ref object stackTrace = ref GetStackTrace(ex);
            ref sbyte[] stackTraceData = ref GetStackTraceData(ref stackTrace);

            Span<int> buffer = stackalloc int[0x400];
            int buffer_index = 0;

            fixed (sbyte* oldPtr = stackTraceData)
            {
                var header = (ArrayHeader*)oldPtr;

                var ti = hl_get_thread();
                var old = new ReadOnlySpan<StackTraceElement>(header + 1, header->m_size);

                {
                    int index = 0;
                    for (int i = 0; i < old.Length; i++)
                    {
                        var curFrame = old[i];
                        while (index < ti->exc_stack_count)
                        {
                            var sp = ti->exc_stack_ptrs[index];
                            if (sp < 0)
                            {
                                index++;
                                continue;
                            }
                            if (sp >= curFrame.sp)
                            {
                                break;
                            }
                            buffer[buffer_index++] = -(index + 1);
                            ti->exc_stack_ptrs[index++] = -sp;
                        }
                        buffer[buffer_index++] = i;
                    }

                    while (index < ti->exc_stack_count)
                    {
                        var sp = ti->exc_stack_ptrs[index];
                        if (sp < 0)
                        {
                            index++;
                            continue;
                        }
                        if (sp >= stackTop)
                        {
                            break;
                        }
                        buffer[buffer_index++] = -(index + 1);
                        ti->exc_stack_ptrs[index++] = -sp;
                    }
                    
                }
                
                var newTraceData = GC.AllocateArray<sbyte>(sizeof(ArrayHeader) +
                    sizeof(StackTraceElement) * buffer_index, true);
                
                stackTraceData = newTraceData;
                var newheader = (ArrayHeader*)Unsafe.AsPointer(ref newTraceData[0]);
                *newheader = *header;
                newheader->m_size = buffer_index;
                var newdata = new Span<StackTraceElement>(newheader + 1, newheader->m_size);
                
                void* hlbuf = stackalloc char[0x100];

                for (int i = 0; i < buffer_index; i++)
                {
                    var id = buffer[i];
                    if (id >= 0)
                    {
                        newdata[i] = old[id];
                    }
                    else
                    {
                        id = -id - 1;
                        var ip = ti->exc_stack_trace[id];
                        var size = 0x100;
                        module_resolve_symbol((void*)ip, (char*)hlbuf, ref size);
                        var str = new string((char*)hlbuf);

                        var lastDot = str.LastIndexOf('.');
                        var className = "global";
                        var funcName = "";
                        if (lastDot == -1)
                        {
                            funcName = str[..^2];
                        }
                        else
                        {
                            className = str[..lastDot];
                            funcName = str[(lastDot + 1)..^2];
                        }
                        var req = new FakeStackTraceManager.RequestInfo("Haxe!" + className, funcName);
                        var fk = FakeStackTraceManager.RequestFakeMethods(req)[req];
                        newdata[i] = new()
                        {
                            sp = -ti->exc_stack_ptrs[id],
                            ip = fk.IP,
                            flags = 2,
                            pFunc = fk.Method.MethodHandle.Value
                        };

                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void UnInitErrorHandler( ref ErrorHandle handle )
        {
            var ti = hl_get_thread();
            if (ti == null)
            {
                return;
            }
            HashlinkThread.Current.CleanupInvalidReturnPointers((nint)Unsafe.AsPointer(ref handle));
            if (ti->trap_current == (HL_trap_ctx*)Unsafe.AsPointer(ref handle.trap_ctx))
            {
                ti->trap_current = handle.trap_ctx.prev;
            }
            prepare_exception_handle_data->current = handle.prev;
            if (prepare_exception_handle_data->current != null)
            {
                prepare_exception_handle_data->stack_area = prepare_exception_handle_data->current->stack_area;
                Debug.Assert(*(long*)prepare_exception_handle_data->stack_area == Native.STACK_CHUCK_SUM);
            }
            if (last_exception != null)
            {
                ((delegate* unmanaged< void >)Native.Current.asm_empty_method)();
                var ex = last_exception;
                FixExceptionTrace(last_exception, (nint)Unsafe.AsPointer(ref handle));
                last_exception = null;
                ExceptionDispatchInfo.Throw(ex);
            }
        }
    }
}

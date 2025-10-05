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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static Hashlink.HashlinkNative;

namespace ModCore.Native
{
    internal unsafe abstract partial class Native
    {
        public nint phl_throw;
        public nint phl_rethrow;

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

        #region Hooks

        private readonly List<ICoreNativeDetour> detours = [];

        private VMContext* context;

        [UnmanagedCallersOnly]
        protected static nint Hook_trap_filter( nint t, HL_trap_ctx* ctx, nint v )
        {
            if ((nint)ctx->tcheck != 0x4e455445)
            {
                return 0;
            }
            var result = EventSystem.BroadcastEvent<IOnPrepareExceptionReturn, nint, nint>(v);
            Debug.Assert(result.HasValue);
            return result.Value;
        }

        private static nint orig_gc_mark;
        [UnmanagedCallersOnly]
        protected static void Hook_gc_mark()
        {
            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                new(IOnNativeEvent.EventId.HL_EV_GC_BEFORE_MARK, 0));

            ((delegate* unmanaged< void >)orig_gc_mark)();

            IOnNativeEvent.Event_gc_roots roots = new();
            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                new(IOnNativeEvent.EventId.HL_EV_GC_SEARCH_ROOT, (nint)(&roots)));

            Current.GcScanManagedRef(new(roots.roots, roots.nroots));

            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                new(IOnNativeEvent.EventId.HL_EV_GC_AFTER_MARK, 0));
        }
        private static nint orig_gc_major;
        [UnmanagedCallersOnly]
        protected static void Hook_gc_major()
        {
            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                new(IOnNativeEvent.EventId.HL_EV_BEGORE_GC, 0));

            ((delegate* unmanaged< void >)orig_gc_major)();

            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                new(IOnNativeEvent.EventId.HL_EV_AFTER_GC, 0));
        }

        private static nint orig_gc_mark_stack;
        [UnmanagedCallersOnly]
        protected static void Hook_gc_mark_stack( nint start, nint end )
        {
            if (start == 0 || end == 0)
            {
                return;
            }
            ((delegate* unmanaged<nint, nint, void>)orig_gc_mark_stack)(start, end);
        }

        private static nint orig_resolve_library;
        [UnmanagedCallersOnly]
        protected static nint Hook_resolve_library( byte* lib, int is_opt )
        {
            HLEV_native_resolve_event ev = new()
            {
                libName = lib,
            };
            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(new(
                IOnNativeEvent.EventId.HL_EV_RESOLVE_NATIVE, (nint) (&ev)));
            if (ev.result != null)
            {
                return (nint)ev.result;
            }
            return ((delegate* unmanaged<byte*, int, nint>)orig_resolve_library)(lib, is_opt);
        }

        private static nint orig_hl_module_init_natives;
        [UnmanagedCallersOnly]
        protected static void Hook_hl_module_init_natives( HL_module* m )
        {
            ((delegate* unmanaged< HL_module*, void >)orig_hl_module_init_natives)(m);

            for (int i = 0; i < m->code->nnatives; i++)
            {
                var native = m->code->natives + i;

                HLEV_native_resolve_event ev = new()
                {
                    libName = native->lib,
                    functionName = native->name
                };
                EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(new(
                    IOnNativeEvent.EventId.HL_EV_RESOLVE_NATIVE, (nint)(&ev)));
                if (ev.result != null)
                {
                    m->functions_ptrs[native->findex] = ev.result;
                }
            }
        }

        private static nint orig_module_capture_stack;
        [UnmanagedCallersOnly]
        protected static void Hook_module_capture_stack( void** stack, int size )
        {
            ((delegate*unmanaged<void**, int, void>)orig_module_capture_stack)(stack, size);

            int count = 0;
            void** stack_ptr = (void**)&stack;
            void* stack_bottom = stack_ptr;
            void* stack_top = hl_get_thread()->stack_top;
            var m = Current.context->m;
            var code = (nint)m->jit_code;
            int code_size = m->codesize;
            if (m->jit_debug != null)
            {
                int s = m->jit_debug[0].start;
                code += s;
                code_size -= s;
            }
            while (stack_ptr < (void**)stack_top)
            {
                void* stack_addr = *stack_ptr++; // EBP
                if (stack_addr > stack_bottom && stack_addr < stack_top)
                {
                    void* module_addr = *stack_ptr; // EIP
                    if (module_addr >= (void*)code && module_addr < (void*)(code + code_size))
                    {
                        if (count == size)
                            break;
                        Debug.Assert(stack[count] == module_addr);
                        Current.TlsData->exc_stack_ptrs[count++] = (nint)stack_ptr;
                    }
                }
            }
        }

        private static nint orig_gc_allocator_alloc;
        [UnmanagedCallersOnly]
        protected static nint Hook_gc_allocator_alloc( int* size, int page_kind )
        {
            *size += 8;
            var result = ((delegate*unmanaged<int*, int, nint>)orig_gc_allocator_alloc)(size, page_kind);
            *((nint*)(result + *size - 8)) = 0;
            //*size -= 8;
            return result;
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

        protected ICoreNativeDetour CreateNativeHookForHL( string srcName, string hookName, out nint orig )
        {
            var hook = GetType().GetMethod(hookName, BindingFlags.Static | 
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy);

            Debug.Assert(hook != null);

            var ptr = hook.MethodHandle.GetFunctionPointer();

            return Current.CreateNativeHookForHL(srcName, 
                ptr, out orig);
        }
        protected ICoreNativeDetour CreateNativeHookForHL( string srcName, nint hook, out nint orig )
        {
            var phLibhl = NativeLibrary.Load("libhl");
            return CreateNativeHook(NativeLibrary.GetExport(phLibhl, srcName),
                hook, out orig);
        }
        protected ICoreNativeDetour CreateNativeHook( nint src, nint dst, out nint orig )
        {
            var detour = DetourFactory.Current.CreateNativeDetour(
                    src, dst, true);
            orig = detour.OrigEntrypoint;
            Debug.Assert(orig != 0);
            detours.Add(detour);
            return detour;
        }

        protected virtual void InitializeNativeHooks()
        {
            var phLibhl = NativeLibrary.Load("libhl");
            CreateNativeHookForHL("module_capture_stack", nameof(Hook_module_capture_stack), out orig_module_capture_stack);
            CreateNativeHookForHL("break_on_trap", asm_hook_break_on_trap_Entry, out Data->orig_break_on_trap);
            CreateNativeHookForHL("gc_mark_stack", nameof(Hook_gc_mark_stack), out orig_gc_mark_stack);
            CreateNativeHookForHL("gc_mark", nameof(Hook_gc_mark), out orig_gc_mark);
            CreateNativeHookForHL("gc_major", nameof(Hook_gc_major), out orig_gc_major);
            CreateNativeHookForHL("resolve_library", nameof(Hook_resolve_library), out orig_resolve_library);
            CreateNativeHookForHL("hl_module_init_natives", nameof(Hook_hl_module_init_natives), out orig_hl_module_init_natives);
            CreateNativeHookForHL("gc_allocator_alloc", nameof(Hook_gc_allocator_alloc), out orig_gc_allocator_alloc);
            Data->trap_filter = (nint)(delegate* unmanaged< nint, HL_trap_ctx*, nint, nint >)&Hook_trap_filter;

            Data->return_from_managed = (nint)(delegate* unmanaged< void >)&Return_From_Managed;
            Data->capture_current_frame = (nint)(delegate* unmanaged< nint, void >)&Capture_Current_Frame;
        }
        #endregion

        public abstract void FixThreadCurrentStackFrame( HL_thread_info* t );
        public virtual void InitializeGame(ReadOnlySpan<byte> hlboot, out VMContext context)
        {
            InitializeNative();
            InitializeNativeHooks();

            HL_code* code;
            byte* err;
            context = new();
            var ctx = this.context = (VMContext*)Unsafe.AsPointer(ref context);

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
            if (hl_module_init(ctx->m, 0, 0) == 0)
            {
                throw new InvalidProgramException("Failed to init module");
            }

            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                    new(IOnNativeEvent.EventId.HL_EV_VM_READY, (nint)ctx));

            ctx->c.type = ctx->code->functions[ctx->m->functions_indexes[ctx->m->code->entrypoint]].type;
            ctx->c.fun = ctx->m->functions_ptrs[ctx->m->code->entrypoint];
            ctx->c.hasValue = 0;


        }
        public virtual void InitializeNative()
        {
            var phLibhl = NativeLibrary.Load("libhl");

            phl_gc_page_map = (HL_gc_pheader***)NativeLibrary.GetExport(phLibhl, "hl_gc_page_map");
            pglobal_mark_stack = (HL_gc_mstack*)NativeLibrary.GetExport(phLibhl, "global_mark_stack");

            phl_throw = NativeLibrary.GetExport(phLibhl, "hl_throw");
            phl_rethrow = NativeLibrary.GetExport(phLibhl, "hl_rethrow");
        }
        public abstract void MakePageWritable( nint ptr, out int old );
        public abstract void RestorePageProtect( nint ptr, int val ); 
        public abstract ReadOnlySpan<byte> GetHlbootDataFromExe( string exePath );
    }
}

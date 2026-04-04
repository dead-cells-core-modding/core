using Hashlink;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.VM;
using ModCore.Native.Events.Interfaces;
using MonoMod.Core;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static Hashlink.HashlinkNative;

namespace ModCore.Native
{
    internal unsafe abstract partial class Native
    {

        private readonly static HL_type* TYPE_DYN = (HL_type*)NativeMemory.AllocZeroed((nuint)sizeof(HL_type));
        public nint phl_throw;
        public nint phl_rethrow;

        public HL_setup_t* hl_setup;

        public static Func<string, nint> GetLibhlSymbolFunc = name => NativeLibrary.GetExport(NativeLibrary.Load("libhl"), name);

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

        [StructLayout(LayoutKind.Sequential)]
        public struct UnsafeHelpers
        {
            public nint longjmp;
            public nint setjmp;
        }

        #region Hooks

        public UnsafeHelpers unsafeHelpers;

        private readonly List<ICoreNativeDetour> detours = [];

        private HL_module* module;

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

            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                new(IOnNativeEvent.EventId.HL_EV_GC_AFTER_MARK, 0));
        }
        private static nint orig_gc_allocator_after_mark;
        [UnmanagedCallersOnly]
        protected static void Hook_gc_allocator_after_mark()
        {
            IOnNativeEvent.Event_gc_roots roots = new();
            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                new(IOnNativeEvent.EventId.HL_EV_GC_SEARCH_ROOT, (nint)(&roots)));

            Current.GcScanManagedRef(new(roots.roots, roots.nroots));

            ((delegate* unmanaged< void >)orig_gc_allocator_after_mark)();
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
        protected static int Hook_module_capture_stack( void** stack, int size )
        {
            var result = ((delegate*unmanaged<void**, int, int>)orig_module_capture_stack)(stack, size);

            int count = 0;
            void** stack_ptr = (void**)&stack;
            void* stack_bottom = stack_ptr;
            void* stack_top = hl_get_thread()->stack_top;
            var m = Current.module;
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
                        
                        while (stack[count] != module_addr &&
                            count < size)
                        {
                            count++;
                        }
                        if (count == size)
                            break;
                        Debug.Assert(stack[count] == module_addr);
                        Current.TlsData->exc_stack_ptrs[count++] = (nint)stack_ptr;
                    }
                }
            }

            //Debug.Assert(count == result);

            return result;
        }

        private static nint orig_gc_allocator_alloc;
        [UnmanagedCallersOnly]
        protected static nint Hook_gc_allocator_alloc( int* size, int page_kind )
        {
            *size += 8;
            var result = ((delegate*unmanaged<int*, int, nint>)orig_gc_allocator_alloc)(size, page_kind);
            *((nint*)(result + *size - 8)) = 0;
            Debug.Assert(hl_gc_get_memsize((void*)result) >= 0);
            //*size -= 8;
            return result;
        }

        private static nint orig_hl_module_alloc;
        [UnmanagedCallersOnly]
        protected static HL_module* Hook_hl_module_alloc( HL_code* code )
        {
            var result = ((delegate* unmanaged< HL_code*, HL_module* >)orig_hl_module_alloc)(code);
            Current.module = result;
            return result;
        }

        private static nint orig_hl_code_read;
        [UnmanagedCallersOnly]
        protected static HL_code* Hook_hl_code_read( byte* data, int size, void* unknown )
        {
            var codeData = new ReadOnlySpan<byte>(data, size);
            EventSystem.BroadcastEvent<IOnCodeLoading, ReadOnlySpan<byte>>(ref codeData);
            fixed (byte* ptr = codeData)
            {
                return ((delegate* unmanaged< byte*, int, void*, HL_code* >)orig_hl_code_read)(ptr, codeData.Length, unknown);
            }
            
        }

        private static void HashlinkDynSet<T>( nint d, int hfield, T val, nint? extraTypePtr, nint origSet ) where T : unmanaged
        {
            var result = EventSystem.BroadcastEvent<IOnHashlinkDynSet, IOnHashlinkDynSet.Data, bool>(new(d, hfield, val, extraTypePtr));
            if (!result.HasValue)
            {
                if ((extraTypePtr ?? 0) == 0)
                {
                    ((delegate* unmanaged< nint, int, T, void >)origSet)(d, hfield, val);
                }
                else
                {
                    ((delegate* unmanaged< nint, int, nint, T, void >)origSet)(d, hfield, extraTypePtr ?? 0, val);
                }
            }
        }

        private static nint orig_hl_obj_set_field;
        [UnmanagedCallersOnly]
        protected static void Hook_hl_obj_set_field( nint d, int hfield, nint val )
        {
            HashlinkDynSet(d, hfield, val, 0, orig_hl_obj_set_field);
        }

        private static nint orig_hl_dyn_setd;
        [UnmanagedCallersOnly]
        protected static void Hook_hl_dyn_setd( nint d, int hfield, double val )
        {
            HashlinkDynSet(d, hfield, val, null, orig_hl_dyn_setd);
        }

        private static nint orig_hl_dyn_setf;
        [UnmanagedCallersOnly]
        protected static void Hook_hl_dyn_setf( nint d, int hfield, float val )
        {
            HashlinkDynSet(d, hfield, val, null, orig_hl_dyn_setf);
        }

        private static nint orig_hl_dyn_seti64;
        [UnmanagedCallersOnly]
        protected static void Hook_hl_dyn_seti64( nint d, int hfield, long val )
        {
            HashlinkDynSet(d, hfield, val, null, orig_hl_dyn_seti64);
        }

        private static nint orig_hl_dyn_seti;
        [UnmanagedCallersOnly]
        protected static void Hook_hl_dyn_seti( nint d, int hfield, nint t, int val )
        {
            HashlinkDynSet(d, hfield, val, t, orig_hl_dyn_seti);
        }

        private static nint orig_hl_dyn_setp;
        [UnmanagedCallersOnly]
        protected static void Hook_hl_dyn_setp( nint d, int hfield, nint t, nint val )
        {
            HashlinkDynSet(d, hfield, val, t, orig_hl_dyn_setp);
        }

        private static nint orig_hl_obj_has_field;

        [UnmanagedCallersOnly]
        protected static int Hook_hl_obj_has_field( nint d, int hfield )
        {
            var result = EventSystem.BroadcastEvent<IOnHashlinkDynHasField, IOnHashlinkDynHasField.Data, bool>(new(d, hfield));
            if (!result.HasValue)
            {
                return ((delegate* unmanaged< nint, int, int>)orig_hl_obj_has_field)(d, hfield);
            }
            return result.Value ? 1 : 0;
        }

        private static T HashlinkDynGet<T>( nint d, int hfield, nint ptype, nint origGet ) where T : unmanaged
        {
            var result = EventSystem.BroadcastEvent<IOnHashlinkDynGet, IOnHashlinkDynGet.Data, object>(new(d, hfield, ptype));
            if (!result.HasValue)
            {
                return ((delegate* unmanaged<nint, int, nint, T>)origGet)(d, hfield, ptype);
            }
            return (T)(dynamic)result.Value;
        }


        private static nint orig_hl_dyn_getp;
        [UnmanagedCallersOnly]
        protected static nint Hook_hl_dyn_getp( nint d, int hfield, nint ptype )
        {
            return HashlinkDynGet<nint>(d, hfield, ptype, orig_hl_dyn_getp);
        }

        private static nint orig_hl_dyn_getd;
        [UnmanagedCallersOnly]
        protected static double Hook_hl_dyn_getd( nint d, int hfield )
        {
            return HashlinkDynGet<double>(d, hfield, 0, orig_hl_dyn_getd);
        }

        private static nint orig_hl_dyn_getf;
        [UnmanagedCallersOnly]
        protected static float Hook_hl_dyn_getf( nint d, int hfield)
        {
            return HashlinkDynGet<float>(d, hfield, 0, orig_hl_dyn_getf);
        }

        private static nint orig_hl_dyn_geti64;
        [UnmanagedCallersOnly]
        protected static long Hook_hl_dyn_geti64( nint d, int hfield )
        {
            return HashlinkDynGet<long>(d, hfield, 0, orig_hl_dyn_getf);
        }

        private static nint orig_hl_dyn_geti;
        [UnmanagedCallersOnly]
        protected static int Hook_hl_dyn_geti( nint d, int hfield, nint ptype )
        {
            return HashlinkDynGet<int>(d, hfield, ptype, orig_hl_dyn_geti);
        }

        private static nint orig_hl_obj_lookup_extra;
        [UnmanagedCallersOnly]
        protected static nint Hook_hl_obj_lookup_extra( nint d, int hfield )
        {
            return HashlinkDynGet<nint>(d, hfield, (nint)TYPE_DYN, orig_hl_obj_lookup_extra);
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

        public static nint GetLibhlSymbol( string name )
        {
            return GetLibhlSymbolFunc(name);
        }
        public static nint GetLibhlSymbolEx( string name, ref nint cache )
        {
            if (cache != 0)
            {
                return cache;
            }
            return cache = GetLibhlSymbol(name);
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
            return CreateNativeHook(GetLibhlSymbol(srcName),
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

            CreateNativeHookForHL("hl_module_alloc", nameof(Hook_hl_module_alloc), out orig_hl_module_alloc);
            CreateNativeHookForHL("hl_code_read", nameof(Hook_hl_code_read), out orig_hl_code_read);
            CreateNativeHookForHL("module_capture_stack", nameof(Hook_module_capture_stack), out orig_module_capture_stack);
            CreateNativeHookForHL("break_on_trap", asm_hook_break_on_trap_Entry, out Data->orig_break_on_trap);
            CreateNativeHookForHL("gc_mark_stack", nameof(Hook_gc_mark_stack), out orig_gc_mark_stack);
            CreateNativeHookForHL("gc_mark", nameof(Hook_gc_mark), out orig_gc_mark);
            CreateNativeHookForHL("gc_major", nameof(Hook_gc_major), out orig_gc_major);
            CreateNativeHookForHL("resolve_library", nameof(Hook_resolve_library), out orig_resolve_library);
            CreateNativeHookForHL("hl_module_init_natives", nameof(Hook_hl_module_init_natives), out orig_hl_module_init_natives);
            CreateNativeHookForHL("gc_allocator_alloc", nameof(Hook_gc_allocator_alloc), out orig_gc_allocator_alloc);
            CreateNativeHookForHL("gc_allocator_after_mark", nameof(Hook_gc_allocator_after_mark), out orig_gc_allocator_after_mark);

            CreateNativeHookForHL("hl_obj_set_field", nameof(Hook_hl_obj_set_field), out orig_hl_obj_set_field);
            CreateNativeHookForHL("hl_dyn_setp", nameof(Hook_hl_dyn_setp), out orig_hl_dyn_setp);
            CreateNativeHookForHL("hl_dyn_setd", nameof(Hook_hl_dyn_setd), out orig_hl_dyn_setd);
            CreateNativeHookForHL("hl_dyn_setf", nameof(Hook_hl_dyn_setf), out orig_hl_dyn_setf);
            CreateNativeHookForHL("hl_dyn_seti64", nameof(Hook_hl_dyn_seti64), out orig_hl_dyn_seti64);
            CreateNativeHookForHL("hl_dyn_seti", nameof(Hook_hl_dyn_seti), out orig_hl_dyn_seti);

            CreateNativeHookForHL("hl_obj_has_field", nameof(Hook_hl_obj_has_field), out orig_hl_obj_has_field);

            CreateNativeHookForHL("hl_dyn_getp", nameof(Hook_hl_dyn_getp), out orig_hl_dyn_getp);
            CreateNativeHookForHL("hl_dyn_geti", nameof(Hook_hl_dyn_geti), out orig_hl_dyn_geti);
            CreateNativeHookForHL("hl_dyn_getd", nameof(Hook_hl_dyn_getd), out orig_hl_dyn_getd);
            CreateNativeHookForHL("hl_dyn_getf", nameof(Hook_hl_dyn_getf), out orig_hl_dyn_getf);
            CreateNativeHookForHL("hl_dyn_geti64", nameof(Hook_hl_dyn_geti64), out orig_hl_dyn_geti64);
            CreateNativeHookForHL("hl_obj_lookup_extra", nameof(Hook_hl_obj_lookup_extra), out orig_hl_obj_lookup_extra);

            Data->trap_filter = (nint)(delegate* unmanaged< nint, HL_trap_ctx*, nint, nint >)&Hook_trap_filter;

            Data->return_from_managed = (nint)(delegate* unmanaged< void >)&Return_From_Managed;
            Data->capture_current_frame = (nint)(delegate* unmanaged< nint, void >)&Capture_Current_Frame;
        }
        #endregion

        public abstract void FixThreadCurrentStackFrame( HL_thread_info* t );
        public virtual void InitializeCore()
        {
            TYPE_DYN->kind = TypeKind.HDYN;

            InitializeNative();
            InitializeNativeHooks();
        }
        public virtual void InitializeGame(ReadOnlySpan<byte> hlboot, out VMContext context)
        {
            HL_code* code;
            byte* err;
            context = new();
            var ctx = (VMContext*)Unsafe.AsPointer(ref context);

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
            hl_setup = (HL_setup_t*)GetLibhlSymbol("hl_setup");
            phl_gc_page_map = (HL_gc_pheader***)GetLibhlSymbol("hl_gc_page_map");
            pglobal_mark_stack = (HL_gc_mstack*)GetLibhlSymbol("global_mark_stack");
            pmark_threads_active = (byte*)GetLibhlSymbol("mark_threads_active");
            pmark_threads_done = (void**)GetLibhlSymbol("mark_threads_done");

            phl_throw = GetLibhlSymbol("hl_throw");
            phl_rethrow = GetLibhlSymbol("hl_rethrow");

        }
        public abstract void MakePageWritable( nint ptr, out int old );
        public abstract void RestorePageProtect( nint ptr, int val ); 
        public abstract ReadOnlySpan<byte> GetHlbootDataFromExe( string exePath );
    }
}

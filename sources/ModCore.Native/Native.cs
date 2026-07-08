using Hashlink;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.VM;
using ModCore.Native.Events.Interfaces;
using ModCore.Native.Platforms.Android;
using ModCore.Storage;
using MonoMod.Core;
using Serilog;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Hashlink.HashlinkNative;

namespace ModCore.Native
{
    internal unsafe abstract partial class Native
    {
        private nint hlibc;
        private readonly static HL_type* TYPE_DYN = (HL_type*)NativeMemory.AllocZeroed((nuint)sizeof(HL_type));
        public nint phl_throw;
        public nint phl_rethrow;

        public readonly List<(int fidx, nint startPtr)> hlc_functions = [];
        public HL_type** hlc_instance_types;
        public void** hlc_global_data;
        public HL_setup_t* hl_setup;


        public bool RunOnHLC
        {
            get; private set;
        } = false;

        public static Func<string, nint> GetLibhlSymbolFunc = name => NativeLibrary.GetExport(Current?.LoadLibrary("libhl") ?? (
            OperatingSystem.IsAndroid() ? NativeLibrary.Load(
                FolderInfo.CurrentNativeRoot.GetFilePath("libhl.so")
            ) :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? NativeLibrary.Load(
                FolderInfo.CurrentNativeRoot.GetFilePath("libhl.so.1")
            ) : NativeLibrary.Load("libhl")
            ), name);

        public static Native Current
        {
            get;
        } = CreateNative();

        private static Native CreateNative()
        {
            if (OperatingSystem.IsWindows())
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    return new WindowsX64Native();
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    return new LinuxX64Native();
                }
            }
            else if (OperatingSystem.IsAndroid())
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    return new AndroidX64Native();
                }
                // Reserved dispatch point for future Android arm64 support.
                // The arm64 platform backend (AArch64 assembly generators in a
                // dedicated Native subclass) is not implemented yet.
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    throw new PlatformNotSupportedException(
                        "Android arm64 is reserved but not yet implemented. " +
                        "An AArch64 Native backend must be provided.");
                }
            }
            throw new PlatformNotSupportedException();
        }

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

        private static nint orig_hl_dyn_compare;

        [UnmanagedCallersOnly]
        protected static int Hook_hl_dyn_compare( HL_vdynamic* a, HL_vdynamic* b )
        {
            if (a == b)
            {
                return 0;
            }

            if (a != null && b != null)
            {
                if (a->type->kind == TypeKind.HENUM &&
                    b->type->kind == TypeKind.HENUM)
                {
                    var ea = (HL_enum*)a;
                    var eb = (HL_enum*)b;

                    return hl_type_enum_eq(ea, eb) == 1 ? 0 : (ea > eb ? 1 : -1);

                }
            }

            return ((delegate* unmanaged< nint, nint, int >)orig_hl_dyn_compare)((nint)a, (nint)b);
        }

        public virtual bool TryLoadLibrary( string path, out nint handle )
        {
            return NativeLibrary.TryLoad(path, out handle);
        }

        public virtual nint LoadLibrary( string path )
        {
            if (TryLoadLibrary(path, out var handle))
            {
                return handle;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return NativeLibrary.Load(path + ".so");
            }
            return NativeLibrary.Load(path);
        }

        private static nint orig_jit_op_jump;

        [UnmanagedCallersOnly]
        protected static void Hook_jit_op_jump( nint ctx, HL_jit_vreg* ra, HL_jit_vreg* rb, nint op, int targetPos )
        {
            var oat = ra->t;
            var obt = rb->t;
            if (ra->t->kind == TypeKind.HENUM && rb->t->kind == TypeKind.HENUM)
            {
                ra->t = TYPE_DYN;
                rb->t = TYPE_DYN;
            }
            ((delegate* unmanaged< nint, nint, nint, nint, int, void >)orig_jit_op_jump)(ctx, (nint)ra, (nint)rb, op, targetPos);
            ra->t = oat;
            rb->t = obt;
        }

        [UnmanagedCallersOnly]
        protected static nint Hook_trap_filter( nint t, HL_trap_ctx* ctx, nint v )
        {
            if ((nint)ctx->tcheck != Current.Data->trap_magic_number)
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
            if (GCSettings.LatencyMode == GCLatencyMode.NoGCRegion)
            {
                return;
            }
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
            if (start == 0 || end == 0 || Current.IsBadPtr(start))
            {
                return;
            }
            ((delegate* unmanaged< nint, nint, void >)orig_gc_mark_stack)(start, end);
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
                IOnNativeEvent.EventId.HL_EV_RESOLVE_NATIVE, (nint)(&ev)));
            if (ev.result != null)
            {
                return (nint)ev.result;
            }
            return ((delegate* unmanaged< byte*, int, nint >)orig_resolve_library)(lib, is_opt);
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


        private static void GetHLCodeRange( out nint low, out nint high )
        {
            var m = Current.module;

            nint code;
            int code_size;
            if (m->jit_code != null)
            {
                code = (nint)m->jit_code;
                code_size = m->codesize;
                if (m->jit_debug != null)
                {
                    int s = m->jit_debug[0].start;
                    code += s;
                    code_size -= s;
                }
            }
            else
            {
                // HLC
                nint min = Current.hlc_functions[0].startPtr;
                nint max = Current.hlc_functions[^1].startPtr;

                code = min;
                code_size = (int)(max - min);
            }
            low = code;
            high = code + code_size;
        }


        private static int FallbackCaptureStack( void** stack, int size )
        {
            var count = 0;
            void** stack_ptr = (void**)&stack;
            void* stack_bottom = stack_ptr;
            void* stack_top = hl_get_thread()->stack_top;
            var m = Current.module;

            GetHLCodeRange(out var clow, out var chigh);

            while (stack_ptr < (void**)stack_top && count < size)
            {
                void* stack_addr = *stack_ptr++; // EBP
                if (stack_addr > stack_bottom && stack_addr < stack_top)
                {
                    void* module_addr = *stack_ptr; // EIP
                    if (module_addr >= (void*)clow && module_addr < (void*)chigh)
                    {
                        stack[count++] = module_addr;
                    }
                }
            }
            return count;
        }
        [UnmanagedCallersOnly]
        protected static int Hook_module_capture_stack( void** stack, int size )
        {
            var result = ((delegate* unmanaged< void**, int, int >)orig_module_capture_stack)(stack, size);

            if (result == 0)
            {
                result = FallbackCaptureStack(stack, size);
            }

            int count = 0;
            void** stack_ptr = (void**)&stack;
            void* stack_bottom = stack_ptr;
            void* stack_top = hl_get_thread()->stack_top;
            var m = Current.module;

            GetHLCodeRange(out var clow, out var chigh);

            while (stack_ptr < (void**)stack_top)
            {
                void* stack_addr = *stack_ptr++; // EBP
                if (stack_addr > stack_bottom && stack_addr < stack_top)
                {
                    void* module_addr = *stack_ptr; // EIP
                    if (module_addr >= (void*)clow && module_addr < (void*)chigh)
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

        private static nint orig_hl_fatal_error;
        [UnmanagedCallersOnly]
        protected static void Hook_hl_fatal_error( byte* msg, byte* file, int line )
        {
            Log.Fatal("Hashlink fatal error at {File}:{Line}: {Message}",
                Marshal.PtrToStringAnsi((nint)file), line, Marshal.PtrToStringAnsi((nint)msg));

            if (!ContextConfig.Config.suppressFatalWindows)
            {
                ((delegate* unmanaged< byte*, byte*, int, void >)orig_hl_fatal_error)(msg, file, line);
            }
            else
            {
                Environment.FailFast(null);
            }
        }

        public static long totalAllocMemory = 0;

        private static nint orig_gc_allocator_alloc;
        [UnmanagedCallersOnly]
        protected static nint Hook_gc_allocator_alloc( int* size, int page_kind )
        {
            *size += 8;
            totalAllocMemory += *size;
            var result = ((delegate* unmanaged< int*, int, nint >)orig_gc_allocator_alloc)(size, page_kind);
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
                return ((delegate* unmanaged< nint, int, int >)orig_hl_obj_has_field)(d, hfield);
            }
            return result.Value ? 1 : 0;
        }

        private static T HashlinkDynGet<T>( nint d, int hfield, nint ptype, nint origGet ) where T : unmanaged
        {
            var result = EventSystem.BroadcastEvent<IOnHashlinkDynGet, IOnHashlinkDynGet.Data, object>(new(d, hfield, ptype));
            if (!result.HasValue)
            {
                return ((delegate* unmanaged< nint, int, nint, T >)origGet)(d, hfield, ptype);
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
        protected static float Hook_hl_dyn_getf( nint d, int hfield )
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
        private static void Capture_Current_Frame( nint ptr )
        {

        }


        public Native()
        {
            NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, ( lib, asm, _ ) =>
            {
                if (hlibc == 0 && !OperatingSystem.IsWindows())
                {
                    if (!NativeLibrary.TryLoad("libc.so", out hlibc))
                    {
                        hlibc = NativeLibrary.Load("libc.so.6");
                    }
                }

                if (lib.StartsWith("libc.so"))
                {
                    return hlibc;
                }
                return default;
            });

            InitializeAsm();
        }

        public static nint GetLibhlSymbol( string name )
        {
            return GetLibhlSymbolFunc(name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private static readonly Dictionary<string, nint> cachedHDLLs = [];

        private static nint orig_hlc_resolve_symbol;

        [UnmanagedCallersOnly]
        private static char* ResolveHLCSymbolNative( void* addr, char* out_str, int* outSize )
        {
            return ResolveHLCSymbol(addr, out_str, outSize);
        }

        public static char* ResolveHLCSymbol( void* addr, char* out_str, int* outSize )
        {
            var naddr = (nint)addr;
            var out_span = new Span<char>(out_str, *outSize);
            out_span.Clear();
            char* result;
            result = ((delegate* unmanaged< void*, char*, int*, char* >)orig_hlc_resolve_symbol)(addr, out_str, outSize);
            string? orig_str = null;
            if (result != null)
            {
                orig_str = new(out_str);
            }

            GetHLCodeRange(out var low, out var high);

            if (naddr < low || naddr > high)
            {
                return null;
            }

            var functions = Current.hlc_functions;
            var fit = functions[0];

            foreach (var v in functions)
            {
                if (v.startPtr <= naddr)
                {
                    fit = v;
                }
                else
                {
                    break;
                }
            }

            var fidx = fit.fidx;

            var resolve_result = EventSystem.BroadcastEvent<IOnResolveHLCSymbol, IOnResolveHLCSymbol.Data, string>(new(naddr, fidx));

            StringBuilder sb = new();

            if (resolve_result.HasValue && !string.IsNullOrEmpty(resolve_result.Value))
            {
                sb.Append(resolve_result.Value);
            }
            else
            {
                sb.Append("hlc$fidx_");
                sb.Append(fidx);
            }

            if (!string.IsNullOrEmpty(orig_str))
            {
                sb.Append(":(");
                sb.Append(orig_str);
                sb.Append(')');
            }
            sb.Append(":(addr: +0x");
            sb.Append((naddr - fit.startPtr).ToString("x"));
            sb.Append(')');

            var str = sb.ToString();

            *outSize = out_span.Length;

            if (str.Length < out_span.Length)
            {
                *outSize = str.Length;
            }

            str.TryCopyTo(out_span);

            out_span[*outSize] = (char)0;
            return out_str;
        }

        [UnmanagedCallersOnly]
        private static nint ResolveHLCLibrary( byte* lib, byte* name )
        {
            var libName = Marshal.PtrToStringUTF8((nint)lib);
            var funcName = Marshal.PtrToStringUTF8((nint)name);
            Debug.Assert(funcName != null);


            if (string.IsNullOrEmpty(libName))
            {
                if (funcName == "fmod" || funcName == "fmodf")
                {
                    funcName = "hlc_" + funcName;
                }
                return GetLibhlSymbol(funcName);
            }

            Debug.Assert(libName != null);

            HLEV_native_resolve_event ev = new()
            {
                libName = lib,
                functionName = name,
            };
            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(new(
                IOnNativeEvent.EventId.HL_EV_RESOLVE_NATIVE, (nint)(&ev)));

            if (ev.result == null)
            {
                if (!cachedHDLLs.TryGetValue(libName, out var hlib))
                {
                    ev.functionName = null;

                    EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(new(
                    IOnNativeEvent.EventId.HL_EV_RESOLVE_NATIVE, (nint)(&ev)));

                    Debug.Assert(ev.result != null);
                    hlib = (nint)ev.result;
                    cachedHDLLs[libName] = hlib;
                }

                var hlp = NativeLibrary.GetExport(hlib, "hlp_" + funcName);
                nint _r = 0;

                return ((delegate* unmanaged< nint*, nint >)hlp)(&_r);
            }


            return (nint)ev.result;
        }

        protected virtual void InitializeNativeHooks()
        {
            var phLibhl = LoadLibrary("libhl");

            CreateNativeHookForHL("hl_module_alloc", nameof(Hook_hl_module_alloc), out orig_hl_module_alloc);
            CreateNativeHookForHL("hl_code_read", nameof(Hook_hl_code_read), out orig_hl_code_read);
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

            CreateNativeHookForHL("hl_dyn_compare", nameof(Hook_hl_dyn_compare), out orig_hl_dyn_compare);

            CreateNativeHookForHL("op_jump", nameof(Hook_jit_op_jump), out orig_jit_op_jump);

            CreateNativeHookForHL("hl_fatal_error", nameof(Hook_hl_fatal_error), out orig_hl_fatal_error);

            CreateNativeHookForHL("module_capture_stack", nameof(Hook_module_capture_stack), out orig_module_capture_stack);

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
        public virtual void InitializeGame( ReadOnlySpan<byte> hlboot, out VMContext context )
        {
            HL_code* code;
            byte* err;
            context = new();
            var ctx = (VMContext*)Unsafe.AsPointer(ref context);

            hl_global_init();
            fixed (byte* data = hlboot)
            {
                ctx->code = code = (HL_code*)hl_code_read(data, hlboot.Length, &err);
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

            if (ContextConfig.Config.useHLC)
            {
                RunOnHLC = true;

                var libmodcorenative = LoadLibrary("modcorenative");
                var libhlc = EventSystem.BroadcastEvent<IOnGetCompiledHLC, ReadOnlySpan<byte>, nint>(hlboot).Value;

                Debug.Assert(libhlc != 0);

                HL_module_context* mctx = &ctx->m->ctx;
                ctx->m->functions_ptrs = (void**)NativeLibrary.GetExport(libhlc, "hl_functions_ptrs");

                var hlc_types = new ReadOnlySpan<nint>(hlc_instance_types = (HL_type**)NativeLibrary.GetExport(libhlc, "hl_instance_types"), code->ntypes);

                hlc_global_data = (void**)NativeLibrary.GetExport(libhlc, "hlc_global_data");
                ctx->m->globals_data = hlc_global_data[0]; // First

                hl_alloc_init(&mctx->alloc);

                hl_module_init_indexes(ctx->m);

                mctx->functions_ptrs = ctx->m->functions_ptrs;
                mctx->functions_types = (HL_type**)NativeLibrary.GetExport(libhlc, "hl_functions_types");

                *(void**)NativeLibrary.GetExport(libhlc, "hl_resolve_native_library") = (delegate* unmanaged< byte*, byte*, nint >)&ResolveHLCLibrary;
                *(void**)NativeLibrary.GetExport(libhlc, "hl_get_thread") = (void*)GetLibhlSymbol("hl_get_thread");
                *(void**)NativeLibrary.GetExport(libhlc, "hlc_setjmp") = *(void**)NativeLibrary.GetExport(libmodcorenative, "ptr_setjmp");

                orig_hlc_resolve_symbol = NativeLibrary.GetExport(libmodcorenative, "hlc_resolve_symbol");
                orig_module_capture_stack = NativeLibrary.GetExport(libmodcorenative, "hlc_capture_stack");

                ((delegate* unmanaged< nint, nint, nint, nint, nint, void >)NativeLibrary.GetExport(libmodcorenative, "hlc_setup_callback"))(
                    (nint)(delegate* unmanaged< void*, char*, int*, char* >)&ResolveHLCSymbolNative,
                    (nint)(delegate* unmanaged< void**, int, int >)&Hook_module_capture_stack,
                    NativeLibrary.GetExport(libhlc, "hlc_static_call"),
                    NativeLibrary.GetExport(libhlc, "hlc_get_wrapper"),
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? asm_custom_longjump : 0
                    );

                for (var i = 0; i < ctx->m->code->nfunctions; i++)
                {
                    HL_function* f = ctx->m->code->functions + i;
                    ctx->m->functions_indexes[f->findex] = i;
                }

                ((delegate* unmanaged< HL_module_context*, void >)(NativeLibrary.GetExport(libhlc, "hlc_init_types")))(mctx);

                ((delegate* unmanaged< void >)(NativeLibrary.GetExport(libhlc, "hlc_init_hashes")))();
                ((delegate* unmanaged< void >)(NativeLibrary.GetExport(libhlc, "hlc_init_roots")))();

                for (int i = 0; i < hlc_types.Length; i++)
                {
                    ref var src = ref *(HL_type*)hlc_types[i];

                    if (src.kind == TypeKind.HOBJ)
                    {
                        _ = hl_get_obj_proto((HL_type*)hlc_types[i]);
                    }

                    ref var dst = ref code->types[i];

                    dst = src;
                }

                ctx->c.type = mctx->functions_types[ctx->m->code->entrypoint];

                var funcCount = ctx->m->code->nfunctions;
                var funcPtrSpan = new ReadOnlySpan<nint>(ctx->m->functions_ptrs, funcCount);

                for (int i = 0; i < funcPtrSpan.Length; i++)
                {
                    hlc_functions.Add((i, funcPtrSpan[i]));
                }

                hlc_functions.Sort(( a, b ) => (int)(a.startPtr - b.startPtr));
            }
            else
            {
                RunOnHLC = false;
                if (hl_module_init(ctx->m, 0, 0) == 0)
                {
                    throw new InvalidProgramException("Failed to init module");
                }
                ctx->c.type = ctx->code->functions[ctx->m->functions_indexes[ctx->m->code->entrypoint]].type;
            }


            EventSystem.BroadcastEvent<IOnNativeEvent, IOnNativeEvent.Event>(
                    new(IOnNativeEvent.EventId.HL_EV_VM_READY, (nint)ctx));


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

        public virtual string[] GetDisplayDevices()
        {
            return [];
        }

        public abstract void MakePageWritable( nint ptr, out int old );
        public abstract void RestorePageProtect( nint ptr, int val );
        public abstract ReadOnlySpan<byte> GetHlbootDataFromExe( string exePath );
        public abstract bool IsBadPtr( nint ptr );

        /// <summary>
        /// Resolve the load base address of a native library handle returned by
        /// <see cref="LoadLibrary(string)"/>.
        /// <para>
        /// On Windows the handle (HMODULE) is already the module base.
        /// On glibc Linux <c>dlopen</c> returns a <c>link_map*</c> whose first
        /// field (<c>l_addr</c>) is the load base.
        /// </para>
        /// Platform backends (e.g. Android Bionic, where the handle is an opaque
        /// <c>soinfo*</c>) override this to provide a correct value.
        /// </summary>
        public virtual nint GetModuleBaseAddress( nint libHandle )
        {
            if (OperatingSystem.IsWindows())
            {
                return libHandle;
            }
            // glibc: first field of link_map is l_addr (the load base).
            return Marshal.ReadIntPtr(libHandle);
        }
    }
}

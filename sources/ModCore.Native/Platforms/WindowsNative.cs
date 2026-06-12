extern alias iced;
using Hashlink;
using iced::Iced.Intel;
using ModCore.Storage;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Diagnostics.Debug;
using static iced::Iced.Intel.AssemblerRegisters;
using static Windows.Win32.PInvoke;

#pragma warning disable CA1416

namespace ModCore.Native
{
    [SupportedOSPlatform("windows")]
    internal unsafe partial class WindowsNative : Native
    {
        [LibraryImport("modcorenative", EntryPoint = "init_veh")]
        private static partial void InitVEH( nint createDumpCommand );

        public override void MakePageWritable( nint ptr, out int old )
        {
            var pageStart = ptr & ~(Environment.SystemPageSize - 1);
            VirtualProtect((void*)pageStart, (nuint)Environment.SystemPageSize,
                Windows.Win32.System.Memory.PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE, out var oldFlags);
            old = (int)oldFlags;
        }
        public override void RestorePageProtect( nint ptr, int val )
        {
            var pageStart = ptr & ~(Environment.SystemPageSize - 1);
            VirtualProtect((void*)pageStart, (nuint)Environment.SystemPageSize,
                (Windows.Win32.System.Memory.PAGE_PROTECTION_FLAGS)val, out _);
        }

        public override nint GetCurrentThreadStackBase()
        {
            GetCurrentThreadStackLimits(out _, out var highLimit);
            return (nint)highLimit;
        }

        [UnmanagedCallersOnly]
        protected static int Hook_throw_handler( int code )
        {
            return 0;
        }

        public override void InitializeNative()
        {
            base.InitializeNative();

            {
                var dmpPath = FolderInfo.Cache.GetFilePath("crash.dmp");
                if (File.Exists(dmpPath))
                {
                    File.Delete(dmpPath);
                }

                //Console.Error.WriteLine("[DCCMDBG-CRASH]" + dmpPath);

                //var rtRoot = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
                //var createDumpPath = Path.Combine(rtRoot, "createdump.exe");
                //var createDumpCmd = $"\"{createDumpPath}\" -f \"{dmpPath}\" -n";

                //InitVEH(Marshal.StringToHGlobalUni(createDumpCmd));
            }
        }

        public override string[] GetDisplayDevices()
        {
            List<string> devices = [];
            DISPLAY_DEVICEW ddevice = new()
            {
                cb = (uint)sizeof(DISPLAY_DEVICEW)
            };

            uint index = 0;
            while (EnumDisplayDevices(null, index++, ref ddevice, 0))
            {
                devices.Add(new string(ddevice.DeviceString.Value).Trim());
            }
            return [.. devices];
        }

        protected override void InitializeNativeHooks()
        {
            base.InitializeNativeHooks();

            try
            {
                CreateNativeHookForHL("global_handler", nameof(Hook_throw_handler), out _);
            }
            catch (Exception) { }


        }

        public override ReadOnlySpan<byte> GetHlbootDataFromExe( string exePath )
        {
            var hExe = LoadLibraryEx(exePath,
                 Windows.Win32.System.LibraryLoader.LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_AS_DATAFILE |
                 Windows.Win32.System.LibraryLoader.LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_AS_IMAGE_RESOURCE);

            if (hExe.IsInvalid)
            {
                return default;
            }

            var res = FindResource(hExe, "hlboot.dat", "#10");
            if (res.IsNull)
            {
                hExe.Dispose();
                return default;
            }

            var size = SizeofResource(hExe, res);
            var hres = LoadResource(hExe, res);
            if (hres.IsInvalid)
            {
                hExe.Dispose();
                return default;
            }
            var ptr = LockResource(hres);

            hExe.SetHandleAsInvalid();
            hres.SetHandleAsInvalid();
            return new(ptr, (int)size);
        }

        /***
         * This operation must be performed in an unmanaged environment
         */
        protected override void Generate_asm_hl2cs_throw_exception( Assembler c )
        {
            AsmGetTlsDataPtrRax(c, ref tls_template->prev_hl_error_ptr);

            c.mov(rcx, __[rax]);

            c.sub(rsp, 56);

            AsmGetTlsDataPtrRax(c, ref tls_template->hl_throw_ptr);

            c.jmp(__qword_ptr[rax]);
        }

        /***
         * This operation must be performed in an unmanaged environment
         * Get the return address storage location to facilitate return address hijacking
         * 
         */
        protected override void Generate_asm_hl2cs_store_return_ptr( Assembler c )
        {

            AsmGetTlsDataPtrRax(c, ref tls_template->hl2cs_return_pointers);

            c.cmp(rax, 0x1000); //Tls is null
            c.jl(c.F);

            c.mov(r11, __[rax]);
            c.cmp(r11, 0); // null
            c.je(c.F);

            c.mov(r10, __[r11]); // full or overflow
            c.cmp(r10, 1);
            c.je(c.F);

            c.lea(r10, __[rsp + 8]);
            c.mov(__[r11], r10);

            c.add(r11, 8);
            c.mov(__[rax], r11);

            c.AnonymousLabel();

            c.pop(rax);
            c.jmp(__qword_ptr[rax]);
        }

        /**
         * 
         * void* result = trap_filter(t, ctx, v);
         * if(result < 0xff) {
         *  return orig(t, ctx, v);
         * }
         * 
         * RestoreStack(); 
         * 
         */
        protected override void Generate_asm_hook_break_on_trap_Entry( Assembler c )
        {
            var fallback = c.CreateLabel();

            c.push(rcx);
            c.push(rdx);
            c.push(r8);
            c.push(r9);

            c.sub(rsp, 40);

            c.mov(rax, (long)&Data->trap_filter);
            c.mov(r11, __[rax]);
            c.call(r11);

            c.add(rsp, 40);

            c.cmp(rax, 0xff);
            c.jl(fallback);

            // Restore Execution Context
            // This operation must be performed in an unmanaged environment

            c.mov(rsp, __[rax + 16]);

            c.pop(r10); //Checksum
            c.cmp(r10, STACK_CHUCK_SUM);
            Assert(c);

            c.pop(r15);
            c.pop(r14);
            c.pop(r13);
            c.pop(r12);
            c.pop(rsi);
            c.pop(rdi);
            c.pop(rbp);
            c.pop(rbx);

            c.pop(r10); //Checksum
            c.cmp(r10, STACK_CHUCK_SUM);
            Assert(c);

            c.pop(rax);

            c.pop(r10); //Checksum
            c.cmp(r10, STACK_CHUCK_SUM);
            Assert(c);

            c.pop(r11); //!!!!!!!!!!!!

            c.mov(rsp, rax);

            c.mov(rax, (long)&Data->return_from_managed);

            c.mov(__[rsp], r11); // Fix return ptr
                                 // It's dangerous but effective

            c.jmp(__qword_ptr[rax]);

            c.AnonymousLabel();
            c.int3();


            // Fallback

            c.Label(ref fallback);
            c.pop(r9);
            c.pop(r8);
            c.pop(rdx);
            c.pop(rcx);

            c.mov(rax, (long)&Data->orig_break_on_trap);
            c.jmp(__qword_ptr[rax]);
        }

        protected override void Generate_asm_cs_hl_store_context( Assembler c )
        {
            c.pop(r11); //Data Table Pointer
            c.mov(r10, __[rsp]); // Return IP

            c.mov(rax, rsp); //Original Stack

            c.mov(rsp, __[r11]); //Register Store

            c.push(r10); // Return IP

            c.mov(r10, STACK_CHUCK_SUM);
            c.push(r10); //Checksum

            c.push(rax); //RSP

            c.push(r10); //Checksum

            c.push(rbx);
            c.push(rbp);
            c.push(rdi);
            c.push(rsi);
            c.push(r12);
            c.push(r13);
            c.push(r14);
            c.push(r15);

            //Checksum
            c.push(r10);

            c.mov(__[r11 + 16], rsp); //Save rsp (Register store)

            c.mov(rsp, rax);

            c.jmp(__qword_ptr[r11 + 8]);//Target 
        }

        public override void FixThreadCurrentStackFrame( HL_thread_info* t )
        {
            if (!Environment.Is64BitProcess)
            {
                throw new PlatformNotSupportedException();
            }
            if (t->thread_id == GetCurrentThreadId())
            {
                t->stack_cur = &t;
                return;
            }
            using var th = OpenThread_SafeHandle(Windows.Win32.System.Threading.THREAD_ACCESS_RIGHTS.THREAD_GET_CONTEXT
                | Windows.Win32.System.Threading.THREAD_ACCESS_RIGHTS.THREAD_SUSPEND_RESUME, false, (uint)t->thread_id);


            SuspendThread(th);

            CONTEXT* context = stackalloc CONTEXT[1];

            context->ContextFlags = CONTEXT_FLAGS.CONTEXT_CONTROL_AMD64;

            var err = GetThreadContext(th, ref context[0]);

            Debug.Assert(err != 0);

            var rsp = context->Rsp;

            //var rsp = context.Rsp;
            Debug.Assert(rsp != 0);

            t->stack_cur = (void*)rsp;

            ResumeThread(th);
        }

        public override void SetTlsValue( int index, nint val )
        {
            TlsSetValue((uint)index, (void*)val);
        }

        public override nint GetTlsValue( int index )
        {
            return (nint)TlsGetValue((uint)index);
        }

        protected override void AsmGetTlsDataPtrRax<T>( Assembler c, ref T offset )
        {
            var ofs = (nint)Unsafe.AsPointer(ref offset) - (nint)tls_template;
            var tls_id = Data->tls_slot_index;
            c.push(rcx);
            if (tls_id < 0x40)
            {
                c.mov(rcx, __.gs[5248 + tls_id * 8]);
            }
            else
            {
                c.mov(rax, __.gs[0x1780]);
                c.mov(rcx, __[rax + 8 * (tls_id - 64)]);
            }
            c.lea(rax, __[rcx + (int)ofs]);
            c.pop(rcx);
        }

        public override int AllocTls()
        {
            return (int)TlsAlloc();
        }

        public override bool IsBadPtr( nint ptr )
        {
            return IsBadReadPtr((void*)ptr, 8) && IsBadWritePtr((void*)ptr, 8) && IsBadCodePtr((FARPROC)ptr);
        }
    }
}

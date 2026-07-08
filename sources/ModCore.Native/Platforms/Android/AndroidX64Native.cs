#pragma warning disable CA1416

using Hashlink;
using ModCore.Storage;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ModCore.Native.Platforms.Android
{
    [SupportedOSPlatform("android")]
    internal unsafe partial class AndroidX64Native : LinuxX64Native
    {
        // ── Library loading ─────────────────────────────────────────────

        /// <summary>
        /// Android Bionic does not use versioned SONAMEs (.so.1).
        /// Override to skip the .so.1 attempt that always fails on Android.
        /// </summary>
        public override bool TryLoadLibrary(string path, out nint handle)
        {
            if (NativeLibrary.TryLoad(path, out handle))
                return true;
            if (NativeLibrary.TryLoad(path + ".so", out handle))
                return true;
            if (NativeLibrary.TryLoad(FolderInfo.CurrentNativeRoot.GetFilePath(path), out handle))
                return true;
            if (NativeLibrary.TryLoad(FolderInfo.CurrentNativeRoot.GetFilePath(path + ".so"), out handle))
                return true;
            return false;
        }

        // ── Thread stack ────────────────────────────────────────────────

        /// <summary>
        /// Bionic does not provide pthread_getattr_np on API &lt; 28.
        /// For remote threads we use a conservative fallback.
        /// </summary>
        public override void FixThreadCurrentStackFrame(HL_thread_info* t)
        {
            if (!Environment.Is64BitProcess)
                throw new PlatformNotSupportedException();

            int myTid = (int)GetCurrentThreadId();
            if (t->thread_id == myTid)
            {
                t->stack_cur = &t;
                return;
            }

            // Conservative fallback: mark the whole stack range valid.
            t->stack_cur = (void*)GetCurrentThreadStackBase();
        }

        // ── Pointer validity ────────────────────────────────────────────

        /// <summary>
        /// On Android, /proc/self/maps may be restricted by SELinux.
        /// Fall back to a conservative "assume valid" if procfs is unreadable.
        /// </summary>
        public override bool IsBadPtr(nint ptr)
        {
            if (ptr == 0) return true;
            try
            {
                return base.IsBadPtr(ptr);
            }
            catch
            {
                // /proc/self/maps inaccessible — assume pointer is valid.
                return false;
            }
        }

        // ── Module base address ─────────────────────────────────────────

        [System.Runtime.InteropServices.StructLayout(
            System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct Dl_info
        {
            public nint dli_fname;
            public nint dli_fbase;
            public nint dli_sname;
            public nint dli_saddr;
        }

        [System.Runtime.InteropServices.LibraryImport("libdl.so")]
        private static partial int dladdr(nint addr, Dl_info* info);

        [System.Runtime.InteropServices.LibraryImport("libdl.so", StringMarshalling =
            System.Runtime.InteropServices.StringMarshalling.Utf8)]
        private static partial nint dlsym(nint handle, string symbol);

        /// <summary>
        /// Android Bionic's <c>dlopen</c> returns an opaque <c>soinfo*</c>, so the
        /// glibc <c>link_map.l_addr</c> trick does not apply. Resolve the load base
        /// by taking any exported symbol from the handle and querying
        /// <c>dladdr</c> for its containing module base (<c>dli_fbase</c>).
        /// </summary>
        public override nint GetModuleBaseAddress(nint libHandle)
        {
            // hl libraries always export "hlp_" or standard symbols; probe a few
            // common ones. As a last resort, dlsym on a definitely-present symbol.
            foreach (var probe in _baseProbeSymbols)
            {
                var sym = dlsym(libHandle, probe);
                if (sym != 0)
                {
                    Dl_info info = default;
                    if (dladdr(sym, &info) != 0 && info.dli_fbase != 0)
                    {
                        return info.dli_fbase;
                    }
                }
            }
            throw new PlatformNotSupportedException(
                "Unable to resolve module base address on Android for the given handle.");
        }

        private static readonly string[] _baseProbeSymbols =
        [
            // Standard libhl exports — always present in Hashlink builds.
            "hl_global_init", "hl_alloc_array", "hl_gc_alloc_gen",
            // Additional common exports for robustness.
            "hl_code_read", "hl_module_alloc"
        ];
    }
}

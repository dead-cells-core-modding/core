using Hashlink;
using ModCore.Storage;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ModCore.Native.Platforms.Android
{
    [SupportedOSPlatform("android")]
    internal unsafe class AndroidX64Native : LinuxX64Native
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
    }
}

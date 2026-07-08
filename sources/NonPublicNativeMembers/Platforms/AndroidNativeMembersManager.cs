#pragma warning disable CA1416

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace NonPublicNativeMembers.Platforms
{
    /// <summary>
    /// Android-specific native members manager.
    /// <para>
    /// On Android, <see cref="Process.GetCurrentProcess().Modules"/> may not
    /// reliably enumerate native shared libraries loaded via
    /// <see cref="System.Runtime.InteropServices.NativeLibrary.Load(string)"/>.
    /// This implementation provides a fallback that activates modules by name
    /// without hash verification, relying on the nativemembers JSON that was
    /// pre-generated at build time (via cross-compilation host tools).
    /// </para>
    /// <para>
    /// The <see cref="LinuxNativeMembersManager.Generate"/> path (readelf / objdump)
    /// is inherited but is NOT called at runtime on Android — generation happens
    /// exclusively on the build host.
    /// </para>
    /// </summary>
    [SupportedOSPlatform("android")]
    internal class AndroidNativeMembersManager : LinuxNativeMembersManager
    {
        public override bool LoadAndActivateModule(string moduleName, string? path = null)
        {
            if (IsActivated(moduleName))
                return true;

            // ── Attempt 1: standard path via Process.Modules ──────────
            // On Android, Process.GetCurrentProcess().Modules parses
            // /proc/self/maps and should include loaded .so files.
            try
            {
                var module = Process.GetCurrentProcess().Modules
                    .Cast<ProcessModule>()
                    .FirstOrDefault(m =>
                        GetModuleNameFromPath(m.ModuleName)
                            .Equals(moduleName, StringComparison.OrdinalIgnoreCase));

                if (module != null)
                {
                    var hash = SHA256.HashData(File.ReadAllBytes(module.FileName));
                    if (ActivateModule(moduleName, hash))
                        return true;
                }
            }
            catch
            {
                // Process.Modules may not be available or may not enumerate
                // native libraries on some Android configurations / SELinux
                // policies. Fall through to name-only activation.
            }

            // ── Fallback: activate by name without hash verification ──
            // The nativemembers JSON was pre-generated on the build host
            // and is trusted. Hash verification is skipped on Android
            // because the library file may not be directly readable from
            // the APK or the process module list may be restricted.
            //
            // SECURITY NOTE: Android's integrity depends entirely on the
            // trustworthiness of the nativemembers JSON file. A corrupted
            // or malicious JSON could redirect non-public symbol resolution
            // to arbitrary addresses. Consider JSON-level integrity checks
            // (HMAC or signature) as defense-in-depth for production.
            return ActivateModule(moduleName, null);
        }

        public override bool ActivateModule(string name)
        {
            if (IsActivated(name))
                return true;

            try
            {
                return base.ActivateModule(name);
            }
            catch
            {
                // Same reasoning as LoadAndActivateModule — Process.Modules
                // may be unavailable. Activate by name without hash check.
                return ActivateModule(name, null);
            }
        }
    }
}

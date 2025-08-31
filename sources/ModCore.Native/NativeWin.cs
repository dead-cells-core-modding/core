using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using static Windows.Win32.PInvoke;

#pragma warning disable CA1416 

namespace ModCore.Native
{
    [SupportedOSPlatform("windows")]
    internal unsafe static class NativeWin
    {
        public static ReadOnlySpan<byte> GetHlbootDataFromExe( string exePath )
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
    }
}

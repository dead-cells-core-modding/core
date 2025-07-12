
using Hashlink;
using Hashlink.Proxy.Objects;
using MonoMod.Utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace ModCore
{
    internal static unsafe class Utils
    {

        public static bool MemCmp( ReadOnlySpan<byte> a, ReadOnlySpan<byte> b )
        {
            return a.SequenceEqual(b);
        }

        [DoesNotReturn]
        public static void HashlinkThrow( this Exception ex )
        {
            var err = new HashlinkNETExceptionObj(ex);
            hl_throw((HL_vdynamic*) err.HashlinkPointer);
        }
        public static Type?[] SafeGetAllTypes( this Assembly assembly )
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types;
            }
        }
        public static byte[] HashFile( string path )
        {
            using var fs = File.OpenRead(path);
            return SHA256.HashData(fs);
        }
    }
}

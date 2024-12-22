using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    internal static class Utils
    {
       

        public static bool MemCmp(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static nint GetFrameIP(this StackFrame frame)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_nativeOffset")]
            static extern ref int GetNativeOffset(StackFrame frame);

            var offset = GetNativeOffset(frame);
            return offset + (frame.GetMethod()?.MethodHandle.GetFunctionPointer() ?? 0);
        }

        public static string GetDisplay(this StackFrame frame)
        {
            return frame.ToString();
        }

        public static byte[] HashFile(string path)
        {
            using var fs = File.OpenRead(path);
            return SHA256.HashData(fs);
        }
    }
}

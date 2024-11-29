using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    internal static class Utils
    {
        public static void ExitGame()
        {
            Process.GetCurrentProcess().Kill(true);
        }

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

        public static byte[] HashFile(string path)
        {
            using var fs = File.OpenRead(path);
            return SHA256.HashData(fs);
        }
    }
}

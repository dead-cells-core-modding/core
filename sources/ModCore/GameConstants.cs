using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    public static class GameConstants
    {
        public static string ModCoreHostRoot { get; } = Path.GetDirectoryName(typeof(GameConstants).Assembly.Location)!;
        public static string GamePath { get; } = Process.GetCurrentProcess().Modules[0].FileName;
        public static string GameRoot { get; } = Path.GetDirectoryName(GamePath)!;
        public static string ModCoreRoot { get; } =
            Environment.GetEnvironmentVariable("DCCM_Root") ??
            Path.Combine(GameRoot, "coremod");

        public static string CacheRoot { get; } =
            Environment.GetEnvironmentVariable("DCCM_CacheRoot") ??
            Path.Combine(ModCoreRoot, "cache");

        public static string ConfigRoot { get; } =
            Environment.GetEnvironmentVariable("DCCM_ConfigRoot") ??
            Path.Combine(ModCoreRoot, "config");

        public static string LogsRoot { get; } =
            Environment.GetEnvironmentVariable("DCCM_LogsRoot") ??
            Path.Combine(ModCoreRoot, "logs");

        public static string DataRoot { get; } = 
            Environment.GetEnvironmentVariable("DCCM_DataRoot") ??
            Path.Combine(ModCoreRoot, "data");
        
    }
}

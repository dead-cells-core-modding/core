using System.Runtime.InteropServices;
using System.Text;

namespace ModCore.Storage
{
    /// <summary>
    /// Represents information about a folder
    /// </summary>
    public class FolderInfo
    {
        private static readonly Dictionary<string, FolderInfo> folders = [];
        private static readonly string platform_name = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" : "linux";
        private static readonly string cpu_name = Environment.Is64BitProcess ? "x64" : "x86";

        /// <summary>
        /// A value indicating the folder where the core file is located
        /// </summary>
        public static FolderInfo CoreRoot
        {
            get;
        } = new("CORE_ROOT",
            Path.GetDirectoryName(
                Path.GetDirectoryName(Path.GetDirectoryName(typeof(FolderInfo).Assembly.Location)!)!
                )!
            );
        /// <summary>
        /// A value representing the folder where all native files are located
        /// </summary>
        public static FolderInfo NativeRoot
        {
            get;
        } = new("CORE_NATIVE_ROOT", "{CORE_ROOT}core/native");
        /// <summary>
        /// A value representing the folder where the native files for the current platform are located
        /// </summary>
        public static FolderInfo CurrentNativeRoot
        {
            get;
        } =
            new("CORE_CURRENT_NATIVE_ROOT", "{CORE_NATIVE_ROOT}" +
               platform_name + "-" + cpu_name);
        /// <summary>
        /// A value representing the root directory of the game file
        /// </summary>
        public static FolderInfo GameRoot { get; } = new("GAME_ROOT", "{CORE_ROOT}../");
        /// <summary>
        /// A value indicating the location of the game save file
        /// </summary>
        public static FolderInfo SaveRoot { get; } = new("SAVE_ROOT", "{GAME_ROOT}/save");
        /// <summary>
        /// A value indicating the location of the DCCM log file
        /// </summary>
        public static FolderInfo Logs { get; } = new("CORE_LOGS", "{CORE_ROOT}/logs");
        /// <summary>
        /// A value indicating the location of the configuration file
        /// </summary>
        public static FolderInfo Config { get; } = new("CORE_CONFIG", "{CORE_ROOT}/config");
        /// <summary>
        /// A value indicating the location of the cache file
        /// </summary>
        public static FolderInfo Cache { get; } = new("CORE_CACHE", "{CORE_ROOT}/cache");
        /// <summary>
        /// A value indicating the location of the data file
        /// </summary>
        public static FolderInfo Data { get; } = new("CORE_DATA", "{CORE_ROOT}/data");
        /// <summary>
        /// A value indicating the location of mod
        /// </summary>
        public static FolderInfo Mods { get; } = new("CORE_MODS", "{CORE_ROOT}/mods");
        /// <summary>
        /// A value indicating the location of plugin
        /// </summary>
        public static FolderInfo Plugins { get; } = new("CORE_PLUGINS", "{CORE_ROOT}/plugins");

        /// <summary>
        /// Get the full path of a file in the folder (the file does not necessarily exist)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetFilePath( string name )
        {
            return Path.Combine(FullPath, name);
        }
        /// <summary>
        /// 
        /// </summary>
        public DirectoryInfo Info => info;

        /// <summary>
        /// The name of the folder
        /// </summary>
        public string Name
        {
            get;
        }

        /// <summary>
        /// The full path to the folder
        /// </summary>
        public string FullPath
        {
            get;
        }

        private readonly DirectoryInfo info;


        private static string ParsePath( string path )
        {
            var parts = path.Split('{', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var sb = new StringBuilder();

            for (var i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                var idx = p.IndexOf('}');
                if (idx == -1)
                {
                    sb.Append(p);
                    continue;
                }
                var name = p[..idx];
                var env = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrEmpty(env))
                {
                    sb.Append(env);
                }
                else
                {
                    sb.Append(folders[name.ToUpper()].FullPath);
                    sb.Append(Path.DirectorySeparatorChar);
                }

                if (idx < p.Length - 1)
                {
                    sb.Append(p[(idx + 1)..]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get FolderInfo by name
        /// </summary>
        /// <param name="name">The name of the folder</param>
        /// <returns></returns>
        public static FolderInfo? GetFolder( string name )
        {
            return folders.TryGetValue(name.ToUpper(), out var folder) ? folder : null;
        }

        /// <summary>
        /// Create a FolderInfo
        /// </summary>
        /// <param name="name">The name of the folder</param>
        /// <param name="path">The full path of the folder</param>
        public FolderInfo( string name, string path )
        {
            name = name.ToUpper();
            Name = name;
            var overridePath = Environment.GetEnvironmentVariable("DCCM_OverridePath_" + name);
            if (!string.IsNullOrEmpty(overridePath))
            {
                path = overridePath;
            }

            folders.Add(name, this);
            FullPath = Path.GetFullPath(ParsePath(path));
            info = new(FullPath);
            info.Create();
        }

    }
}

using System.Runtime.InteropServices;
using System.Text;

namespace ModCore.Storage
{
    public class FolderInfo
    {
        private static readonly Dictionary<string, FolderInfo> folders = [];

        public static FolderInfo CoreRoot
        {
            get;
        } = new("CORE_ROOT",
            Path.GetDirectoryName(
                Path.GetDirectoryName(Path.GetDirectoryName(typeof(FolderInfo).Assembly.Location)!)!
                )!
            );
        public static FolderInfo CoreNativeRoot
        {
            get;
        } =
            new("CORE_NATIVE_ROOT", "{CORE_ROOT}core/native/" +
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x86" : "linux-x64"));

        public static FolderInfo GameRoot { get; } = new("GAME_ROOT", "{CORE_ROOT}../");

        public static FolderInfo Logs { get; } = new("CORE_LOGS", "{CORE_ROOT}/logs");
        public static FolderInfo Config { get; } = new("CORE_CONFIG", "{CORE_ROOT}/config");
        public static FolderInfo Cache { get; } = new("CORE_CACHE", "{CORE_ROOT}/cache");
        public static FolderInfo Data { get; } = new("CORE_DATA", "{CORE_ROOT}/data");
        public static FolderInfo Mods { get; } = new("CORE_MODS", "{CORE_ROOT}/mods");
        public static FolderInfo Plugins { get; } = new("CORE_PLUGINS", "{CORE_ROOT}/plugins");


        public string GetFilePath( string name )
        {
            return Path.Combine(FullPath, name);
        }
        public DirectoryInfo Info => info;
        public string Name
        {
            get;
        }
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

        public static FolderInfo? GetFolder( string name )
        {
            return folders.TryGetValue(name.ToUpper(), out var folder) ? folder : null;
        }

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

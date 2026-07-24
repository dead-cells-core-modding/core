using Newtonsoft.Json;
using NonPublicNativeMembers.Platforms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace NonPublicNativeMembers
{
    public abstract class NativeMembersManager
    {
        public static NativeMembersManager Create()
        {
            // Cross-compilation override: when scanning native libraries for a
            // different target platform (e.g., Android .so files from a Windows
            // build host), set DCCM_NATIVE_MEMBERS_PLATFORM to the TARGET
            // platform's name. This bypasses host-platform dispatch and returns
            // the correct manager for ELF (linux/android) vs PDB (windows).
            var overridePlatform = Environment.GetEnvironmentVariable("DCCM_NATIVE_MEMBERS_PLATFORM");
            if (string.Equals(overridePlatform, "linux", StringComparison.OrdinalIgnoreCase))
#pragma warning disable CA1416 // 验证平台兼容性
                return new LinuxNativeMembersManager();
#pragma warning restore CA1416 // 验证平台兼容性

            // Android is checked first because its runtime also reports
            // IsOSPlatform(OSPlatform.Linux) == true, but we need the
            // Android-specific activation path.
            if (OperatingSystem.IsAndroid())
            {
                return new AndroidNativeMembersManager();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsNativeMembersManager();
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxNativeMembersManager();
            }
            throw new PlatformNotSupportedException();
        }

        protected NativeMembersData data = new();
        private readonly Dictionary<string, NativeMembersData.ModuleInfo> activeModules = [];

        protected string GetModuleNameFromPath(string path)
        {
            var fn = Path.GetFileNameWithoutExtension(path);
            if (fn.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                fn.EndsWith(".so", StringComparison.OrdinalIgnoreCase) ||
                fn.Contains(".so.", StringComparison.OrdinalIgnoreCase))
            {
                return GetModuleNameFromPath(fn);
            }
            return fn;
        }

        public abstract void Generate( params string[] modules );

        public void LoadFromFile( string filePath )
        {
            var d = JsonConvert.DeserializeObject<NativeMembersData>(File.ReadAllText(filePath))!;
            foreach (var v in d.Modules)
            {
                data.Modules.Add(v);
            }
        }
        public byte[] Save()
        {
            return Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(data)
                );
        }
        public void Clear()
        {
            data = new();
            activeModules.Clear();
        }
        public virtual bool IsActivated( string moduleName )
        {
            return activeModules.ContainsKey(moduleName);
        }
        public virtual bool LoadAndActivateModule( string moduleName, string? path = null )
        {
            if (IsActivated(moduleName))
            {
                return true;
            }
            var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                .FirstOrDefault(m => GetModuleNameFromPath(m.ModuleName)
                    .Equals(moduleName, StringComparison.OrdinalIgnoreCase));
            Debug.Assert(module != null);

            var hash = SHA256.HashData(File.ReadAllBytes(module.FileName));
            moduleName = GetModuleNameFromPath(moduleName);

            if (!ActivateModule(moduleName, hash))
            {
                Generate(module.FileName);
                if (!ActivateModule(moduleName, hash))
                {
                    return false;
                }
            }
            return true;
        }
        public virtual bool ActivateModule( string name )
        {
            if (IsActivated(name))
            {
                return true;
            }
            var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                .FirstOrDefault(m => Path.GetFileNameWithoutExtension(m.ModuleName)
                    .Equals(name, StringComparison.OrdinalIgnoreCase));
            Debug.Assert(module != null);

            var hash = SHA256.HashData(File.ReadAllBytes(module.FileName));
            name = Path.GetFileNameWithoutExtension(name);
            return ActivateModule(name, hash);
        }
        public virtual bool ActivateModule( string name, byte[]? hash256 )
        {
            if (IsActivated(name))
            {
                return true;
            }

            var info = data.Modules.Where(x => x.Name == name)
                                    .FirstOrDefault(
                                    x => hash256?.SequenceEqual(x.Hash) ?? true
                );
            if (info == null)
            {
                return false;
            }
            activeModules.Add(name, info);
            return true;
        }

        public NativeMembersData.MemberInfo? Resolve( string name )
        {
            foreach (var v in activeModules)
            {
                if (v.Value.Members.TryGetValue(name, out var m))
                {
                    return m;
                }
            }
            return null;
        }

        public NativeMembersData Data => data;
    }
}

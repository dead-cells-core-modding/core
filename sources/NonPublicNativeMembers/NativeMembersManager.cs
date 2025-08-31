using Newtonsoft.Json;
using NonPublicNativeMembers.Win;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace NonPublicNativeMembers
{
    public abstract class NativeMembersManager
    {
        public static NativeMembersManager Create()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsNativeMembersManager();
            }
            throw new PlatformNotSupportedException();
        }

        protected NativeMembersData data = new();
        private readonly Dictionary<string, NativeMembersData.ModuleInfo> activeModules = [];

        public abstract void Generate( params string[] modules );

        public void LoadFromFile(string filePath)
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
            if (!ActivateModule(moduleName, string.IsNullOrEmpty(path) ?
                null : SHA256.HashData(File.ReadAllBytes(path))))
            {
                return false;
            }
            return true;
        }
        public virtual bool ActivateModule( string name )
        {
            return ActivateModule(name, null);
        }
        public virtual bool ActivateModule( string name, byte[]? hash256 )
        {
            if (IsActivated(name))
            {
                return true;
            }
            var info = data.Modules.Where(x => x.Name == name)
                                    .FirstOrDefault(x => hash256?.SequenceEqual(x.Hash) ?? true);
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

using SharpPdb.Native;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

#pragma warning disable CA1416

namespace NonPublicNativeMembers.Win
{
    [SupportedOSPlatform("windows")]
    internal unsafe class WindowsNativeMembersManager : NativeMembersManager
    {
        public override void Generate( params string[] modules )
        {
            foreach (var v in modules)
            {
                using var pdb = new PdbFileReader(Path.ChangeExtension(v, "pdb"));

                var pdbGuid = pdb.PdbFile.InfoStream.Header.Guid;

                var moduleContent = File.ReadAllBytes(v);

                if (moduleContent.AsSpan().IndexOf(pdbGuid.ToByteArray()) == -1)
                {
                    throw new InvalidOperationException("PDB and module mismatch.");
                }

                var moduleName = Path.GetFileNameWithoutExtension(v);
                var moduleInfo = new NativeMembersData.ModuleInfo()
                {
                    Name = moduleName,
                    Hash = SHA256.HashData(moduleContent)
                };
                data.Modules.Add(moduleInfo);
                

                foreach (var f in pdb.Functions)
                {
                    moduleInfo.Members[f.Name] = new()
                    {
                        Name = f.Name,
                        ModuleName = moduleName,
                        IsFunction = true,
                        RVA = f.RelativeVirtualAddress
                    };
                }
                foreach (var gv in pdb.GlobalVariables)
                {
                    moduleInfo.Members[gv.Name] = new()
                    {
                        Name = gv.Name,
                        ModuleName = moduleName,
                        IsFunction = false,
                        RVA = gv.RelativeVirtualAddress
                    };
                }
            }

           
        }

        public override bool LoadAndActivateModule( string moduleName, string? path = null )
        {
            if (IsActivated(moduleName))
            {
                return true;
            }
            if (!NativeLibrary.TryLoad(moduleName, out var hDll))
            {
                return false;
            }
            char* nameBuf = stackalloc char[1024];
            _ = GetModuleFileName(new HMODULE(hDll), new PWSTR(nameBuf), 1024);
            var dllPath = new string(nameBuf);
            var hash = SHA256.HashData(File.ReadAllBytes(dllPath));
            moduleName = Path.GetFileNameWithoutExtension(moduleName);
            if (!ActivateModule(moduleName, hash))
            {
                Generate(dllPath);
                if (!ActivateModule(moduleName, hash))
                {
                    return false;
                }
            }
            return true;
        }
        public override bool ActivateModule( string name )
        {
            if (IsActivated(name))
            {
                return true;
            }
            var hDll = NativeLibrary.Load(name);
            char* nameBuf = stackalloc char[1024];
            _ = GetModuleFileName(new HMODULE(hDll), new PWSTR(nameBuf), 1024);
            var dllPath = new string(nameBuf);
            return ActivateModule(name, SHA256.HashData(File.ReadAllBytes(dllPath)));
        }
    }
}

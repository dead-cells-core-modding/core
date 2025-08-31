using SharpPdb.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
                var moduleName = Path.GetFileName(v);
                var moduleInfo = new NativeMembersData.ModuleInfo()
                {
                    Name = moduleName,
                    Hash = SHA256.HashData(File.ReadAllBytes(v))
                };
                data.Modules.Add(moduleInfo);
                using var pdb = new PdbFileReader(Path.ChangeExtension(v, "pdb"));

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

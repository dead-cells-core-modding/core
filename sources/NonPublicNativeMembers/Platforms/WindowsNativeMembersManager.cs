using SharpPdb.Native;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

#pragma warning disable CA1416

namespace NonPublicNativeMembers.Platforms
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
    }
}

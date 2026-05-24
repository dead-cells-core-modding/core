#pragma warning disable CA1416

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NonPublicNativeMembers.Platforms
{
    [SupportedOSPlatform("linux")]
    internal unsafe partial class LinuxNativeMembersManager : NativeMembersManager
    {
        [GeneratedRegex(@"^([0-9a-fA-F]{16})\s+([lgwu])\s+(.)\s+(\S+)\s+([0-9a-fA-F]{16})\s+(.+)$")]
        private static partial Regex ObjdumpOutputRegex();
        public override void Generate( params string[] modules )
        {
            var regex = ObjdumpOutputRegex();
            foreach (var v in modules)
            {
                var moduleContent = File.ReadAllBytes(v);

                var moduleName = Path.GetFileNameWithoutExtension(v);
                var moduleInfo = new NativeMembersData.ModuleInfo()
                {
                    Name = moduleName,
                    Hash = SHA256.HashData(moduleContent)
                };
                data.Modules.Add(moduleInfo);

                var proc = Process.Start(new ProcessStartInfo()
                {
                    FileName = "objdump",
                    Arguments = $"-t \"{v}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });

                Debug.Assert(proc != null);

                StringBuilder outputBuilder = new();
                proc.OutputDataReceived += (sender, e) =>
                {
                    var result = regex.Match(e.Data ?? "");
                    if (result.Success)
                    {
                        var rva = Convert.ToUInt64(result.Groups[1].Value, 16);
                        var name = result.Groups[6].Value;
                        var flag = result.Groups[3].Value;

                        if(rva > 0 && !string.IsNullOrEmpty(name) &&
                            (flag == "O" || flag == "F"))
                        {
                            moduleInfo.Members[name] = new()
                            {
                                Name = name,
                                ModuleName = moduleName,
                                IsFunction = flag == "F",
                                RVA = rva
                            };
                        }
                    }
                };
                proc.BeginOutputReadLine();
                proc.WaitForExit();



                // Linux does not have a standard debug info format like PDB, so we cannot reliably extract member information.
                // Instead, we will just store the module information and rely on the user to provide correct hashes for verification.
            }
        }
    }
}
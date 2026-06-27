#pragma warning disable CA1416

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace NonPublicNativeMembers.Platforms
{
    [SupportedOSPlatform("linux")]
    internal unsafe partial class LinuxNativeMembersManager : NativeMembersManager
    {
        /// <summary>
        /// Regex for parsing <c>readelf -s -W</c> output (preferred, stable column format).
        /// Format: Num: Value Size Type Bind Vis Ndx Name
        /// Example:     22: 0000000000001119    26 FUNC    GLOBAL DEFAULT   14 public_func
        /// </summary>
        [GeneratedRegex(@"^\s*\d+:\s+([0-9a-fA-F]{16})\s+(\d+)\s+(FUNC|OBJECT)\s+\S+\s+\S+\s+\S+\s+(.+)$")]
        private static partial Regex ReadelfOutputRegex();

        /// <summary>
        /// Regex for parsing <c>objdump -t</c> output (fallback).
        /// Format: address(16) flags(7) section size(16) name
        /// Example: 0000000000001133 l     F .text  000000000000001a break_on_trap
        /// </summary>
        [GeneratedRegex(@"^([0-9a-fA-F]{16})\s+([lgwu])\s+(.)\s+(\S+)\s+([0-9a-fA-F]{16})\s+(.+)$")]
        private static partial Regex ObjdumpOutputRegex();

        private static bool IsAllDigits( ReadOnlySpan<char> s )
        {
            foreach (var c in s)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return s.Length > 0;
        }

        public override void Generate( params string[] modules )
        {
            // Prefer readelf (stable, semantic column format), fall back to objdump.
            var readelfRegex = ReadelfOutputRegex();
            var objdumpRegex = ObjdumpOutputRegex();

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

                // ── Attempt 1: readelf -s -W ────────────────────────────
                if (TryGetSymbolsViaReadelf(v, moduleInfo, readelfRegex))
                {
                    continue;
                }

                // ── Attempt 2: objdump -t (fallback) ────────────────────
                TryGetSymbolsViaObjdump(v, moduleInfo, objdumpRegex);
            }
        }

        /// <summary>
        /// Scan symbols using <c>readelf -s -W</c>.
        /// Returns true on success, false if the tool is unavailable or fails.
        /// </summary>
        private static bool TryGetSymbolsViaReadelf(
            string modulePath,
            NativeMembersData.ModuleInfo moduleInfo,
            Regex regex )
        {
            try
            {
                using var proc = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = "readelf",
                        Arguments = $"-s -W \"{modulePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                    }
                };

                if (!proc.Start())
                    return false;

                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0 || string.IsNullOrEmpty(output))
                    return false;

                foreach (var line in output.Split('\n'))
                {
                    var result = regex.Match(line);
                    if (result.Success)
                    {
                        var rva = Convert.ToUInt64(result.Groups[1].Value, 16);
                        var symbolType = result.Groups[3].Value; // "FUNC" or "OBJECT"
                        var name = result.Groups[4].Value;

                        // Strip readelf's version index, e.g. "puts@GLIBC_2.2.5 (2)" -> "puts@GLIBC_2.2.5"
                        var versionParen = name.LastIndexOf(" (");
                        if (versionParen > 0 && name.EndsWith(')') &&
                            IsAllDigits(name.AsSpan(versionParen + 2, name.Length - versionParen - 3)))
                        {
                            name = name[..versionParen];
                        }

                        if (rva > 0 && !string.IsNullOrEmpty(name))
                        {
                            moduleInfo.Members[name] = new()
                            {
                                Name = name,
                                ModuleName = moduleInfo.Name,
                                IsFunction = symbolType == "FUNC",
                                RVA = rva
                            };
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Scan symbols using <c>objdump -t</c> (fallback tool).
        /// Returns true on success, false if the tool is unavailable or fails.
        /// </summary>
        private static bool TryGetSymbolsViaObjdump(
            string modulePath,
            NativeMembersData.ModuleInfo moduleInfo,
            Regex regex )
        {
            try
            {
                using var proc = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = "objdump",
                        Arguments = $"-t \"{modulePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                    }
                };

                if (!proc.Start())
                    return false;

                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0 || string.IsNullOrEmpty(output))
                    return false;

                foreach (var line in output.Split('\n'))
                {
                    var result = regex.Match(line);
                    if (result.Success)
                    {
                        var rva = Convert.ToUInt64(result.Groups[1].Value, 16);
                        var name = result.Groups[6].Value;
                        var flag = result.Groups[3].Value;

                        if (rva > 0 && !string.IsNullOrEmpty(name) &&
                            (flag == "O" || flag == "F"))
                        {
                            moduleInfo.Members[name] = new()
                            {
                                Name = name,
                                ModuleName = moduleInfo.Name,
                                IsFunction = flag == "F",
                                RVA = rva
                            };
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
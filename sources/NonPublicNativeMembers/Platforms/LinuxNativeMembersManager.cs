#pragma warning disable CA1416

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace NonPublicNativeMembers.Platforms
{
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("windows")]
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

        // ── Tool resolution ─────────────────────────────────────────
        // On Windows, GNU readelf/objdump are not available.  The Android NDK
        // ships LLVM equivalents (llvm-readelf, llvm-objdump) that produce
        // compatible output.  We probe them first, falling back to the bare
        // tool names (which work on Linux / Git Bash / MSYS2).

        private static readonly string[] s_readelfProbes = ResolveElfToolProbes("readelf");
        private static readonly string[] s_objdumpProbes  = ResolveElfToolProbes("objdump");

        private static string[] ResolveElfToolProbes(string baseName)
        {
            var probes = new List<string>();

            // NDK LLVM toolchain (shipped with Android NDK, works on all hosts)
            var ndkHome = Environment.GetEnvironmentVariable("ANDROID_NDK_HOME");
            if (!string.IsNullOrEmpty(ndkHome))
            {
                string hostTag = OperatingSystem.IsWindows() ? "windows-x86_64" :
                                 OperatingSystem.IsMacOS()  ? "darwin-x86_64"  :
                                                              "linux-x86_64";
                var exeExt = OperatingSystem.IsWindows() ? ".exe" : "";
                var llvmBin = Path.Combine(ndkHome, "toolchains", "llvm", "prebuilt", hostTag, "bin");
                var ndkPath = Path.Combine(llvmBin, $"llvm-{baseName}{exeExt}");
                if (File.Exists(ndkPath))
                    probes.Add(ndkPath);
            }

            // llvm-* on PATH (installed via package manager)
            probes.Add(OperatingSystem.IsWindows() ? $"llvm-{baseName}.exe" : $"llvm-{baseName}");

            // Bare tool name — works on Linux, Git Bash, MSYS2, or if llvm
            // tools are symlinked without the llvm- prefix.
            probes.Add(baseName);

            return [.. probes];
        }

        private static bool TryStartElfTool(string[] probes, string arguments,
            out string output, out int exitCode)
        {
            foreach (var fileName in probes)
            {
                try
                {
                    using var proc = new Process()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            FileName = fileName,
                            Arguments = arguments,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                        }
                    };

                    if (proc.Start())
                    {
                        output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();
                        exitCode = proc.ExitCode;
                        return true;
                    }
                }
                catch
                {
                    // Tool not found or not executable — try next probe.
                }
            }

            output = "";
            exitCode = -1;
            return false;
        }

        /// <summary>
        /// Scan symbols using <c>readelf -s -W</c> (or <c>llvm-readelf</c>).
        /// Returns true on success, false if the tool is unavailable or fails.
        /// </summary>
        private static bool TryGetSymbolsViaReadelf(
            string modulePath,
            NativeMembersData.ModuleInfo moduleInfo,
            Regex regex )
        {
            try
            {
                if (!TryStartElfTool(s_readelfProbes,
                        $"-s -W \"{modulePath}\"",
                        out var output, out var exitCode))
                    return false;

                if (exitCode != 0 || string.IsNullOrEmpty(output))
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
        /// Scan symbols using <c>objdump -t</c> (or <c>llvm-objdump</c>).
        /// Returns true on success, false if the tool is unavailable or fails.
        /// </summary>
        private static bool TryGetSymbolsViaObjdump(
            string modulePath,
            NativeMembersData.ModuleInfo moduleInfo,
            Regex regex )
        {
            try
            {
                if (!TryStartElfTool(s_objdumpProbes,
                        $"-t \"{modulePath}\"",
                        out var output, out var exitCode))
                    return false;

                if (exitCode != 0 || string.IsNullOrEmpty(output))
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

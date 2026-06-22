using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;


class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.BuildAll);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Target OS Platform to build")]
    readonly string CurrentOSPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                                        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                                        throw new NotSupportedException();

    [Parameter("Target Architecture to build")]
    readonly string CurrentArchPlatform = RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.Arm => "arm",
        _ => throw new PlatformNotSupportedException()
    };

    #region Sources Path

    AbsolutePath HlbootPath => RootDirectory + "/hlboots";
    AbsolutePath SourceRoot => RootDirectory + "/sources";
    AbsolutePath NativeSrcRoot => SourceRoot + "/native";
    AbsolutePath MDKSrcRoot => RootDirectory + "/mdk";
    AbsolutePath DCCMToolSrcProject => MDKSrcRoot + "/DCCMTool" + "/DCCMTool.csproj";

    AbsolutePath GoldbergRoot => RootDirectory + "/3rd/Goldberg" + $"/{CurrentOSPlatform}-{CurrentArchPlatform}"; 

    #endregion

    #region Bin Path

    AbsolutePath BinRoot => RootDirectory + "/bin";
    AbsolutePath CoreBinRoot => BinRoot + "/core";
    AbsolutePath HostBinRoot => CoreBinRoot + "/host";
    AbsolutePath NativeBinRoot => CoreBinRoot + "/native" + $"/{CurrentOSPlatform}-{CurrentArchPlatform}";
    
    AbsolutePath MDKSelfBinRoot => RootDirectory + "/mdk" + "/bin";
    AbsolutePath ProxyBinRoot => MDKSelfBinRoot + "/ref";

    AbsolutePath MDKBinRoot => CoreBinRoot + "/mdk";

    #endregion

    #region Common

    Target GenerateGameProxy => _ => _.Executes(() =>
    {
        var tempDir = Path.GetTempFileName() + ".dir";
        Directory.CreateDirectory(tempDir);

        List<string> dat = [];

        foreach (var v in Directory.EnumerateFiles(HlbootPath, "*.dat", SearchOption.TopDirectoryOnly))
        {
            var name = $"GameProxy_{Path.GetFileNameWithoutExtension(v)}";
            var proxyPath = Path.Combine(tempDir, name + ".dll");
           
            
            DotNetTasks.DotNetRun(s =>
                s.SetProjectFile(DCCMToolSrcProject)
                .EnableNoLaunchProfile()
                .SetConfiguration("Release")
                .SetApplicationArguments(
                    "generate-game-persudo",
                    "-i", v,
                    "-o", tempDir,
                    "-n", name,
                    "-r"));

            dat.Add(proxyPath);
        }

        DotNetTasks.DotNetRun(s =>
                s.SetProjectFile(DCCMToolSrcProject)
                .EnableNoLaunchProfile()
                .SetConfiguration("Release")
                .SetApplicationArguments([
                    "internal",
                    "generate-sub-assembly",
                    ..dat.SelectMany<string, string>(x => [ "-i", x]),
                    "-o", Path.Combine(ProxyBinRoot, "GameProxy.dll")
                    ]));
    });

    #endregion

    #region MDK Build

    Target BuildMDK => _ => _
    .DependsOn(GenerateGameProxy)
    .Executes(() =>
    {
        DotNetTasks.DotNetBuild(s =>
            s.SetProjectFile(MDKSrcRoot + "/mdk.slnx")
            .SetConfiguration("Debug")
            );

        DotNetTasks.DotNetPublish(s =>
            s.SetProject(MDKSrcRoot + "/mdk.slnx")
            .SetConfiguration("Release")
            );

        Log.Information("Copying mdk");

        MDKSelfBinRoot.Copy(MDKBinRoot, ExistsPolicy.MergeAndOverwriteIfNewer);
    });

    #endregion

    #region Native Build

    Target BuildNative => _ => _
    .Executes(() =>
    {
        var cmakePreset = $"{CurrentOSPlatform}-{CurrentArchPlatform}-{Configuration.ToString().ToLower()}";
        ProcessTasks.StartProcess("cmake",
            $". --preset={cmakePreset}",
            NativeSrcRoot).WaitForExit();
        ProcessTasks.StartProcess("cmake",
            $"--build ./out/build/{cmakePreset}",
            NativeSrcRoot).WaitForExit();

        Log.Information("Copying Goldberg");

        GoldbergRoot.GlobFiles("*").ForEach(
            x => {
                Log.Information("Copying {file}", x);
                x.CopyToDirectory(
                    (NativeBinRoot + "/goldberg").CreateDirectory(), ExistsPolicy.FileOverwrite
                );
                });

        Log.Information("Scanning private members");

        string[] scanLibraries = [];

        if(CurrentOSPlatform == "win")
        {
            scanLibraries = [
                "hljit.dll", "libhl.dll"
                ];
        }
        else if(CurrentOSPlatform == "linux")
        {
            scanLibraries = [
                "libhl.so", "hljit.so"
                ];
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        DotNetTasks.DotNetRun(s =>
            s.SetProjectFile(DCCMToolSrcProject)
            .EnableNoLaunchProfile()
            .SetConfiguration("Release")
            .SetApplicationArguments([
                "internal",
                "scan-native-private-member",
                "-b", NativeBinRoot.ToString("d"),
                ..scanLibraries.SelectMany<string, string>(x => ["-i", x]),
                "-o", (NativeBinRoot + "/nativemembers.json")
                ]));
    });


    #endregion

    #region Core Build

    Target BuildCore => _ => _.DependsOn(GenerateGameProxy)
            .DependsOn(BuildNative)
            .Executes(() =>
            {
                DotNetTasks.DotNetBuild(s => s.SetConfiguration(Configuration)
                    .SetProjectFile(SourceRoot + "/DCCMShell"));
                DotNetTasks.DotNetBuild(s => s.SetConfiguration(Configuration)
                    .SetProjectFile(SourceRoot + "/ModCore.ModLoader.Default"));

                DotNetTasks.DotNetPublish(s => s.SetConfiguration(Configuration)
                    .SetProject(SourceRoot + "/SteamStartShell"));
                DotNetTasks.DotNetPublish(s => s.SetConfiguration("Release")
                    .SetProject(SourceRoot + "/DeadCellsModding"));
            });

    Target BuildAssets => _ => _
        .DependsOn(BuildMDK)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s.SetConfiguration(Configuration) 
                .SetProjectFile(SourceRoot + "/ModCore.Assets"));
        });

    #endregion

    Target BuildAll => _ => _.DependsOn(
        BuildNative,
        BuildCore,
        BuildMDK,
        BuildAssets
        )
    ;

}

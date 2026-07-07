using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NSwag;
using Nuke.Common.Utilities.Collections;
using Octokit;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;


class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main()
    {
        HttpTasks.DefaultTimeout = TimeSpan.FromSeconds(600);

        return Execute<Build>(x => x.BuildAll);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Target OS Platform to build")]
    readonly string CurrentOSPlatform = OperatingSystem.IsAndroid() ? "android" :
                                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                                        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                                        throw new NotSupportedException();

    [Parameter("Target Architecture to build")]
    readonly string CurrentArchPlatform = RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.Arm => "arm",
        Architecture.Arm64 => "arm",
        _ => throw new PlatformNotSupportedException()
    };

    #region Sources Path

    AbsolutePath HlbootPath => RootDirectory + "/hlboots";
    AbsolutePath SourceRoot => RootDirectory + "/sources";
    AbsolutePath NativeSrcRoot => SourceRoot + "/native";
    AbsolutePath MDKSrcRoot => RootDirectory + "/mdk";
    AbsolutePath DCCMToolSrcProject => MDKSrcRoot + "/DCCMTool" + "/DCCMTool.csproj";

    AbsolutePath GoldbergRoot => RootDirectory + "/3rd/Goldberg" + $"/{CurrentOSPlatform}-{CurrentArchPlatform}";

    AbsolutePath TinyCCRoot => NativeSrcRoot + "/3rd/tinycc";
    AbsolutePath TinyCCBuildRoot => NativeSrcRoot + "/tinycc";
    AbsolutePath TinyCCWin32Root => TinyCCRoot + "/win32";

    AbsolutePath CrashlinkSrcRoot => RootDirectory + "/3rd/crashlink/crashlink";
    AbsolutePath HlrunSrcRoot => RootDirectory + "/3rd/crashlink/hlrun";

    #endregion

    #region Bin Path

    AbsolutePath TinyCCBinRoot => NativeBinRoot + "/tinycc";

    AbsolutePath BinRoot => RootDirectory + "/bin";
    AbsolutePath CoreBinRoot => BinRoot + "/core";
    AbsolutePath HostBinRoot => CoreBinRoot + "/host";
    AbsolutePath NativeBinRoot => CoreBinRoot + "/native" + $"/{CurrentOSPlatform}-{CurrentArchPlatform}";
    
    AbsolutePath MDKSelfBinRoot => RootDirectory + "/mdk" + "/bin";
    AbsolutePath ProxyBinRoot => MDKSelfBinRoot + "/ref";

    AbsolutePath MDKBinRoot => CoreBinRoot + "/mdk";

    AbsolutePath CrashlinkDstRoot => CoreBinRoot + "/crashlink";

    AbsolutePath WorkshopTemp { get; } = Path.GetTempFileName() + ".dir";

    AbsolutePath WorkshopBinWin64Zip => WorkshopTemp + "/win-x64.zip";
    AbsolutePath WorkshopBinLinux64Zip => WorkshopTemp + "/linux-x64.zip";

    AbsolutePath WorkshopPublishRoot => RootDirectory + "/workshop-publish";

    AbsolutePath WorkshopDummySteamStartShell => WorkshopPublishRoot + "/core/host/startup/steam";
    AbsolutePath WorkshopPublishWin64Content => WorkshopPublishRoot + "/win-x64/content";
    AbsolutePath WorkshopPublishLinux64Content => WorkshopPublishRoot + "/linux-x64/content";

    #endregion

    #region Release Info

    AbsolutePath ReleaseInfoPath => BinRoot + "/ReleaseInfo.md";
    AbsolutePath ReleaseInfoChinesePath => BinRoot + "/ReleaseInfo.zh.md";
    AbsolutePath ReleaseInfoEnglishPath => BinRoot + "/ReleaseInfo.en.md";

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
                    "-i", Path.Combine(tempDir, "GameProxy_hlboot-opengl-steam.dll"),
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
        TinyCCBinRoot.CreateDirectory();

        var cmakePreset = $"{CurrentOSPlatform}-{CurrentArchPlatform}-{Configuration.ToString().ToLower()}";
        ProcessTasks.StartProcess("cmake",
            $". --preset={cmakePreset}",
            NativeSrcRoot).AssertZeroExitCode();
        ProcessTasks.StartProcess("cmake",
            $"--build ./out/build/{cmakePreset}",
            NativeSrcRoot).AssertZeroExitCode();

        if (CurrentOSPlatform != "android")
        {
            Log.Information("Copying Goldberg");

            GoldbergRoot.GlobFiles("*").ForEach(
                x => {
                    Log.Information("Copying {file}", x);
                    x.CopyToDirectory(
                        (NativeBinRoot + "/goldberg").CreateDirectory(), ExistsPolicy.FileOverwrite
                    );
                    });
        }

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
        else if(CurrentOSPlatform == "android")
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
            .DependsOn(PrepareHLC)
            .Executes(() =>
            {
                DotNetTasks.DotNetBuild(s => s.SetConfiguration(Configuration)
                    .SetProjectFile(SourceRoot + "/DCCMShell"));
                DotNetTasks.DotNetBuild(s => s.SetConfiguration(Configuration)
                    .SetProjectFile(SourceRoot + "/ModCore.ModLoader.Default"));

                DotNetTasks.DotNetPublish(s => s.SetConfiguration(Configuration)
                    .SetProject(SourceRoot + "/SteamStartShell"));
                DotNetTasks.DotNetPublish(s => s.SetConfiguration(Configuration)
                    .SetProject(SourceRoot + "/GOGStartShell"));
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

    Target PrepareHLC => _ => _
        .DependsOn(BuildNative)
        .Executes(() =>
        {
            TinyCCBinRoot.CreateDirectory();

            File.Copy(TinyCCRoot + "/include/tccdefs.h", TinyCCBinRoot + "/tccdefs.h", true);
            File.Copy(TinyCCBuildRoot + "/include/core_hlc_inc.h", TinyCCBinRoot + "/core_hlc_inc.h", true);

            // Copy crashlink
            CrashlinkSrcRoot.CopyToDirectory(CrashlinkDstRoot, ExistsPolicy.MergeAndOverwrite);
            HlrunSrcRoot.CopyToDirectory(CrashlinkDstRoot, ExistsPolicy.MergeAndOverwrite);

            foreach(var v in Directory.GetDirectories(CrashlinkDstRoot, "__pycache__",SearchOption.AllDirectories))
            {
                if(Directory.Exists(v))
                {
                    Directory.Delete(v, true);
                }
            }
        });

    #endregion

    Target BuildAll => _ => _.DependsOn(
        BuildNative,
        BuildCore,
        BuildMDK,
        BuildAssets
        )
    ;

    Target DownloadWin64Bin => _ => _.Executes(async () =>
    {
        WorkshopTemp.CreateDirectory();
        Log.Information("Downloading...");

        await HttpTasks.HttpDownloadFileAsync("https://nightly.link/dead-cells-core-modding/core/workflows/build/dev/build-win-x64-Debug.zip", WorkshopBinWin64Zip, 
            System.IO.FileMode.Create);
        WorkshopPublishWin64Content.CreateOrCleanDirectory();

        Log.Information("Extracting...");
        await ZipFile.ExtractToDirectoryAsync(WorkshopBinWin64Zip, WorkshopPublishWin64Content);

        Log.Information("Copying SteamStartShell");
        WorkshopDummySteamStartShell.CreateOrCleanDirectory();

        File.Copy(Path.Combine(WorkshopPublishWin64Content, "ModCoreVersion.txt"), Path.Combine(WorkshopPublishRoot, "ModCoreVersion.txt"), true);
        File.Copy(Path.Combine(WorkshopPublishWin64Content, "core", "host", "startup", "steam", "deadcells.exe"), 
            Path.Combine(WorkshopDummySteamStartShell, "deadcells.exe"), true);
    });

    Target DownloadLinux64Bin => _ => _.Executes(async () =>
    {
        WorkshopTemp.CreateDirectory();
        Log.Information("Downloading...");

        await HttpTasks.HttpDownloadFileAsync("https://nightly.link/dead-cells-core-modding/core/workflows/build/dev/build-linux-x64-Debug.zip", WorkshopBinLinux64Zip,
            System.IO.FileMode.Create);
        WorkshopPublishLinux64Content.CreateOrCleanDirectory();

        Log.Information("Extracting...");
        await ZipFile.ExtractToDirectoryAsync(WorkshopBinLinux64Zip, WorkshopPublishLinux64Content);
    });

    Target GenerateReleaseInfo => _ => _.Executes(async () =>
    {
        ReleaseInfoPath.DeleteFile();
        ReleaseInfoEnglishPath.DeleteFile();
        ReleaseInfoChinesePath.DeleteFile();

        ProcessTasks.StartProcess("opencode", " run -m deepseek/deepseek-v4-pro --dangerously-skip-permissions --format json --command release-info --log-level ERROR", 
            RootDirectory)
            .AssertZeroExitCode();

        Assert.FileExists(ReleaseInfoPath);
        Assert.FileExists(ReleaseInfoEnglishPath);
        Assert.FileExists(ReleaseInfoChinesePath);
    });

    Target Release => _ => _
        .DependsOn(GenerateReleaseInfo)
        .Executes(async () =>
        {
            var msg = await File.ReadAllTextAsync(ReleaseInfoPath);
            var ver = File.ReadAllText(BinRoot + "/ModCoreVersion.txt").Trim();
            File.Copy(ReleaseInfoPath, Path.Combine(RootDirectory, "latest-release.md"), true);
            GitTasks.Git("add latest-release.md");
            GitTasks.Git($"commit -m {("chore: update release info")}");
            GitTasks.Git($"tag v{ver}");
            GitTasks.Git("push origin");
            GitTasks.Git("push origin --tags");
            GitTasks.Git("push origin dev:main");
        });

    Target PublishMAPI => _ => _
        .DependsOn(DownloadWin64Bin)
        .DependsOn(DownloadLinux64Bin)
        .Executes(async () =>
    {
        DotNetTasks.DotNetRun(s =>
               s.SetProjectFile(DCCMToolSrcProject)
               .EnableNoLaunchProfile()
               .SetConfiguration("Release")
               .SetApplicationArguments(
                   "internal", "upload-mapi",
                   "-i", WorkshopPublishRoot,
                   "-r", Path.Combine(RootDirectory, "latest-release.md")
                   ));
    });

}

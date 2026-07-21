
using DCCMTool.Commands;
using DCCMTool.Commands.Atlas;
using DCCMTool.Commands.Cdb;
using DCCMTool.Commands.Core;
using DCCMTool.Commands.Docs;
using DCCMTool.Commands.MSBuild;
using DCCMTool.Commands.Pak;
using DCCMTool.Commands.Steam;
using DCCMTool.Commands.Tmx;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using System.Reflection;

namespace DCCMTool
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var app = new CommandApp();

            app.Configure(config =>
            {
                config.PropagateExceptions();

                config.SetApplicationName("DCCMTool");
                config.SetApplicationVersion(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                    .InformationalVersion);

                config.AddBranch("internal", inter =>
                {
                    inter.AddCommand<GenerateTemplatePakCommand>("gen-pak-template");
                    inter.AddCommand<UploadMAPICommand>("upload-mapi");
                    inter.AddCommand<JsonMergeCommand>("merge-json");
                    inter.AddCommand<GenerateHaxeDBCommand>("generate-haxedb");
                    inter.AddCommand<ScanNativePrivateMemberCommand>("scan-native-private-member");
                    inter.AddCommand<InstallCommand>("install-mdk");
                    inter.AddCommand<GenerateSubAssemblyCommand>("generate-sub-assembly");
                });

                config.AddBranch("steam", steam =>
                {
                    steam.AddCommand<UploadModCommnand>("upload")
                        .WithDescription("Upload a mod to Steam Workshop.");

                    steam.AddCommand<SteamWorkshopMountCommand>("mount")
                        .WithDescription("Mount Steam Workshop mods into the local mods folder.");
                });

                config.AddBranch("cdb", cdb =>
                {
                    cdb.AddCommand<DiffCdbCommand>("diff")
                        .WithDescription("Compare the differences between two CDBs.");
                });

                config.AddBranch("atlas", atlas =>
                {
                    atlas.AddBranch("colorswap", colorswap =>
                    {
                        colorswap.AddCommand<DecodeColorswapCommand>("decode")
                            .WithDescription("Decode colorswap to a more human-readable format.");

                        colorswap.AddCommand<EncodeColorswapCommand>("encode")
                            .WithDescription("Encode images into a colorswap palette and images.");
                    });

                    atlas.AddCommand<UpackAtlasCommand>("unpack")
                        .WithDescription("Unpack an atlas file into its constituent images.");
                });

                config.AddBranch<PakCommandSettings>("pak", pak =>
                {
                    pak.AddCommand<MergePakCommand>("merge")
                        .WithDescription("Merge multiple PAK files into a single pak file.");

                    pak.AddCommand<UnpackPakCommand>("unpack")
                        .WithDescription("Extract the contents from the pak file");

                    pak.AddBranch("pack", pack =>
                    {
                        pack.AddCommand<PackDirToPakCommand>("dir")
                            .WithDescription("Pack the contents of the folder into a pak file.");

                        pack.AddCommand<PackFilesToPakCommand>("files")
                            .WithDescription("Pack files into a pak file.");
                    });

                });

                config.AddBranch("tmx", tmx =>
                {
                    tmx.AddCommand<CollapseTmxCommand>("collapse")
                        .WithDescription("Convert back tmx xml files to binary files.");
                    tmx.AddCommand<ExpandTmxCommand>("expand")
                        .WithDescription("Expand binary files to tmx xml files.");
                });

                config.AddCommand<HaxeDebugInfoCommand>("resolve-line-to-il")
                    .WithAlias("resolve-line")
                    .WithDescription("Converts line numbers in error messages to IL sequence numbers in pseudo-code");

                config.AddCommand<GenerateGamePersudoCommand>("generate-game-persudo")
                    .WithDescription("Generate the pseudo-code assembly for hlboot.dat");
            });

            try
            {
                return await app.RunAsync(args);
            }
            catch (CommandRuntimeException ex) when (ex.Pretty != null)
            {
                AnsiConsole.Write(ex.Pretty);
                return -1;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShowLinks);
                return -1;
            }
        }
    }
}

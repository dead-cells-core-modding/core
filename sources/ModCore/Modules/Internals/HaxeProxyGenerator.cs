using dc.en.mob.boss.giant;
using HashlinkNET.Bytecode;
using HashlinkNET.Compiler;
using HaxeProxy.Runtime.Internals;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.VM;
using ModCore.Storage;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules.Internals
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    internal class HaxeProxyGenerator : CoreModule<HaxeProxyGenerator>,
        IOnCodeLoading,
        IOnHashlinkVMReady
    {
        public readonly CacheFile proxyCache = new("GameProxy.dll");
        public readonly CacheFile pseudoCache = new("GamePseudocode.dll");
        public override int Priority => 1000;

        private Assembly? proxyAssembly;

        void IOnCodeLoading.OnCodeLoading( ref ReadOnlySpan<byte> data )
        {
            proxyCache.UpdateMetadata("code", data);
            proxyCache.UpdateMetadata("version", GetType().Assembly.GetName().Version?.ToString() ?? "None");

            pseudoCache.UpdateMetadata("code", data);
            pseudoCache.UpdateMetadata("version", GetType().Assembly.GetName().Version?.ToString() ?? "None");
            pseudoCache.UpdateMetadata("enabled", Core.Config.Value.GeneratePseudocodeAssembly.ToString());


            if (Core.Config.Value.GeneratePseudocodeAssembly &&
                !pseudoCache.IsValid)
            {
                Logger.Information("Generating Pseudocode Assembly");

                var asm = AssemblyDefinition.CreateAssembly(new("GamePseudocode", new()), "GamePseudocode", ModuleKind.Dll);
                var compiler = new HashlinkCompiler(
                    HlCode.FromBytes(data), asm, new()
                    {
                        AllowParalle = true,
                        GeneratePseudocode = true
                    });
                compiler.Compile();
                asm.Write(pseudoCache.CachePath);

                compiler = null;
                asm.MainModule.Types.Clear();
                asm.Dispose();
                asm = null;

                GC.Collect();

                pseudoCache.UpdateCache();
            }

            if (!proxyCache.IsValid)
            {
                Logger.Information("Generating Haxe Proxy Assembly");

                var asm = AssemblyDefinition.CreateAssembly(new("GameProxy", new()), "GameProxy", ModuleKind.Dll);
                var compiler = new HashlinkCompiler(
                    HlCode.FromBytes(data), asm, new()
                    {
                        AllowParalle = true
                    });
                compiler.Compile();
                asm.Write(proxyCache.CachePath);

                compiler = null;
                asm.MainModule.Types.Clear();
                asm.Dispose();
                asm = null;

                GC.Collect();

                proxyCache.UpdateCache();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            proxyAssembly = Assembly.LoadFrom(proxyCache.CachePath);

            Logger.Information("Loading Haxe Proxy Assembly from {path}", proxyAssembly.Location);

        }

        private Assembly? CurrentDomain_AssemblyResolve( object? sender, ResolveEventArgs args )
        {
            var asmName = new AssemblyName(args.Name);
            if (asmName.Name == "GameProxy")
            {
                return proxyAssembly;
            }
            return null;
        }

        void IOnHashlinkVMReady.OnHashlinkVMReady()
        {
            Debug.Assert(proxyAssembly != null);

            Logger.Information("Initializing Haxe Proxy");

            HaxeProxyManager.Initialize(proxyAssembly);

            Logger.Information("Loading advanced modules");

            Core.LoadCoreModules(typeof(Core).Assembly, CoreModuleAttribute.CoreModuleKind.Normal);

            EventSystem.BroadcastEvent<IOnAdvancedModuleInitializing>();
        }
    }
}

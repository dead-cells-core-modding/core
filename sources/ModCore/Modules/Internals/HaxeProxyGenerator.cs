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
        public override int Priority => 1000;

        private Assembly? proxyAssembly;

        void IOnCodeLoading.OnCodeLoading( ref ReadOnlySpan<byte> data )
        {
            proxyCache.UpdateMetadata("code", data);
            proxyCache.UpdateMetadata("version", GetType().Assembly.GetName().Version?.ToString() ?? "None");

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
                asm.Dispose();
                asm = null;

                GC.Collect();

                proxyCache.UpdateCache();
            }

            proxyAssembly = Assembly.LoadFrom(proxyCache.CachePath);

            Logger.Information("Loading Haxe Proxy Assembly from {path}", proxyAssembly.Location);

           

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

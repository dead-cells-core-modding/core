using HarmonyLib.Public.Patching;
using HaxeProxy.Runtime.Internals;
using ModCore.Events.Interfaces;
using ModCore.Hooks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ModCore.Modules.Internals
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    internal class HarmonyXModule : CoreModule<HarmonyXModule>,
        IOnCoreModuleInitializing
    {
        public override int Priority => -999999;
        void IOnCoreModuleInitializing.OnCoreModuleInitializing()
        {
            PatchManager.ResolvePatcher += PatchManager_ResolvePatcher;
        }

        private void PatchManager_ResolvePatcher( object? sender, PatchManager.PatcherResolverEventArgs e )
        {
            var fidxAttr = e.Original.GetCustomAttribute<HashlinkFIndexAttribute>();
            if (fidxAttr != null)
            {
                e.MethodPatcher = new HashlinkFunctionPatcher(e.Original, fidxAttr.Index);
            }
        }
    }
}

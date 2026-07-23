using HarmonyLib.Public.Patching;
using Hashlink.Marshaling;
using Hashlink.Reflection.Members;
using Hashlink.UnsafeUtilities;
using HaxeProxy.Runtime;
using ModCore.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Core;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ModCore.Hooks
{
    internal class HashlinkFunctionPatcher : MethodPatcher
    {
        private Type? originalType;
        private readonly HashlinkHookManager? hookManager;
        private Delegate? nextProcessor;
        private readonly int fidx;
        private MethodBase? processor;
        private ICoreDetour? detour;

        public HashlinkFunctionPatcher(MethodBase original, int fidx) : base(original)
        {
            this.fidx = fidx;

            hookManager = HashlinkHooks.Instance.GetManager(
                   (HashlinkFunction)HashlinkMarshal.Module.GetFunctionByFIndex(fidx)
                   );
        }

        public override MethodBase DetourTo( MethodBase replacement )
        {
            Debug.Assert(originalType != null);
            Debug.Assert(hookManager != null);

            if (processor == null)
            {
                processor = GenerateDMD().Generate();

                hookManager.AddProcessor(
                    processor.CreateDelegate(originalType).CreateAdaptDelegate()
                    );
            }

            var patchInfo = Original.GetPatchInfo();
            detour?.Dispose();
            detour = DetourFactory.Current.CreateDetour(processor, replacement, true);
            
            return replacement;
        }
        public override DynamicMethodDefinition? PrepareOriginal()
        {
            Debug.Assert(hookManager != null);
            return GenerateDMD();
        }

        public override DynamicMethodDefinition CopyOriginal()
        {
            Debug.Assert(hookManager != null);
            return GenerateDMD();
        }

        private DynamicMethodDefinition GenerateDMD()
        {
            Debug.Assert(hookManager != null);

            var dmd = new DynamicMethodDefinition(Original);


            if (originalType == null)
            {
                var fakeMI = dmd.Generate();
                originalType = fakeMI.CreateAnonymousDelegate(null).GetType();
            }

            if (nextProcessor == null)
            {
                nextProcessor = hookManager.GetProcessorChainLast().CreateAdaptDelegate(originalType);
            }


            var body = dmd.Definition.Body = new(dmd.Definition);
            var il = body.GetILProcessor();

            il.EmitNewReference(nextProcessor, out var cellRef);

            foreach (var v in dmd.Definition.Parameters)
            {
                il.Emit(OpCodes.Ldarg, v);
            }

            il.Emit(OpCodes.Callvirt, nextProcessor.GetType()
                    .GetMethod("Invoke")!);

            il.Emit(OpCodes.Ret);
            

            return dmd;
        }
    }
}

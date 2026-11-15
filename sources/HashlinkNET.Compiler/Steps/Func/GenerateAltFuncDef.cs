using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using HaxeProxy.Runtime;
using HaxeProxy.Runtime.Internals;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;

namespace HashlinkNET.Compiler.Steps.Func
{
    internal class GenerateAltFuncDef : ParallelCompileStep<HlFunction>
    {
        private TypeDefinition? altFuncHolder;
        private readonly HashSet<int> processedFunctions = [];

        // Creating shared wrapper types is easier and safer when serialized.
        protected override bool SupportParalle => false;

        private TypeDefinition GetAltFuncHolder( GlobalData gdata )
        {
            if (altFuncHolder != null)
            {
                return altFuncHolder;
            }
            altFuncHolder = new TypeDefinition("HashlinkNET.Proxy", "AltFunctions",
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
                gdata.Module.TypeSystem.Object);
            gdata.Module.Types.Add(altFuncHolder);
            return altFuncHolder;
        }

        private static bool IsRefParameter( TypeReference type )
        {
            return type is GenericInstanceType git &&
                git.ElementType.FullName == typeof(Ref<>).FullName;
        }

        private TypeReference GetAltType( IDataContainer container, RuntimeImports rdata, HlType arg )
        {
            if (arg is HlTypeWithType refType && arg.Kind == HlTypeKind.Ref)
            {
                return rdata.refType.MakeGenericInstanceType(
                    container.GetTypeRef(refType.Type.Value));
            }
            return container.GetTypeRef(arg)!;
        }

        protected override void Execute( IDataContainer container, HlFunction item, int index )
        {
            if (!processedFunctions.Add(item.FunctionIndex))
            {
                return;
            }

            var rdata = container.GetGlobalData<RuntimeImports>();
            var gdata = container.GetGlobalData<GlobalData>();
            var md = container.GetData<MethodDefinition>(item);
            var func = ((HlTypeWithFun)item.Type.Value).FunctionDescription;

            if (container.TryGetData<AltFuncData>(md, out _))
            {
                return;
            }

            if (container.GetAltTypeRef(item.Type.Value) == null)
            {
                return;
            }

            if (md.DeclaringType == null)
            {
                // Unbound functions are attached to a synthetic type by the
                // pseudocode steps. Without them (reference/proxy mode) the
                // definition has no module, so referencing it would produce
                // invalid metadata.
                if (gdata.Config.GeneratePseudocode)
                {
                    CreateStaticWrapper(container, gdata, rdata, md, func, item.FunctionIndex);
                }
                return;
            }

            var altParameterTypes = new TypeReference[md.Parameters.Count];
            var differs = false;

            for (int i = 0; i < md.Parameters.Count; i++)
            {
                var arg = func.Arguments[
                    func.Arguments.Length - md.Parameters.Count + i
                    ].Value;
                var pt = GetAltType(container, rdata, arg);
                altParameterTypes[i] = pt;
                differs |= !TypeEquals(pt, md.Parameters[i].ParameterType);
            }

            if (!differs)
            {
                return;
            }

            var alt = new MethodDefinition(md.Name, md.Attributes, md.ReturnType)
            {
                ExplicitThis = md.ExplicitThis,
                HasThis = md.HasThis,
                Attributes = md.Attributes,
                ImplAttributes = md.ImplAttributes,
            };
            alt.IsVirtual = false;

            foreach (var attr in md.CustomAttributes)
            {
                if (attr.AttributeType.FullName ==
                    typeof(HashlinkFIndexAttribute).FullName)
                {
                    continue;
                }
                alt.CustomAttributes.Add(attr);
            }
            alt.CustomAttributes.Add(new(rdata.attrAlt));

            for (int i = 0; i < md.Parameters.Count; i++)
            {
                var p = md.Parameters[i];
                alt.Parameters.Add(new(p.Name, p.Attributes, altParameterTypes[i]));
            }

            RunSync(() => md.DeclaringType.Methods.Add(alt));
            container.TryAddData(md, new AltFuncData(alt));

            EmitWrapperBody(alt, md, rdata);
        }

        private void CreateStaticWrapper( IDataContainer container, GlobalData gdata,
            RuntimeImports rdata, MethodDefinition md, HlTypeFun func, int functionIndex )
        {
            var holder = GetAltFuncHolder(gdata);
            var wrapper = new MethodDefinition("Func" + functionIndex,
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                md.ReturnType);

            for (var i = 0; i < func.Arguments.Length; i++)
            {
                wrapper.Parameters.Add(new("arg" + i, ParameterAttributes.None,
                    GetAltType(container, rdata, func.Arguments[i].Value)));
            }

            holder.Methods.Add(wrapper);
            container.TryAddData(md, new AltFuncData(wrapper));
            EmitWrapperBody(wrapper, md, rdata);
        }

        private static void EmitWrapperBody( MethodDefinition wrapper, MethodDefinition target,
            RuntimeImports rdata )
        {
            var body = wrapper.Body = new(wrapper);
            var il = body.GetILProcessor();

            if (target.HasThis && !wrapper.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }

            foreach (var v in wrapper.Parameters)
            {
                if (IsRefParameter(v.ParameterType))
                {
                    var element = ((GenericInstanceType)v.ParameterType).GenericArguments[0];
                    il.Emit(OpCodes.Ldarg, v);
                    il.Emit(OpCodes.Call, rdata.phUnref.MakeInstance(element));
                }
                else
                {
                    il.Emit(OpCodes.Ldarg, v);
                }
            }

            il.Emit(target.HasThis && !wrapper.IsStatic ? OpCodes.Callvirt : OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
        }

        private static bool TypeEquals( TypeReference a, TypeReference b )
        {
            return a.FullName == b.FullName;
        }

        protected override IReadOnlyList<HlFunction> GetItems( IDataContainer container )
        {
            return container.GetGlobalData<GlobalData>().Code.Functions;
        }
    }
}

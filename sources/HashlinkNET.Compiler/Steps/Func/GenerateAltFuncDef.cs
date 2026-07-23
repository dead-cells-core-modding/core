using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace HashlinkNET.Compiler.Steps.Func
{
    internal class GenerateAltFuncDef : ParallelCompileStep<HlFunction>
    {
        protected override void Execute( IDataContainer container, HlFunction item, int index )
        {
            var rdata = container.GetGlobalData<RuntimeImports>();
            var md = container.GetData<MethodDefinition>(item);

            if (md.DeclaringType == null)
            {
                return;
            }

            var func = ((HlTypeWithFun)item.Type.Value).FunctionDescription;
            var altType = container.GetAltTypeRef(item.Type.Value);

            if (altType == null)
            {
                return;
            }

            var alt = new MethodDefinition(md.Name, md.Attributes, md.ReturnType)
            {
                ExplicitThis = md.ExplicitThis,
                HasThis = md.HasThis,
                Attributes = md.Attributes,
                ImplAttributes = md.ImplAttributes,
                IsVirtual = true
            };
            alt.CustomAttributes.AddRange(md.CustomAttributes);
            alt.CustomAttributes.Add(new(rdata.attrAlt));

            var altRet = container.GetAltTypeRef(func.ReturnType.Value);

            if (altRet != null)
            {
                alt.ReturnType = altRet;
            }

            for (int i = 0; i < md.Parameters.Count; i++)
            {
                var arg = func.Arguments[
                    func.Arguments.Length - md.Parameters.Count + i
                    ].Value;
                var pt = container.GetAltTypeRef(arg);

                if (pt == null)
                {
                    if (arg.Kind == HlTypeKind.Ref)
                    {
                        pt = new ByReferenceType(container.GetTypeRef(
                            ((HlTypeWithType)arg).Type.Value
                            ));
                    }
                    else
                    {
                        pt = container.GetTypeRef(arg);
                    }
                }
                var p = md.Parameters[i];
                alt.Parameters.Add(new(p.Name, p.Attributes, pt));
            }

            alt.IsVirtual = false;
            RunSync(() => md.DeclaringType.Methods.Add(alt));

            var body = alt.Body = new(alt);
            var il = body.GetILProcessor();

            if (md.HasThis)
            {
                il.Emit(OpCodes.Ldarg_0);
            }

            foreach (var v in alt.Parameters)
            {
                il.Emit(OpCodes.Ldarg, v);

                if (v.ParameterType is ByReferenceType byRef)
                {
                    il.Emit(OpCodes.Call, rdata.hGetRef.MakeInstance(byRef.ElementType));
                }
            }

            il.Emit(OpCodes.Callvirt, md);
            il.Emit(OpCodes.Ret);
        }

        protected override IReadOnlyList<HlFunction> GetItems( IDataContainer container )
        {
            return container.GetGlobalData<GlobalData>().Code.Functions;
        }
    }
}

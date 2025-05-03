using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func
{
    internal class FixFuncTypeRefStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type is HlTypeWithFun;
        }
        public override void Execute( IDataContainer container, HlCode code,
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            var func = (HlTypeWithFun)type;
            var tr = container.GetTypeRef(type);

            var invokeMR = new MethodReference("Invoke", gdata.Module.TypeSystem.Void, tr)
            {
                HasThis = true
            };
            var ctor = new MethodReference(".ctor", gdata.Module.TypeSystem.Void, tr)
            {
                HasThis = true,
                Parameters =
                {
                    new(gdata.Module.TypeSystem.Object),
                    new(gdata.Module.TypeSystem.IntPtr)
                }
            };

            container.TryAddData(tr,
                container.AddData(type, new ClosureClassData()
                {
                    Construct = ctor,
                    Invoke = invokeMR,
                    TypeRef = tr,
                    TypeIndex = type.TypeIndex
                }));

            if (tr is not GenericInstanceType ft)
            {
                return;
            }

            var bt = ft.GetElementType();

            var desc = func.FunctionDescription;
            var ret = desc.ReturnType.Value;


            var gpId = 0;
            if (ret.Kind != HlTypeKind.Void)
            {
                var rt = container.GetTypeRef(ret);
                invokeMR.ReturnType = bt.GenericParameters[0];
                gpId++;
                ft.GenericArguments.Add(rt);
            }
            for (int i = 0; i < desc.Arguments.Length; i++)
            {
                var at = container.GetTypeRef(desc.Arguments[i].Value);
                invokeMR.Parameters.Add(new(bt.GenericParameters[gpId++]));
                ft.GenericArguments.Add(at);
            }
        }
    }
}

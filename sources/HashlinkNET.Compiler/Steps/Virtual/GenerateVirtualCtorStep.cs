using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Virtual
{
    class GenerateVirtualCtorStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Virtual;
        }
        public override void Execute( IDataContainer container, HlCode code,
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            var info = container.GetData<VirtualClassData>(type);
            if (info.TypeDef == null)
            {
                return;
            }

            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public |
                            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                            gdata.Module.TypeSystem.Void);
            info.TypeDef.Methods.Add(ctor);
            var il = ctor.Body.GetILProcessor();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, type.TypeIndex);
            il.Emit(OpCodes.Call, rdata.hCreateInstance);
            il.Emit(OpCodes.Call, rdata.objBaseCtorMethod);
            il.Emit(OpCodes.Ret);
        }
    }
}

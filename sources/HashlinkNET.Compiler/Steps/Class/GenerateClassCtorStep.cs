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

namespace HashlinkNET.Compiler.Steps.Class
{
    class GenerateClassCtorStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type is HlTypeWithObj obj;
        }
        public override void Execute( IDataContainer container, HlCode code,
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            var ot = (HlTypeWithObj)type;
            var info = container.GetData<ObjClassData>(type);

            var ctor = (MethodDefinition) info.Construct;
            var il = ctor.Body.GetILProcessor();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, type.TypeIndex);
            il.Emit(OpCodes.Call, rdata.hCreateInstance);
            il.Emit(OpCodes.Call, rdata.objBaseCtorMethod);


            if (info.GlobalClassType != info.TypeDef)
            {

                var realCtor = info.GlobalClassType?.FindMethod("__inst_construct__") ??
                                info.GlobalClassType?.FindMethod("__construct__");
                if (realCtor != null)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    for (int i = 1; i < realCtor.Parameters.Count; i++)
                    {
                        var cp = realCtor.Parameters[i];
                        var p = new ParameterDefinition(cp.Name, cp.Attributes, cp.ParameterType);
                        ctor.Parameters.Add(p);
                        il.Emit(OpCodes.Ldarg, p);
                    }
                    il.Emit(OpCodes.Call, realCtor);
                }
            }
            il.Emit(OpCodes.Ret);
        }
    }
}

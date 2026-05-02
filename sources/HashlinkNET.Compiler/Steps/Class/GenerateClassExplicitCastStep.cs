using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Steps.Class
{
    internal class GenerateClassExplicitCastStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Obj;
        }
        public override void Execute( IDataContainer container, HlCode code, GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            var info = container.GetData<ObjClassData>(type);
            var td = info.TypeDef;
            foreach (var v in info.CastExplicitTypes)
            {
                if (!v.Value)
                {
                    continue;
                }
                {
                    var to = new MethodDefinition(
                        $"op_Explicit",
                        MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                        rdata.objectType
                    )
                    {
                        Parameters =
                    {
                        new("value", ParameterAttributes.None, td)
                    },
                        ReturnType = v.Key
                    };
                    var il = to.Body.GetILProcessor();

                    il.Emit(OpCodes.Ldarg_0);

                    if (v.Value) //Is Virtual
                    {
                        il.Emit(OpCodes.Call, rdata.hToVirtual.MakeInstance(v.Key));
                    }
                    else
                    {
                        il.Emit(OpCodes.Call, rdata.hToObject.MakeInstance(v.Key));
                    }

                    il.Emit(OpCodes.Ret);


                    td.Methods.Add(to);
                }
                {
                    var from = new MethodDefinition(
                            $"op_Explicit",
                            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                            rdata.objectType
                        )
                    {
                        Parameters =
                    {
                        new("value", ParameterAttributes.None, v.Key)
                    },
                        ReturnType = td
                    };

                    var il = from.Body.GetILProcessor();

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, rdata.hToObject.MakeInstance(td));
                    il.Emit(OpCodes.Ret);

                    td.Methods.Add(from);
                }
            }
        }
    }
}

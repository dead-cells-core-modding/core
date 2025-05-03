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

namespace HashlinkNET.Compiler.Steps.Enum
{
    internal class GenerateEnumItemTypesStep : ForeachHlTypeCompileStep
    {
        public override void Execute( IDataContainer container, HlCode code, GlobalData gdata, 
            RuntimeImports rdata, HlType type )
        {
            var te = ((HlTypeWithEnum)type).Enum;
            var ei = container.GetData<EnumClassData>(type);
            var enumType = ei.TypeDef;

            var itemTypes = ei.ItemTypes = new TypeDefinition[te.Constructs.Length];
            var itemCtors = ei.ItemCtors = new MethodReference[te.Constructs.Length];
            for (int i = 0; i < te.Constructs.Length; i++)
            {
                var ec = te.Constructs[i];
                var td = new TypeDefinition("", ec.GetEnumItemName(), TypeAttributes.Class, enumType)
                {
                    Methods =
                    {
                        new("get_Index", MethodAttributes.HideBySig | MethodAttributes.SpecialName | 
                            MethodAttributes.Public | MethodAttributes.Virtual, ei.IndexType)
                        {
                            Body =
                            {
                                Instructions =
                                {
                                    Instruction.Create(OpCodes.Ldc_I4, i),
                                    Instruction.Create(OpCodes.Ret)
                                }
                            }
                        }
                    }
                };
                td.Properties.Add(new("Index", PropertyAttributes.None, ei.IndexType)
                {
                    GetMethod = td.Methods[0]
                });
                enumType.NestedTypes.Add( td );
                td.IsNestedPublic = true;
                itemTypes[i] = td;
                var ctor = new MethodDefinition(".ctor", MethodAttributes.SpecialName | MethodAttributes.RTSpecialName |
                    MethodAttributes.Public, gdata.Module.TypeSystem.Void)
                {
                    HasThis = true
                };
                td.Methods.Add(ctor);
                itemCtors[i] = ctor;
                var ilp = ctor.Body.GetILProcessor();
                for (int j = 0; j < ec.Params.Length; j++)
                {
                    var fd = new FieldDefinition("param" + j, FieldAttributes.Public,
                        container.GetTypeRef(ec.Params[j].Value));
                    td.Fields.Add(fd);

                    var mp = new ParameterDefinition("p" + j, ParameterAttributes.None, fd.FieldType);
                    ctor.Parameters.Add(mp);
                    ilp.Emit(OpCodes.Ldarg_0);
                    ilp.Emit(OpCodes.Ldarg, mp);
                    ilp.Emit(OpCodes.Stfld, fd);
                }
                ilp.Emit(OpCodes.Ret);
            }
            
        }

        public override bool Filter( HlType type )
        {
            return type is HlTypeWithEnum;
        }

    
    }
}

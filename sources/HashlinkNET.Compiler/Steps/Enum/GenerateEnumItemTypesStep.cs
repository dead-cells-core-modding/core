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
    internal class GenerateEnumItemTypesStep : GenerateTypeCompileStep
    {
        public override void Execute( IDataContainer container, HlCode code, GlobalData gdata, 
            RuntimeImports rdata, HlType type )
        {
            var te = ((HlTypeWithEnum)type).Enum;
            var ei = container.GetData<EnumClassData>(type);
            var isCtx = ei is ArrowFuncContextData;
            var enumType = ei.TypeDef;

            var itemTypes = ei.ItemTypes = new TypeDefinition[te.Constructs.Length];
            var itemCtors = ei.ItemCtors = new MethodReference[te.Constructs.Length];

            if (isCtx && te.Constructs.Length != 1)
            {
                throw new NotSupportedException();
            }
            for (int i = 0; i < te.Constructs.Length; i++)
            {
                var ec = te.Constructs[i];
                TypeDefinition td;
                if (isCtx)
                {
                    td = enumType;
                }
                else
                {
                    td = new TypeDefinition("", ec.GetEnumItemName(), TypeAttributes.Class, enumType)
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
                    addedTypes.Add(new(td, 
                        HaxeProxy.Runtime.Internals.HaxeProxyBindingAttribute.GetSubTypeId(type.TypeIndex, i))
                        );
                    enumType.NestedTypes.Add(td);
                    td.IsNestedPublic = true;
                }

                itemTypes[i] = td;
                var ctor = new MethodDefinition(".ctor", MethodAttributes.SpecialName | MethodAttributes.RTSpecialName |
                    MethodAttributes.Public, gdata.Module.TypeSystem.Void)
                {
                    HasThis = true
                };
                td.Methods.Add(ctor);
                itemCtors[i] = ctor;
                var ilp = ctor.Body.GetILProcessor();

                ilp.Emit(OpCodes.Ldarg_0);
                ilp.Emit(OpCodes.Ldc_I4, type.TypeIndex);
                ilp.Emit(OpCodes.Ldc_I4, i);
                ilp.Emit(OpCodes.Call, rdata.hCreateEnumInstance);
                ilp.Emit(OpCodes.Call, rdata.objBaseCtorMethod);

                for (int j = 0; j < ec.Params.Length; j++)
                {
                    var pd = new PropertyDefinition("Param" + j, PropertyAttributes.None,
                        container.GetTypeRef(ec.Params[j].Value));
                    pd.CheckDynamic(rdata, pd.PropertyType);
                    GeneralUtils.EmitFieldGetterSetter(
                        td, pd, container, (j | i << 16).ToString());
                    td.Properties.Add(pd);

                    var mp = new ParameterDefinition("p" + j, ParameterAttributes.None, pd.PropertyType);
                    
                    ctor.Parameters.Add(mp);
                    ilp.Emit(OpCodes.Ldarg_0);
                    ilp.Emit(OpCodes.Ldarg, mp);
                    ilp.Emit(OpCodes.Callvirt, pd.SetMethod);
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

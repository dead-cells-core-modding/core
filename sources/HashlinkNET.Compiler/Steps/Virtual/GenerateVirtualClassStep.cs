using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Virtual
{
    class GenerateVirtualClassStep : CompileStep
    {

        public override void Execute( IDataContainer container )
        {
            var virtList = container.GetGlobalData<VirtualTypeList>();
            var rdata = container.GetGlobalData<RuntimeImports>();
            var gdata = container.GetGlobalData<GlobalData>();

            foreach ((_, var group) in virtList.Virtuals)
            {
                var td = group.TypeDef;

                //Ctor
                {
                    var ctor = new MethodDefinition(".ctor", MethodAttributes.Public |
                               MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                               gdata.Module.TypeSystem.Void);
                    td.Methods.Add(ctor);
                    var il = ctor.Body.GetILProcessor();

                    il.Emit(OpCodes.Ldarg_0);
                    if (group.Types.Count == 1)
                    {
                        il.Emit(OpCodes.Ldc_I4, group.Types[0].TypeIndex);
                    }
                    else
                    {
                        var cf = new FieldDefinition("cachedTypeIndex",
                            FieldAttributes.Static | FieldAttributes.Private,
                            gdata.Module.TypeSystem.Int32);
                        td.Fields.Add(cf);
                        il.Emit(OpCodes.Ldsflda, new FieldReference(cf.Name, cf.FieldType, 
                            td.MakeGenericInstanceType([.. td.GenericParameters])));
                        il.Emit(OpCodes.Call, rdata.hGetTypeIndexFromType.MakeInstance(
                            td.MakeGenericInstanceType([.. td.GenericParameters]
                            )));
                    }
                    il.Emit(OpCodes.Call, rdata.hCreateInstance);
                    il.Emit(OpCodes.Call, rdata.objBaseCtorMethod);
                    il.Emit(OpCodes.Ret);
                }

                //Fields
                {

                    foreach (var f in group.SortedFieldNames)
                    {
                        TypeReference ftype;
                        if (group.DifferentTypeFields.Contains(f))
                        {
                            ftype = td.GenericParameters.First(x => x.Name == f);
                        }
                        else
                        {
                            ftype = container.GetTypeRef(
                                group.Types[0].Virtual.Fields.First(x => x.Name == f).Type.Value
                                );
                        }
                        var fd = new PropertyDefinition(f, PropertyAttributes.None, ftype);
                       
                        td.EmitFieldGetterSetter(fd, container, f);


                        td.Properties.Add(fd);
                    }

                    foreach (var v in group.Types)
                    {
                        var virtInfo = container.GetData<VirtualClassData>(v);
                        var fields = virtInfo.Fields;
                        foreach (var f in v.Virtual.Fields)
                        {
                            var parent = td.Properties.First(x => x.Name == f.Name);
                            if (group.Types.Count == 1)
                            {
                                fields.Add(parent);
                                continue;
                            }

                            var pd = new PropertyDefinition(f.Name, parent.Attributes, parent.PropertyType)
                            {
                                GetMethod = Unsafe.As<MethodDefinition>(parent.GetMethod.CreateGenericInstanceTypeMethod(virtInfo.TypeRef)),
                                SetMethod = Unsafe.As<MethodDefinition>(parent.SetMethod.CreateGenericInstanceTypeMethod(virtInfo.TypeRef))
                            };
                            if (pd.PropertyType is GenericParameter gp)
                            {
                                pd.PropertyType = ((GenericInstanceType)virtInfo.TypeRef).GenericArguments[gp.Position];
                            }
                            pd.CopyAttributeFrom(parent);
                            fields.Add(pd);
                            container.AddDataEach(f, pd);
                        }
                    }
                }
            }
        }
    }
}

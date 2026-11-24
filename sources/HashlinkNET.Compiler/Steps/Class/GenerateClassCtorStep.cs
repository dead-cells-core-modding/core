using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace HashlinkNET.Compiler.Steps.Class
{
    class GenerateClassCtorStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type is HlTypeWithObj;
        }
        public override void Execute( IDataContainer container, HlCode code,
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            var ot = (HlTypeWithObj)type;
            var info = container.GetData<ObjClassData>(type);
            var td = info.TypeDef;

            var ctor = (MethodDefinition)info.Construct;
            var il = ctor.Body.GetILProcessor();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, type.TypeIndex);
            il.Emit(OpCodes.Call, rdata.hCreateInstance);
            il.Emit(OpCodes.Call, rdata.objBaseCtorMethod);

            while (true)
            {
                var tglobal = info.GlobalHlType;
                if (tglobal == null)
                {
                    break;
                }
                var tglobal_info = container.GetData<ObjClassData>(tglobal)!;
                if (tglobal_info.InstanceCtor == null)
                {
                    break;
                }
                var realCtor = container.GetData<MethodDefinition>(tglobal_info.InstanceCtor);
                if (realCtor == null || realCtor.Parameters.Count != 1)
                {
                    break;
                }
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, realCtor);
                break;
            }

            il.Emit(OpCodes.Ret);

            // Additional constructor overload mirroring __inst_construct__,
            // so that 'New' + constructor call sites decompile to
            // 'new SomeType(args)'. The type graph is shared between worker
            // threads, so this is deferred to the synchronized phase.
            RunSync(() =>
            {
                var tglobal = info.GlobalHlType;
                if (tglobal == null)
                {
                    return;
                }
                var tglobal_info = container.GetData<ObjClassData>(tglobal)!;
                if (tglobal_info.InstanceCtor == null)
                {
                    return;
                }
                var realCtor = container.GetData<MethodDefinition>(tglobal_info.InstanceCtor);
                if (realCtor == null || realCtor.Parameters.Count <= 1)
                {
                    return;
                }

                if (td.Methods.Any(m => m != ctor &&
                    m.Name == ".ctor" &&
                    !m.IsStatic &&
                    m.Parameters.Count == realCtor.Parameters.Count - 1))
                {
                    return;
                }

                var ctorArgs = new MethodDefinition(".ctor", ctor.Attributes,
                    gdata.Module.TypeSystem.Void)
                {
                    HasThis = true
                };
                ctorArgs.Body = new(ctorArgs);
                td.Methods.Add(ctorArgs);
                info.ConstructArgs = ctorArgs;

                var il2 = ctorArgs.Body.GetILProcessor();
                il2.Emit(OpCodes.Ldarg_0);
                il2.Emit(OpCodes.Ldc_I4, type.TypeIndex);
                il2.Emit(OpCodes.Call, rdata.hCreateInstance);
                il2.Emit(OpCodes.Call, rdata.objBaseCtorMethod);

                il2.Emit(OpCodes.Ldarg_0);
                for (int i = 1; i < realCtor.Parameters.Count; i++)
                {
                    var cp = realCtor.Parameters[i];
                    var p = new ParameterDefinition(cp.Name, cp.Attributes, cp.ParameterType);
                    ctorArgs.Parameters.Add(p);
                    il2.Emit(OpCodes.Ldarg, p);
                }
                il2.Emit(OpCodes.Call, realCtor);
                if (realCtor.ReturnType.FullName != "System.Void")
                {
                    il2.Emit(OpCodes.Pop);
                }
                il2.Emit(OpCodes.Ret);

                // Alt constructor overload using Ref<T> parameters (the main
                // constructor uses ref T).
                var altParameterTypes = new TypeReference[ctorArgs.Parameters.Count];
                var hasRefParameter = false;
                for (var i = 0; i < ctorArgs.Parameters.Count; i++)
                {
                    var pt = ctorArgs.Parameters[i].ParameterType;
                    if (pt is ByReferenceType byRef)
                    {
                        altParameterTypes[i] = rdata.refType.MakeGenericInstanceType(
                            byRef.ElementType);
                        hasRefParameter = true;
                    }
                    else
                    {
                        altParameterTypes[i] = pt;
                    }
                }

                if (!hasRefParameter)
                {
                    return;
                }

                var altCtor = new MethodDefinition(".ctor", ctorArgs.Attributes,
                    gdata.Module.TypeSystem.Void)
                {
                    HasThis = true,
                    ImplAttributes = ctorArgs.ImplAttributes
                };
                altCtor.Body = new(altCtor);
                altCtor.CustomAttributes.Add(new(rdata.attrAlt));

                for (var i = 0; i < ctorArgs.Parameters.Count; i++)
                {
                    var p = ctorArgs.Parameters[i];
                    altCtor.Parameters.Add(new(p.Name, p.Attributes, altParameterTypes[i]));
                }
                td.Methods.Add(altCtor);

                var il3 = altCtor.Body.GetILProcessor();
                il3.Emit(OpCodes.Ldarg_0);
                foreach (var p in altCtor.Parameters)
                {
                    if (p.ParameterType is GenericInstanceType git &&
                        git.ElementType.FullName == rdata.refType.FullName)
                    {
                        il3.Emit(OpCodes.Ldarg, p);
                        il3.Emit(OpCodes.Call, rdata.phUnref.MakeInstance(git.GenericArguments[0]));
                    }
                    else
                    {
                        il3.Emit(OpCodes.Ldarg, p);
                    }
                }
                il3.Emit(OpCodes.Call, ctorArgs);
                il3.Emit(OpCodes.Ret);
            });
        }
    }
}

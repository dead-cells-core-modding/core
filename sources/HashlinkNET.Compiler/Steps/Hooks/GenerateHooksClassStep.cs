using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.CompilerServices.SymbolWriter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HashlinkNET.Compiler.Steps.Hooks
{
    internal class GenerateHooksClassStep : GenerateTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type is HlTypeWithObj obj && 
                (obj.Obj.Protos.Length > 0 || obj.Obj.Bindings.Length > 0);
        }
        public override void Execute( IDataContainer container, HlCode code, 
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            var obj = ((HlTypeWithObj)type).Obj;
            var info = container.GetData<ObjClassData>(type);
            var typeDef = info.TypeDef;
            var htd = new TypeDefinition(typeDef.Namespace, "Hook_" + typeDef.Name,
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed)
            {
                BaseType = rdata.objectType
            };
            addedTypes.Add(new(htd, AddTypeKind.AddToModule));

            var methods = new List<(string, int, MethodDefinition)>();

            foreach (var v in obj.Protos)
            {
                var pd = container.GetData<MethodDefinition>(v);
                methods.Add((v.Name, v.FIndex, pd));
            }

            foreach (var v in obj.Bindings)
            {
                methods.Add((
                   info.GetField(v.FieldIndex)!.Name, v.FunctionIndex,
                    container.GetData<MethodDefinition>(code.GetFunctionById(v.FunctionIndex)!)
                    ));
            }

            foreach ((var name, int fidx, var pd) in methods)
            {
                

                var delOrig = HookGenerator.GenerateDelegateFor(pd, gdata.Module.TypeSystem, rdata);
                delOrig.Name = "orig_" + name;
                htd.NestedTypes.Add(delOrig);

                var delHook = HookGenerator.GenerateDelegateFor(pd, gdata.Module.TypeSystem, rdata);
                delHook.Name = "hook_" + name;
                var delHookInvoke = delHook.FindMethod("Invoke")!;
                delHookInvoke.Parameters.Insert(0, new ParameterDefinition("orig", ParameterAttributes.None, delOrig));
                var delHookBeginInvoke = delHook.FindMethod("BeginInvoke")!;
                delHookBeginInvoke.Parameters.Insert(0, new ParameterDefinition("orig", ParameterAttributes.None, delOrig));
                htd.NestedTypes.Add(delHook);

                var addHook = new MethodDefinition(
                    "add_" + name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                    gdata.Module.TypeSystem.Void
                );
                addHook.Parameters.Add(new ParameterDefinition(null, ParameterAttributes.None, delHook));
                addHook.Body = new MethodBody(addHook);
                var il = addHook.Body.GetILProcessor();
                il.Emit(OpCodes.Ldc_I4, fidx);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, rdata.hAddHook);
                il.Emit(OpCodes.Ret);
                htd.Methods.Add(addHook);

                var removeHook = new MethodDefinition(
                    "remove_" + name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                    gdata.Module.TypeSystem.Void
                );
                removeHook.Parameters.Add(new ParameterDefinition(null, ParameterAttributes.None, delHook));
                removeHook.Body = new MethodBody(removeHook);
                il = removeHook.Body.GetILProcessor();
                il.Emit(OpCodes.Ldc_I4, fidx);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, rdata.hRemoveHook);
                il.Emit(OpCodes.Ret);
                htd.Methods.Add(removeHook);

                var ev = new EventDefinition(name, EventAttributes.None, delHook)
                {
                    AddMethod = addHook,
                    RemoveMethod = removeHook
                };
                htd.Events.Add(ev);
            }

        }
    }
}

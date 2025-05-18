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
                obj.Obj.Protos.Length > 0 &&
                GeneralUtils.ParseHlTypeName(obj.Name, out _, out var name) &&
                !name.StartsWith('$');
        }
        public override void Execute( IDataContainer container, HlCode code, 
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            var obj = ((HlTypeWithObj)type).Obj;
            var typeDef = container.GetData<ITypeDefinitionValue>(type).TypeDef;
            var htd = new TypeDefinition(typeDef.Namespace, "Hook_" + typeDef.Name,
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed)
            {
                BaseType = rdata.objectType
            };
            addedTypes.Add(new(htd, -1));

            foreach (var v in obj.Protos)
            {
                var pd = container.GetData<MethodDefinition>(v);

                var delOrig = HookGenerator.GenerateDelegateFor(pd, gdata.Module.TypeSystem, rdata);
                delOrig.Name = "orig_" + v.Name;
                htd.NestedTypes.Add(delOrig);

                var delHook = HookGenerator.GenerateDelegateFor(pd, gdata.Module.TypeSystem, rdata);
                delHook.Name = "hook_" + v.Name;
                var delHookInvoke = delHook.FindMethod("Invoke")!;
                delHookInvoke.Parameters.Insert(0, new ParameterDefinition("orig", ParameterAttributes.None, delOrig));
                var delHookBeginInvoke = delHook.FindMethod("BeginInvoke")!;
                delHookBeginInvoke.Parameters.Insert(0, new ParameterDefinition("orig", ParameterAttributes.None, delOrig));
                htd.NestedTypes.Add(delHook);

                var addHook = new MethodDefinition(
                    "add_" + v.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                    gdata.Module.TypeSystem.Void
                );
                addHook.Parameters.Add(new ParameterDefinition(null, ParameterAttributes.None, delHook));
                addHook.Body = new MethodBody(addHook);
                var il = addHook.Body.GetILProcessor();
                il.Emit(OpCodes.Ldc_I4, v.FIndex);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, rdata.hAddHook);
                il.Emit(OpCodes.Ret);
                htd.Methods.Add(addHook);

                var removeHook = new MethodDefinition(
                    "remove_" + v.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                    gdata.Module.TypeSystem.Void
                );
                removeHook.Parameters.Add(new ParameterDefinition(null, ParameterAttributes.None, delHook));
                removeHook.Body = new MethodBody(removeHook);
                il = removeHook.Body.GetILProcessor();
                il.Emit(OpCodes.Ldc_I4, v.FIndex);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, rdata.hRemoveHook);
                il.Emit(OpCodes.Ret);
                htd.Methods.Add(removeHook);

                var ev = new EventDefinition(v.Name, EventAttributes.None, delHook)
                {
                    AddMethod = addHook,
                    RemoveMethod = removeHook
                };
                htd.Events.Add(ev);
            }

        }
    }
}

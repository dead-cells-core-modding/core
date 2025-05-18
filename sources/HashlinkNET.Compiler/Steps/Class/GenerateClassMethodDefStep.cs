using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
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
    internal class GenerateClassMethodDefStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Obj || type.Kind == HlTypeKind.Struct;
        }
        public override void Execute( IDataContainer container, HlCode code, GlobalData gdata,
            RuntimeImports rdata, HlType type )
        {

            var ot = (HlTypeWithObj)type;
            var info = container.GetData<ObjClassData>(type);
            var obj = ot.Obj;
            var td = info.TypeDef;
            var protos = info.Protos;

            void EmitFunc(HlFunction f)
            {
                var fd = ((HlTypeWithFun)f.Type.Value).FunctionDescription;
                var md = container.GetData<MethodDefinition>(f);

                md.FixPIndex();
                
                md.Body.Instructions.Clear();
                var ilp = md.Body.GetILProcessor();

                td.EmitCallHlFunc(ilp, container, f, ( il, i ) =>
                {
                    var at = fd.Arguments[i];
                    if (md.IsStatic)
                    {
                        il.Emit(OpCodes.Ldarg, md.Parameters[i]);
                    }
                    else if (i > 0)
                    {
                        il.Emit(OpCodes.Ldarg, md.Parameters[i - 1]);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg_0);
                    }
                    
                });

                ilp.Emit(OpCodes.Ret);
            }

            if (info.InstanceCtor != null)
            {
                var fd = ((HlTypeWithFun)info.InstanceCtor.Type.Value).FunctionDescription;
                var md = container.GetData<MethodDefinition>(info.InstanceCtor);

                md.Name = "__inst_construct__";
                md.IsAssembly = true;
                td.Methods.Add(md);

                EmitFunc(info.InstanceCtor);
            }

            foreach (var p in obj.Protos)
            {
                var f = code.Functions[code.FunctionIndexes[p.FIndex]];
                var fd = ((HlTypeWithFun)f.Type.Value).FunctionDescription;
                var md = container.GetData<MethodDefinition>(f);
                if (p.PIndex >= 0)
                {
                    md.IsVirtual = true;
                    protos[p.PIndex] = md;
                }
                md.IsStatic = false;
                md.HasThis = true;
                md.Parameters.RemoveAt(0); //Remove 'this'
                md.Name = p.Name;
                container.AddData(p, md);
                td.Methods.Add(md);

                //Emit Body

                EmitFunc(f);
            }

        }
    }
}

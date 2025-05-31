using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func.ArrowFunc
{
    internal class GenerateArrowFuncContextStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Enum;
        }
        public override void Execute( IDataContainer container, HlCode code, 
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            if (!container.TryGetData<ArrowFuncContextData>(type, out var data))
            {
                return;
            }
            var td = data.TypeDef;
            td.BaseType = rdata.arrowFuncCtxType;

            foreach (var method in data.Methods)
            {
                var f = container.GetData<HlFunction>(method);
                var fd = ((HlTypeWithFun)f.Type.Value).FunctionDescription;
                var md = method.Definition;
                var usedby = method.UsedBy[0];

                md.Name = "ArrowFunctionEntry_" + f.FunctionIndex;
                md.HasThis = true;
                md.IsStatic = false;
                md.IsPublic = true;
                md.Parameters.RemoveAt(0);

                {
                    md.FixPIndex();

                    md.Body.Instructions.Clear();
                    var ilp = md.Body.GetILProcessor();

                    data.TypeDef.EmitCallHlFunc(ilp, container, f, ( il, i ) =>
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
            }
            
            TypeDefinition? parentDef = null;
            foreach (var v in data.Methods)
            {
                var parent = v.UsedBy[0].Item1;
                if (parent.DeclaringClass != null)
                {
                    parentDef = parent.DeclaringClass.TypeDef;
                    data.DirectParent = parent;
                    break;
                }
            }

            if (parentDef == null)
            {
                foreach (var v in data.Methods)
                {
                    var parent = v.UsedBy[0].Item1;
                    var pmd = parent.Definition;
                    if (pmd.DeclaringType == null ||
                        td == pmd.DeclaringType)
                    {
                        continue;
                    }
                    else
                    {
                        parentDef = pmd.DeclaringType;
                        data.DirectParent = parent;
                        break;
                    }
                }
            }

            if (parentDef == null)
            {
                return;
            }
           
            Debug.Assert(td != parentDef);

            RunSync(() =>
            {
                gdata.Module.Types.Remove(td);
                parentDef.NestedTypes.Add(td);
            });
            td.IsNestedPublic = true;
            td.Namespace = "";
            td.IsAbstract = false;
        }

    }
}

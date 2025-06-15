using HashlinkNET.Compiler.Pseudocode.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_SetLocalReg(
        HlFuncRegisterData? dst,
        IRResult src,
        string? assign
        ) : IRBase(src)
    {
        public HlFuncRegisterData? dst = dst;
        public readonly IRResult src = src;
        public readonly string? assign = assign;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, dst != null);

            if (dst == null)
            {
                return null;
            }
            else if (dst.Kind == HlFuncRegisterData.RegisterKind.Parameter)
            {
                il.Emit(OpCodes.Starg, dst.Parameter);
            }
            else
            {
                Debug.Assert(dst.Variable != null);
                il.Emit(OpCodes.Stloc, dst.Variable);

                if (assign != null)
                {
                    var scope = ctx.VariableDebugs[dst.Index];
                    var last = il.Body.Instructions[^1];
                    if (scope != null)
                    {
                        scope.End = new(last);
                    }
                   
                    scope = new(last, last)
                    {
                        End = ctx.Scope.End,
                    };
                    ctx.VariableDebugs[dst.Index] = scope;
                    scope.Variables.Add(new VariableDebugInformation(dst.Variable, assign));
                    ctx.Scope.Scopes.Add(scope);
                }
                

            }
            return null;
        }
    }
}

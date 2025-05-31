using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Call
{
    class IR_CallClosure(
        MethodReference? invokeMethod,
        IRResult src,
        params IRResult[] args
        ) : IRBase([src, ..args])
    {
        public MethodReference? invokeMethod = invokeMethod;
        public readonly IRResult src = src;
        public readonly IRResult[] args = args;
        public override bool HasSideEffects => true;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            if (invokeMethod != null)
            {
                foreach (var v in args)
                {
                    v.Emit(ctx, true);
                }
                il.Emit(OpCodes.Callvirt, invokeMethod);
                return invokeMethod.ReturnType;
            }
            else
            {
                var im = container.GetData<RuntimeImports>(container.Parent!).delegateDynInvokeMethod;
                
                il.Emit(OpCodes.Ldc_I4, args.Length);
                il.Emit(OpCodes.Newarr, ctx.TypeSystem.Object);

                for (var i = 0; i < args.Length; i++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, i);
                    var argType = args[i].Emit(ctx, true);
                    il.Emit(OpCodes.Stelem_Any, argType);
                }
                il.Emit(OpCodes.Callvirt, im);
                return ctx.TypeSystem.Object;
            }
        }
    }
}

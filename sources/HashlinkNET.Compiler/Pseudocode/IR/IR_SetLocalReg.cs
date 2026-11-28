using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_SetLocalReg(
        HlFuncRegisterData? dst,
        IRResult src
        ) : IRBaseWithSideEffects(src)
    {
        public HlFuncRegisterData? dst = dst;
        public readonly IRResult src = src;
        private static readonly bool TraceTypes =
            Environment.GetEnvironmentVariable("DCCM_TRACE_TYPE_MISMATCH") == "1";
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var srcType = src.Emit(ctx, dst != null)!;

            if (dst == null)
            {
                return null;
            }

            if (!srcType.IsSameType(dst.RegisterType))
            {
                if (TraceTypes &&
                    (dst.RegisterType.FullName == "System.Boolean" ||
                     srcType.FullName is "System.Double" or "System.Single"))
                {
                    Console.Error.WriteLine(
                        $"[setreg] fn={ctx.GlobalData.Function.FunctionIndex} " +
                        $"src={srcType.FullName} srcIR={src.IR?.GetType().Name} " +
                        $"dst={dst.RegisterType.FullName} dstIdx={dst.Index} kind={dst.Kind}");
                }
                new IR_Cast(new IR_LoadFromStack(srcType), dst.RegisterType, false).Emit(ctx, true);
            }

            if (dst.Kind == HlFuncRegisterData.RegisterKind.Parameter)
            {
                il.Emit(OpCodes.Starg, dst.Parameter);
            }
            else
            {
                Debug.Assert(dst.Variable != null);
                il.Emit(OpCodes.Stloc, dst.Variable);
            }
            return null;
        }
    }
}




using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace HashlinkNET.Compiler.Pseudocode.IR.Call
{
    class IR_Call(
        MethodReference method,
        bool virt,
        params IRResult[] args
        ) : IRBaseWithSideEffects(args)
    {
        public MethodReference method = method;
        public readonly IRResult[] args = args;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {

            for (var i = 0; i < args.Length; i++)
            {
                var actual = args[i].Emit(ctx, true);

                Debug.Assert(actual != null);

                TypeReference? expected = null;
                if (method.HasThis && !method.ExplicitThis)
                {
                    if (i == 0)
                    {
                        expected = method.DeclaringType;
                    }
                    else if (i - 1 < method.Parameters.Count)
                    {
                        expected = method.Parameters[i - 1].ParameterType;
                    }
                }
                else if (i < method.Parameters.Count)
                {
                    expected = method.Parameters[i].ParameterType;
                }

                Debug.Assert(expected != null);

                if (!actual.IsSameType(expected))
                {
                    new IR_Cast(new IR_LoadFromStack(actual), expected, false).Emit(ctx, true);
                }
            }

            il.Emit(virt && method.HasThis ? OpCodes.Callvirt : OpCodes.Call, method);
            return method.ReturnType;
        }
    }
}


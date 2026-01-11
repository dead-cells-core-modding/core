using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Call
{
    class IR_Call(
        MethodReference method,
        bool virt,
        params IRResult[] args
        ) : IRBase(args)
    {
        public MethodReference method = method;
        public readonly IRResult[] args = args;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {

            foreach (var v in args)
            {
                v.Emit(ctx, true);
            }

            il.Emit(virt && method.HasThis ? OpCodes.Callvirt : OpCodes.Call, method);
            return method.ReturnType;
        }
    }
}

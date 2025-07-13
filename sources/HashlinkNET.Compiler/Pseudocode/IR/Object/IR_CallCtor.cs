using HashlinkNET.Compiler.Data.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Object
{
    class IR_CallCtor(
        TypeReference type,
        IRResult[] args
        )
        : IRBase(args)
    {
        public TypeReference type = type;
        public readonly IRResult[] args = args;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            foreach (var v in args)
            {
                v.Emit(ctx, true);
            }
            il.Emit(OpCodes.Newobj, container.GetData<IConstructable>(type).Construct);
            return type;
        }
    }
}

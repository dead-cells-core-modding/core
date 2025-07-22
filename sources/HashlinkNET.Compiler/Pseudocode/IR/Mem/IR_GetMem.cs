using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR.Mem
{
    internal class IR_GetMem(
        IRResult ptr,
        IRResult index,
        TypeReference type
        ) : IRBase(ptr, index)
    {
        public readonly IRResult ptr = ptr;
        public readonly IRResult index = index;
        public readonly TypeReference type = type;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, 
            ILProcessor il )
        {
            ptr.Emit( ctx, true );

            index.Emit( ctx, true );
            
            il.Emit(OpCodes.Call, ctx.RuntimeImports.phReadMem.MakeInstance(type) );
            return type;
        }
    }
}

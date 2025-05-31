using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_Cast(
        IRResult src,
        TypeReference type) : IRBase(src)
    {
        public readonly IRResult src = src;
        public TypeReference type = type;

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var st = src.Emit(ctx, true)!;
            if (!type.IsValueType)
            {
                if (st.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, type);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, type);
                }
            }
            else
            {
                if (type.Namespace == "System")
                {
                    if (type.Name == typeof(int).Name)
                    {
                        il.Emit(OpCodes.Conv_I4);
                    }
                    else if (type.Name == typeof(long).Name)
                    {
                        il.Emit(OpCodes.Conv_I8);
                    }
                    else if (type.Name == typeof(float).Name)
                    {
                        il.Emit(OpCodes.Conv_R4);
                    }
                    else if (type.Name == typeof(double).Name)
                    {
                        il.Emit(OpCodes.Conv_R8);
                    }
                }
                else if (type.IsByReference)
                {
                    il.Emit(OpCodes.Conv_U);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            return type;
        }
    }
}

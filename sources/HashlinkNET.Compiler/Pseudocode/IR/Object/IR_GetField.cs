using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Object
{
    class IR_GetField(
        IRResult obj,
        PropertyDefinition field
        ) : IRBase(obj)
    {
        public readonly IRResult obj = obj;
        public PropertyDefinition field = field;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (obj.IsNone)
            {
                il.Emit(OpCodes.Call, field.GetMethod);
            }
            else
            {
                obj.Emit(ctx, true);
                MethodReference getMethod = field.GetMethod;
                TypeReference? declaring = null;
                try
                {
                    declaring = getMethod?.DeclaringType;
                }
                catch (InvalidCastException)
                {
                }
                if (declaring != null &&
                    !declaring.IsValueType &&
                    !declaring.IsByReference &&
                    declaring.FullName != "System.Object")
                {
                    il.Emit(OpCodes.Castclass, declaring);
                }
                il.Emit(OpCodes.Call, getMethod);
            }
            return field.PropertyType;
        }
    }
}

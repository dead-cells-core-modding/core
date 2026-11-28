using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_LoadNull( TypeReference? targetType = null ) : IR_LoadConst(null, targetType)
    {
    }
    class IR_LoadConst( object? val, TypeReference? targetType = null ) : IRBase
    {
        public object? value = val;
        public TypeReference? targetType = targetType;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            if (value == null)
            {
                if (targetType != null &&
                    (targetType.FullName == "System.IntPtr" ||
                     targetType.FullName == "System.UIntPtr" ||
                     targetType.IsPointer))
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Conv_U);
                    return targetType;
                }
                il.Emit(OpCodes.Ldnull);
                if (targetType != null)
                {
                    if (targetType.IsValueType)
                    {
                        // Nullable<T>: null is represented by unboxing a null
                        // reference into the nullable value type.
                        if (targetType.IsNullable(out _))
                        {
                            il.Emit(OpCodes.Unbox_Any, targetType);
                            return targetType;
                        }
                    }
                    else
                    {
                        // Preserve the declared reference type so downstream
                        // emitters do not insert a redundant cast.
                        return targetType;
                    }
                }
                return ctx.TypeSystem.Object;
            }
            else if (value is int @int)
            {
                il.Emit(OpCodes.Ldc_I4, @int);
                return ctx.TypeSystem.Int32;
            }
            else if (value is long @long)
            {
                il.Emit(OpCodes.Ldc_I8, @long);
                return ctx.TypeSystem.Int64;
            }
            else if (value is float @float)
            {
                il.Emit(OpCodes.Ldc_R4, @float);
                return ctx.TypeSystem.Single;
            }
            else if (value is double @double)
            {
                il.Emit(OpCodes.Ldc_R8, @double);
                return ctx.TypeSystem.Double;
            }
            else if (value is string @string)
            {
                il.Emit(OpCodes.Ldstr, @string);
                if (targetType != null && targetType.FullName != "System.String")
                {
                    if (targetType.FullName == "System.IntPtr" ||
                        targetType.FullName == "System.UIntPtr")
                    {
                        il.Emit(OpCodes.Call, ctx.RuntimeImports.phAsHaxeStringPtr);
                        return ctx.TypeSystem.IntPtr;
                    }
                    // System.String must be converted into the Haxe string
                    // proxy (dc.String) through the shared helper.
                    il.Emit(OpCodes.Call,
                        ctx.RuntimeImports.phAsHaxeString.MakeInstance(targetType));
                    return targetType;
                }
                return ctx.TypeSystem.String;
            }
            else if (value is bool @bool)
            {
                il.Emit(OpCodes.Ldc_I4, @bool ? 1 : 0);
                return ctx.TypeSystem.Boolean;
            }
            return null;
        }
    }
}

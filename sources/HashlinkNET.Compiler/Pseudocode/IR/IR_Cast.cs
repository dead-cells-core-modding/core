using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR
{
    class IR_Cast(
        IRResult src,
        TypeReference type,
        bool safe = false ) : IRBase(src)
    {
        public readonly IRResult src = src;
        public TypeReference type = type;
        public bool safe = safe;

        private static void EmitNumericConvert( ILProcessor il, TypeReference from, TypeReference to )
        {
            if (from.FullName == to.FullName)
            {
                return;
            }
            switch (to.Name)
            {
                case nameof(Byte):
                    il.Emit(OpCodes.Conv_U1);
                    break;
                case nameof(SByte):
                    il.Emit(OpCodes.Conv_I1);
                    break;
                case nameof(Int16):
                    il.Emit(OpCodes.Conv_I2);
                    break;
                case nameof(UInt16):
                    il.Emit(OpCodes.Conv_U2);
                    break;
                case nameof(Int32):
                    il.Emit(OpCodes.Conv_I4);
                    break;
                case nameof(UInt32):
                    il.Emit(OpCodes.Conv_U4);
                    break;
                case nameof(Int64):
                    il.Emit(OpCodes.Conv_I8);
                    break;
                case nameof(UInt64):
                    il.Emit(OpCodes.Conv_U8);
                    break;
                case nameof(Single):
                    il.Emit(OpCodes.Conv_R4);
                    break;
                case nameof(Double):
                    il.Emit(OpCodes.Conv_R8);
                    break;
                case nameof(IntPtr):
                    il.Emit(OpCodes.Conv_I);
                    break;
                case nameof(UIntPtr):
                    il.Emit(OpCodes.Conv_U);
                    break;
                case nameof(Boolean):
                    il.Emit(OpCodes.Conv_I4);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Cgt_Un);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported cast to {to.FullName}");
            }
        }

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var st = src.Emit(ctx, true)!;
            var targetIsNullable = type is GenericInstanceType targetGit &&
                targetGit.ElementType.FullName == "System.Nullable`1";
            if (safe)
            {
                HashlinkMethodMarker.MarkShouldUseNative(ctx.DataContainer, ctx.Definition);
            }
            if (!targetIsNullable && type.IsValueType &&
                st != null && !st.IsValueType && !st.IsByReference)
            {
                HashlinkMethodMarker.MarkShouldUseNative(ctx.DataContainer, ctx.Definition);
            }
            if (!targetIsNullable)
            {
                // Numeric/other conversions operate on the underlying value.
                st = il.EmitNullableUnwrap(container, st)!;
            }
            if (type.IsByReference || st.IsByReference)
            {
                if (!st.IsByReference)
                {
                    il.Emit(OpCodes.Conv_U);
                }
                return type;
            }
            if (!type.IsValueType)
            {
                if (st.IsValueType)
                {
                    il.Emit(OpCodes.Box, st);
                }
                if (type.FullName != "System.Object")
                {
                    il.Emit(OpCodes.Call, ctx.RuntimeImports.phCast.MakeInstance(type));
                }
            }
            else if (!st.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
            }
            else if (st.FullName == type.FullName)
            {
                // Same type: nothing to do.
            }
            else if (type is GenericInstanceType nullable &&
                nullable.ElementType.FullName == "System.Nullable`1" &&
                nullable.GenericArguments.Count == 1)
            {
                // T -> Nullable<T>. Constructing Nullable<T>::.ctor directly
                // can mix corlib scopes; the helper guarantees the runtime's
                // Nullable<T> type identity.
                var underlying = nullable.GenericArguments[0];
                il.Emit(OpCodes.Call,
                    ctx.RuntimeImports.phMakeNullable.MakeInstance(underlying));
            }
            else
            {
                EmitNumericConvert(il, st, type);
            }
            return type;
        }
    }
}




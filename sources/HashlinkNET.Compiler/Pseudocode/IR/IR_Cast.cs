using HashlinkNET.Compiler.Utils;
using HaxeProxy.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;

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
                    il.Emit(OpCodes.Nop);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported cast to {to.FullName}");
            }
        }

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var st = src.Emit(ctx, true)!;

            if (st.FullName == type.FullName)
            {
                return st;
            }

            var targetIsNullable = type.IsNullable(out var typeNullableUnderlying);
            var srcIsNullable = st.IsNullable(out var stNullableUnderlying);

            Debug.Assert(!targetIsNullable || !srcIsNullable);
            if (safe && !st.IsValueType && !type.IsValueType
                && !st.IsByReference && !type.IsByReference)
            {
                il.Emit(OpCodes.Isinst, type);
                return type;
            }

            if (st.FullName == "System.Object" && type.IsByReference) // Load Null (object)
            {
                //il.Emit(OpCodes.Pop);
                //il.Emit(OpCodes.Ldc_I4_0);
                //il.Emit(OpCodes.Conv_U);
                return type;
            }

            if (st.IsByReference && type is GenericInstanceType git_r
                && git_r.ElementType.FullName == typeof(Ref<>).FullName)
            {
                il.Emit(OpCodes.Call, ctx.RuntimeImports.phMakeRef.MakeInstance(git_r.GenericArguments[0]));
                return type;
            }
            if (type.IsByReference && st is GenericInstanceType git_r2
                && git_r2.ElementType.FullName == typeof(Ref<>).FullName)
            {
                il.Emit(OpCodes.Call, ctx.RuntimeImports.phUnref.MakeInstance(git_r2.GenericArguments[0]));
                return type;
            }

            if (st.IsValueType && !type.IsValueType)
            {
                il.Emit(OpCodes.Box, st);
                if (type.FullName != "System.Object")
                {
                    Debug.Assert(!type.IsValueType && !type.IsByReference);
                    il.Emit(OpCodes.Call, ctx.RuntimeImports.phCast.MakeInstance(type));
                }
                return type;
            }

            if (!st.IsValueType && type.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
                return type;
            }

            if (!st.IsValueType && !type.IsValueType)
            {
                if (type.FullName == "System.Object")
                {
                    il.Emit(OpCodes.Box, st);
                }
                else
                {
                    Debug.Assert(!type.IsValueType && !type.IsByReference);
                    il.Emit(OpCodes.Call, ctx.RuntimeImports.phCast.MakeInstance(type));
                }
                return type;
            }

            if (targetIsNullable != srcIsNullable)
            {
                if ((typeNullableUnderlying ?? type).FullName == (stNullableUnderlying ?? st).FullName)
                {
                    il.Emit(OpCodes.Box, st);
                    il.Emit(OpCodes.Unbox_Any, type);
                    return type;
                }
                if (srcIsNullable)
                {
                    il.Emit(OpCodes.Box, st);
                    il.Emit(OpCodes.Unbox_Any, stNullableUnderlying);
                }

                EmitNumericConvert(il, stNullableUnderlying ?? st, typeNullableUnderlying ?? type);

                if (targetIsNullable)
                {
                    il.Emit(OpCodes.Box, typeNullableUnderlying);
                    il.Emit(OpCodes.Unbox_Any, type);
                }
                return type;
            }

            EmitNumericConvert(il, st, type);
            return type;
        }
    }
}




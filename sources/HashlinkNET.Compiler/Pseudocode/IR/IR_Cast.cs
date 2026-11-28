using HashlinkNET.Compiler.Data.Interfaces;
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
                    // bool has int32 stack representation; source values that
                    // are wider (or floating) must be narrowed first.
                    if (from.MetadataType is MetadataType.Single or MetadataType.Double
                        or MetadataType.Int64 or MetadataType.UInt64
                        or MetadataType.IntPtr or MetadataType.UIntPtr)
                    {
                        il.Emit(OpCodes.Conv_I4);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported cast to {to.FullName}");
            }
        }

        private static bool AreInheritanceRelated( TypeReference a, TypeReference b )
        {
            var ad = TryGetDefinition(a);
            var bd = TryGetDefinition(b);
            if (ad is null || bd is null)
            {
                return false;
            }
            return IsSameOrDerived(ad, bd) || IsSameOrDerived(bd, ad);
        }

        private static TypeDefinition? TryGetDefinition( TypeReference type )
        {
            if (type is TypeDefinition definition)
            {
                return definition;
            }
            try
            {
                return type.Resolve();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsSameOrDerived( TypeDefinition type, TypeDefinition target )
        {
            var guard = 0;
            TypeDefinition? current = type;
            while (current is not null && guard++ < 64)
            {
                if (ReferenceEquals(current, target) ||
                    current.FullName == target.FullName)
                {
                    return true;
                }
                foreach (var implementation in current.Interfaces)
                {
                    if (implementation.InterfaceType.FullName == target.FullName)
                    {
                        return true;
                    }
                }
                var baseType = current.BaseType;
                if (baseType is null)
                {
                    return false;
                }
                if (baseType.FullName == target.FullName)
                {
                    return true;
                }
                current = TryGetDefinition(baseType);
            }
            return false;
        }

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            var st = src.Emit(ctx, true)!;

            if (st.IsSameType(type))
            {
                return st;
            }

            // If the two reference types are related (one derives from / is
            // assignable to the other, regardless of direction), no conversion
            // is required for either Haxe or CLR.
            if (!st.IsValueType && !type.IsValueType &&
                !st.IsByReference && !type.IsByReference &&
                !st.IsPointer && !type.IsPointer &&
                AreInheritanceRelated(st, type))
            {
                return st;
            }


            if (container.TryGetData<ICast>(st, out var icast) && icast.Cast is not null)
            {
                il.Emit(OpCodes.Call, ctx.RuntimeImports.phGetTypePointer.MakeInstance(type));
                il.Emit(OpCodes.Call, icast.Cast);
                return ctx.RuntimeImports.objectType;
            }

            var targetIsNullable = type.IsNullable(out var typeNullableUnderlying);
            var srcIsNullable = st.IsNullable(out var stNullableUnderlying);

            Debug.Assert(!targetIsNullable || !srcIsNullable);
            if (safe && !st.IsValueType && !type.IsValueType
                && !st.IsByReference && !type.IsByReference)
            {
                il.Emit(OpCodes.Call, ctx.RuntimeImports.phCastSafe.MakeInstance(type));
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
                if ((typeNullableUnderlying ?? type).IsSameType(stNullableUnderlying ?? st))
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




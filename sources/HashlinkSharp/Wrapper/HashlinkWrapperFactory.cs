using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using Hashlink.Reflection.Types.Special;
using Hashlink.UnsafeUtilities;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Hashlink.Wrapper
{
    internal static class HashlinkWrapperFactory
    {
        [ThreadStatic]
        private static Dictionary<HashlinkFuncType, DynamicMethod>? hl_wrapper_cache;
        private static readonly FieldInfo FI_wrapperInfo_target = typeof(WrapperInfo)
            .GetField(nameof(WrapperInfo.target))!;
        private static readonly MethodInfo MI_WrapperHelper_AsPointer = typeof(WrapperHelper)
            .GetMethod(nameof(WrapperHelper.AsPointer))!;
        private static readonly MethodInfo MI_WrapperHelper_GetObjectFromPtr = typeof(WrapperHelper)
            .GetMethod(nameof(WrapperHelper.GetObjectFromPtr))!;
        private static readonly MethodInfo MI_WrapperHelper_InitErrorHandler = typeof(WrapperHelper)
            .GetMethod(nameof(WrapperHelper.InitErrorHandler))!;
        private static readonly MethodInfo MI_WrapperHelper_UnInitErrorHandler = typeof(WrapperHelper)
           .GetMethod(nameof(WrapperHelper.UnInitErrorHandler))!;
        private static readonly MethodInfo MI_hl_blocking = typeof(WrapperHelper)
            .GetMethod(nameof(WrapperHelper.SetBlocking))!;
        private static readonly MethodInfo MI_CheckBool = typeof(WrapperHelper)
           .GetMethod(nameof(WrapperHelper.CheckBool))!;

        private static Type GetNativeType( TypeKind kind )
        {
            if (HashlinkMarshal.PrimitiveTypes.TryGetValue(kind, out var result))
            {
                return result;
            }
            if (kind.IsRawPointer() || kind.IsManagedPointer())
            {
                return typeof(nint);
            }
            throw new InvalidCastException();
        }
        private static Type GetManageType( TypeKind kind )
        {
            if (HashlinkMarshal.PrimitiveTypes.TryGetValue(kind, out var result))
            {
                return result;
            }
            if (kind.IsRawPointer())
            {
                return typeof(nint);
            }
            else if (kind.IsManagedPointer())
            {
                return typeof(object);
            }
            throw new InvalidCastException();
        }
        private static DynamicMethod CreateWrapper( HashlinkFuncType func )
        {
            var args = func.ArgTypes;

            var targs = new Type[args.Length + 1];
            var dargs = new Type[args.Length];


            for (int i = 0; i < args.Length; i++)
            {
                dargs[i] = GetNativeType(args[i].TypeKind);
                targs[i + 1] = GetManageType(args[i].TypeKind);
            }

            targs[0] = typeof(WrapperInfo);

            var dm = new DynamicMethod("cs_to_hl+" + func.ToString(),
                GetManageType(func.ReturnType.TypeKind),
                targs, true);
            var ilg = dm.GetILGenerator();

            var loc_eh = ilg.DeclareLocal(typeof(WrapperHelper.ErrorHandle));

            for (int i = 0; i < args.Length; i++)
            {
                var t = args[i];
                var k = t.TypeKind;

                ilg.Emit(OpCodes.Ldarg, i + 1);
                if (t is HashlinkRefType rt)
                {
                    Debug.Assert(rt.RefType.TypeKind.IsValueType());

                    var l = ilg.DeclareLocal(typeof(void*), true);
                    ilg.Emit(OpCodes.Stloc, l);
                    ilg.Emit(OpCodes.Ldloc, l);

                }
                else if (k.IsManagedPointer())
                {
                    if (k == TypeKind.HNULL)
                    {
                        ilg.Emit(OpCodes.Box, typeof(Nullable<>).MakeGenericType(
                            GetManageType(((HashlinkNullType)t).ValueType.TypeKind)
                            ));
                    }
                    ilg.Emit(OpCodes.Ldc_I4, t.TypeIndex);
                    ilg.Emit(OpCodes.Call, MI_WrapperHelper_AsPointer);
                }
                else if (k == TypeKind.HBOOL)
                {
                    ilg.Emit(OpCodes.Call, MI_CheckBool);
                }
            }

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, FI_wrapperInfo_target);

            ilg.Emit(OpCodes.Ldloca, loc_eh);
            ilg.Emit(OpCodes.Call, MI_WrapperHelper_InitErrorHandler);

            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Call, MI_hl_blocking);

            ilg.EmitCalli(OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.Cdecl,
                GetNativeType(func.ReturnType.TypeKind), dargs);

            ilg.Emit(OpCodes.Ldc_I4_1);
            ilg.Emit(OpCodes.Call, MI_hl_blocking);

            ilg.Emit(OpCodes.Ldloca, loc_eh);
            ilg.Emit(OpCodes.Call, MI_WrapperHelper_UnInitErrorHandler);

            if (func.ReturnType.TypeKind.IsManagedPointer())
            {
                ilg.Emit(OpCodes.Call, MI_WrapperHelper_GetObjectFromPtr);
            }

            if (func.ReturnType.TypeKind == TypeKind.HBOOL)
            {
                ilg.Emit(OpCodes.Call, MI_CheckBool);
            }

            ilg.Emit(OpCodes.Ret);

            return dm;
        }
        public static DelegateInfo GetWrapperInfo(
            HashlinkFuncType func,
            nint target )
        {
            hl_wrapper_cache ??= [];

            if (!hl_wrapper_cache.TryGetValue(func, out var mi))
            {
                mi = CreateWrapper(func);
                hl_wrapper_cache[func] = mi;
            }

            var info = new WrapperInfo()
            {
                target = target,
            };
            return new(info, mi);
        }
        public static Delegate GetWrapper(
            HashlinkFuncType func,
            nint target,
            Type? targetType = null )
        {
            var info = GetWrapperInfo(func, target);
            if (targetType != null)
            {
                return info.method!.CreateAnonymousDelegate(info.self).CreateAdaptDelegate(targetType);
            }
            return info.method!.CreateAnonymousDelegate(info.self);
        }
    }

}

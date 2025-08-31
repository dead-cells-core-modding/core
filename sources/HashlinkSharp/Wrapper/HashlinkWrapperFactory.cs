using Hashlink.Marshaling;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
using Hashlink.UnsafeUtilities;
using Hashlink.Wrapper.Callbacks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Wrapper
{
    internal static class HashlinkWrapperFactory
    {
        private static readonly ConcurrentDictionary<HashlinkFuncType, DynamicMethod> hl_wrapper_cache = [];
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
        private static readonly MethodInfo MI_hl_blocking = typeof(HashlinkNative)
            .GetMethod(nameof(HashlinkNative.hl_blocking))!;

        private static Type GetNativeType( TypeKind kind )
        {
            if (HashlinkMarshal.PrimitiveTypes.TryGetValue(kind, out var result))
            {
                return result;
            }
            return typeof(nint);
        }
        private static Type GetManageType( TypeKind kind )
        {
            if (HashlinkMarshal.PrimitiveTypes.TryGetValue(kind, out var result))
            {
                return result;
            }
            return typeof(object);
        }
        private static DynamicMethod CreateWrapper( HashlinkFuncType func )
        {
            var args = func.ArgTypes;

            var targs = new Type[args.Length + 1];
            var dargs = new Type[args.Length];

            List<(int, LocalBuilder)>? objRefs = null;

            for (int i = 0; i < args.Length; i++)
            {
                dargs[i] = GetNativeType(args[i].TypeKind);
                targs[i + 1] = GetManageType(args[i].TypeKind);
            }

            targs[0] = typeof(WrapperInfo);

            var dm = new DynamicMethod("cs_to_hl+" + func.ToString(),
                GetManageType(func.ReturnType.TypeKind),
                targs);
            var ilg = dm.GetILGenerator();

            var loc_eh = ilg.DeclareLocal(typeof(WrapperHelper.ErrorHandle));

            for (int i = 0; i < args.Length; i++)
            {
                var t = args[i];
                var k = t.TypeKind;

                ilg.Emit(OpCodes.Ldarg, i + 1);
                if (t is HashlinkRefType rt)
                {
                    if (!rt.RefType.TypeKind.IsValueType())
                    {
                        objRefs ??= [];
                        var l = ilg.DeclareLocal(typeof(nint));

                        ilg.Emit(OpCodes.Ldind_I);

                        ilg.Emit(OpCodes.Ldc_I4, t.TypeIndex);
                        ilg.Emit(OpCodes.Call, MI_WrapperHelper_AsPointer);
                        ilg.Emit(OpCodes.Stloc, l);
                        ilg.Emit(OpCodes.Ldloca, l);
                        objRefs.Add((i + 1, l));
                    }
                    else
                    {
                        var l = ilg.DeclareLocal(typeof(void*), true);
                        ilg.Emit(OpCodes.Stloc, l);
                        ilg.Emit(OpCodes.Ldloc, l);
                    }
                }
                else if (!k.IsValueType())
                {
                    ilg.Emit(OpCodes.Ldc_I4, t.TypeIndex);
                    ilg.Emit(OpCodes.Call, MI_WrapperHelper_AsPointer);
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

            if (objRefs != null)
            {
                foreach ((var pid, var loc) in objRefs)
                {
                    ilg.Emit(OpCodes.Ldarg, pid);
                    ilg.Emit(OpCodes.Ldloc, loc);
                    ilg.Emit(OpCodes.Call, MI_WrapperHelper_GetObjectFromPtr);
                    ilg.Emit(OpCodes.Stind_I);
                }
            }

            if(!func.ReturnType.TypeKind.IsValueType())
            {
                ilg.Emit(OpCodes.Call, MI_WrapperHelper_GetObjectFromPtr);
            }

            ilg.Emit(OpCodes.Ret);

            return dm;
        }
        public static DelegateInfo GetWrapperInfo(
            HashlinkFuncType func,
            nint target)
        {
            var mi = hl_wrapper_cache.GetOrAdd(func, CreateWrapper);
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
                return info.CreateDelegate(targetType);
            }
            return info.method!.CreateAnonymousDelegate(info.self);
        }
    }

}

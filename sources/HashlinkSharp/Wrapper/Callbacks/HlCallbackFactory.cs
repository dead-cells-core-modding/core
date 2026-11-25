using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using Hashlink.Reflection.Types.Special;
using Hashlink.UnsafeUtilities;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Hashlink.Wrapper.Callbacks
{
    public static unsafe class HlCallbackFactory
    {
        [ThreadStatic]
        private static ConcurrentDictionary<HashlinkFuncType, MethodInfo>? hl_callback_cache;
        private static readonly FieldInfo FI_hlrouterinfo_entry = typeof(HlCallbackInfo)
            .GetField(nameof(HlCallbackInfo.entry))!;
        private static readonly MethodInfo MI_WrapperHelper_AsPointer = typeof(WrapperHelper)
            .GetMethod(nameof(WrapperHelper.AsPointer))!;
        private static readonly MethodInfo MI_WrapperHelper_GetObjectFromPtr = typeof(WrapperHelper)
            .GetMethod(nameof(WrapperHelper.GetObjectFromPtr))!;
        private static readonly MethodInfo MI_WrapperHelper_ThrowNETException = typeof(WrapperHelper)
            .GetMethod(nameof(WrapperHelper.ThrowNetException))!;
        private static readonly MethodInfo MI_WrapperHelper_CallbackCleanup = typeof(WrapperHelper)
            .GetMethod(nameof(WrapperHelper.CallbackCleanup))!;
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

        private static MethodInfo CreateHlCallback( HashlinkFuncType sign )
        {
            var args = sign.ArgTypes;

            var targs = new Type[args.Length + 1];
            var dargs = new Type[args.Length];

            targs[0] = typeof(HlCallbackInfo);

            for (var i = 0; i < args.Length; i++)
            {
                dargs[i] = GetManageType(args[i].TypeKind);
                targs[i + 1] = GetNativeType(args[i].TypeKind);
            }

            var dm = new DynamicMethod("hl_to_cs+" + sign.ToString(),
                GetNativeType(sign.ReturnType.TypeKind), targs, true);



            var ilg = dm.GetILGenerator();

            var resultLoc = dm.ReturnType == typeof(void) ? null : ilg.DeclareLocal(dm.ReturnType);
            var endOfMethod = ilg.DefineLabel();

            ilg.BeginExceptionBlock();

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, FI_hlrouterinfo_entry);
            ilg.Emit(OpCodes.Ldfld, DelegateInfo.FI_self);

            for (var i = 0; i < args.Length; i++)
            {

                ilg.Emit(OpCodes.Ldarg, i + 1);
                var k = args[i].TypeKind;
                if (args[i] is HashlinkRefType rtype)
                {
                    Debug.Assert(rtype.RefType.TypeKind.IsValueType() || rtype.RefType.TypeKind.IsRawPointer());
                }
                else if (k.IsManagedPointer())
                {
                    ilg.Emit(OpCodes.Call, MI_WrapperHelper_GetObjectFromPtr);
                }
                else if (k == TypeKind.HBOOL)
                {
                    // Check valid
                    ilg.Emit(OpCodes.Call, MI_CheckBool);
                }

            }

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, FI_hlrouterinfo_entry);
            ilg.Emit(OpCodes.Ldfld, DelegateInfo.FI_invokePtr);

            ilg.Emit(OpCodes.Ldc_I4_1);
            ilg.Emit(OpCodes.Call, MI_hl_blocking);

            ilg.EmitCalli(OpCodes.Calli, CallingConventions.HasThis,
                GetManageType(sign.ReturnType.TypeKind), dargs, null);

            if (resultLoc != null)
            {
                if (sign.ReturnType.TypeKind.IsManagedPointer())
                {
                    ilg.Emit(OpCodes.Ldc_I4, sign.ReturnType.TypeIndex);
                    ilg.Emit(OpCodes.Call, MI_WrapperHelper_AsPointer);
                }
                ilg.Emit(OpCodes.Stloc, resultLoc);
            }

            ilg.Emit(OpCodes.Leave, endOfMethod);


            ilg.BeginCatchBlock(typeof(Exception));

            ilg.Emit(OpCodes.Call, MI_WrapperHelper_ThrowNETException);
            ilg.Emit(OpCodes.Leave, endOfMethod);

            ilg.EndExceptionBlock();

            ilg.MarkLabel(endOfMethod);

            if (resultLoc != null)
            {
                ilg.Emit(OpCodes.Ldloc, resultLoc);
            }

            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Call, MI_hl_blocking);

            ilg.Emit(OpCodes.Call, MI_WrapperHelper_CallbackCleanup);

            if (sign.ReturnType.TypeKind == TypeKind.HBOOL)
            {
                ilg.Emit(OpCodes.Call, MI_CheckBool);
            }

            ilg.Emit(OpCodes.Ret);

            return dm;
        }

        public static HlCallback GetHlCallback( HashlinkFuncType sign )
        {
            hl_callback_cache ??= [];

            if (!hl_callback_cache.TryGetValue(sign, out var mi))
            {
                mi = CreateHlCallback(sign);
                hl_callback_cache[sign] = mi;
            }
            return new HlCallback(mi);
        }
    }
}

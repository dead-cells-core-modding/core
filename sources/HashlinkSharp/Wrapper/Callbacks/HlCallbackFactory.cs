using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using Hashlink.Trace;
using Hashlink.UnsafeUtilities;
using ModCore;
using MonoMod.Utils;
using MonoMod.Utils.Cil;
using Serilog;
using Serilog.Core;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hashlink.Wrapper.Callbacks
{
    public static unsafe class HlCallbackFactory
    {
        private static readonly ConcurrentDictionary<HashlinkFuncType, MethodInfo> hl_callback_cache = [];
        private static readonly FieldInfo FI_hlrouterinfo_entry = typeof(HlCallbackInfo)
            .GetField(nameof(HlCallbackInfo.entry))!;
        private static readonly FieldInfo FI_hlrouterinfo_directRoute = typeof(HlCallbackInfo)
            .GetField(nameof(HlCallbackInfo.directRoute))!;
        private static readonly MethodInfo MI_WrapperHelper_AsPointer = typeof(WrapperHelper)
            .GetMethod(nameof(WrapperHelper.AsPointer))!;
        private static readonly MethodInfo MI_WrapperHelper_GetObjectFromPtr = typeof(WrapperHelper)
            .GetMethod(nameof(WrapperHelper.GetObjectFromPtr))!;
        private static readonly MethodInfo MI_WrapperHelper_ThrowNETException = typeof(WrapperHelper)
            .GetMethod(nameof(WrapperHelper.ThrowNetException))!;

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

        private static MethodInfo CreateHlCallback( HashlinkFuncType sign )
        {
            var args = sign.ArgTypes;

            var targs = new Type[args.Length + 1];
            var dargs = new Type[args.Length];
   
            List<(int pid, LocalBuilder loc, int tid)>? objRefs = null;

            targs[0] = typeof(HlCallbackInfo);

            for (var i = 0; i < args.Length; i++)
            {
                dargs[i] = GetManageType(args[i].TypeKind);
                targs[i + 1] = GetNativeType(args[i].TypeKind);
            }

            if (targs.Length == 5)
            {
                Debugger.Break();
            }

            var md = new DynamicMethod("hl_router+" + sign.ToString(),
                GetNativeType(sign.ReturnType.TypeKind), targs, true);

            var ilg = md.GetILGenerator();

            var resultLoc = md.ReturnType == typeof(void) ? null : ilg.DeclareLocal(md.ReturnType);
            var endOfMethod = ilg.DefineLabel();

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, FI_hlrouterinfo_directRoute);

            var l1 = ilg.DefineLabel();
            ilg.Emit(OpCodes.Brfalse, l1);

            for (var i = 0; i < args.Length; i++)
            {
                ilg.Emit(OpCodes.Ldarg, i + 1);
            }

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, FI_hlrouterinfo_directRoute);

            ilg.Emit(OpCodes.Tailcall);
            ilg.EmitCalli(OpCodes.Calli, CallingConvention.Cdecl, md.ReturnType, targs[1..]);
            ilg.Emit(OpCodes.Ret);

            ilg.Emit(OpCodes.Nop);

            ilg.MarkLabel(l1);

            ilg.Emit(OpCodes.Nop);

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
                    if (!rtype.RefType.IsValueType)
                    {
                        objRefs ??= [];
                        var l = ilg.DeclareLocal(typeof(object));
                        ilg.Emit(OpCodes.Ldind_I);

                        ilg.Emit(OpCodes.Call, MI_WrapperHelper_GetObjectFromPtr);
                        ilg.Emit(OpCodes.Stloc, l);
                        ilg.Emit(OpCodes.Ldloca, l);
                        objRefs.Add((i + 1, l, rtype.TypeIndex));
                    }
                }
                else if (!k.IsValueType())
                {
                    ilg.Emit(OpCodes.Call, MI_WrapperHelper_GetObjectFromPtr);
                }
                
            }

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, FI_hlrouterinfo_entry);
            ilg.Emit(OpCodes.Ldfld, DelegateInfo.FI_invokePtr);
            
            ilg.EmitCalli(OpCodes.Calli, CallingConventions.HasThis,
                GetManageType(sign.ReturnType.TypeKind), dargs, null);

            if (objRefs != null)
            {
                foreach ((var pid, var loc, var tid) in objRefs)
                {
                    ilg.Emit(OpCodes.Ldarg, pid);
                    ilg.Emit(OpCodes.Ldloc, loc);
                    ilg.Emit(OpCodes.Ldc_I4, tid);
                    ilg.Emit(OpCodes.Call, MI_WrapperHelper_AsPointer);
                    ilg.Emit(OpCodes.Stind_I);
                }
            }

            if (resultLoc != null)
            {
                if (!sign.ReturnType.IsValueType)
                {
                    ilg.Emit(OpCodes.Ldc_I4, sign.ReturnType.TypeIndex);
                    ilg.Emit(OpCodes.Call, MI_WrapperHelper_AsPointer);
                }
                ilg.Emit(OpCodes.Stloc, resultLoc);
            }

            ilg.Emit(OpCodes.Leave, endOfMethod);
            

            ilg.BeginCatchBlock(typeof(Exception));

            ilg.Emit(OpCodes.Call, MI_WrapperHelper_ThrowNETException);
            ilg.Emit(OpCodes.Ldnull);
            ilg.Emit(OpCodes.Throw);

            ilg.EndExceptionBlock();

            ilg.MarkLabel(endOfMethod);

            if (resultLoc != null)
            {
                ilg.Emit(OpCodes.Ldloc, resultLoc);
            }
            ilg.Emit(OpCodes.Ret);

            return md;
        }

        public static HlCallback GetHlCallback( HashlinkFuncType sign )
        {
            var mi = hl_callback_cache.GetOrAdd(sign, CreateHlCallback);
            return new HlCallback( mi );
        }
    }
}

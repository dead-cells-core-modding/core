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
        private static readonly ConcurrentDictionary<HlFuncSign, MethodInfo> hl_callback_cache = [];
        private static readonly FieldInfo FI_hlrouterinfo_entry = typeof(HlCallbackInfo)
            .GetField(nameof(HlCallbackInfo.entry))!;
        private static readonly FieldInfo FI_hlrouterinfo_directRoute = typeof(HlCallbackInfo)
            .GetField(nameof(HlCallbackInfo.directRoute))!;
        private static readonly MethodInfo MI_HashlinkMarshal_AsPointer = typeof(HashlinkMarshal)
            .GetMethod(nameof(HashlinkMarshal.AsPointer))!;
        private static readonly MethodInfo MI_HashlinkMarshal_GetObjectFromPtr = typeof(HashlinkMarshal)
            .GetMethod(nameof(HashlinkMarshal.GetObjectFromPtr))!;

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

        private static MethodInfo CreateHlCallback( HlFuncSign sign )
        {
            var args = sign.ArgTypes;

            var targs = new Type[args.Length + 1];
            var dargs = new Type[args.Length];
   
            List<(int, LocalBuilder)>? objRefs = null;

            targs[0] = typeof(HlCallbackInfo);

            for (var i = 0; i < args.Length; i++)
            {
                dargs[i] = GetManageType(args[i].Kind);
                targs[i + 1] = GetNativeType(args[i].Kind);
            }

            var md = new DynamicMethod("hl_router_" + sign.GetHashCode(),
                GetNativeType(sign.ReturnType), targs, true);

            var ilg = md.GetILGenerator();

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

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, FI_hlrouterinfo_entry);
            ilg.Emit(OpCodes.Ldfld, DelegateInfo.FI_self);

            for (var i = 0; i < args.Length; i++)
            {
                
                ilg.Emit(OpCodes.Ldarg, i + 1);
                var k = args[i].Kind;
                if (k == TypeKind.HREF)
                {
                    if (!args[i].KindEx.IsValueType())
                    {
                        objRefs ??= [];
                        var l = ilg.DeclareLocal(typeof(object));
                        ilg.Emit(OpCodes.Ldind_I);

                        ilg.Emit(OpCodes.Call, MI_HashlinkMarshal_GetObjectFromPtr);
                        ilg.Emit(OpCodes.Stloc, l);
                        ilg.Emit(OpCodes.Ldloca, l);
                        objRefs.Add((i + 1, l));
                    }
                }
                else if (!args[i].Kind.IsValueType())
                {
                    ilg.Emit(OpCodes.Call, MI_HashlinkMarshal_GetObjectFromPtr);
                }
                
            }

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, FI_hlrouterinfo_entry);
            ilg.Emit(OpCodes.Ldfld, DelegateInfo.FI_invokePtr);
            
            ilg.EmitCalli(OpCodes.Calli, CallingConventions.HasThis,
                GetManageType(sign.ReturnType), dargs, null);

            if (objRefs != null)
            {
                foreach ((var pid, var loc) in objRefs)
                {
                    ilg.Emit(OpCodes.Ldarg, pid);
                    ilg.Emit(OpCodes.Ldloc, loc);
                    ilg.Emit(OpCodes.Ldc_I4_0);
                    ilg.Emit(OpCodes.Call, MI_HashlinkMarshal_AsPointer);
                    ilg.Emit(OpCodes.Stind_I);
                }
            }
            if (!sign.ReturnType.IsValueType())
            {
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Call, MI_HashlinkMarshal_AsPointer);
            }
            
            ilg.Emit(OpCodes.Ret);

            return md;
        }

        public static HlCallback GetHlCallback( HlFuncSign sign )
        {
            var mi = hl_callback_cache.GetOrAdd(sign, CreateHlCallback);
            var info = new HlCallbackInfo();
            return new HlCallback( mi.CreateAnonymousDelegate(info, true), info);
        }
    }
}

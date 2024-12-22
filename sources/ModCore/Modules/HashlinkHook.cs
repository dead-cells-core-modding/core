using Hashlink;
using ModCore.Hashlink;
using ModCore.Modules.Events;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class HashlinkHook : CoreModule<HashlinkHook> , IOnBeforeGameStartup
    {
        public override int Priority => ModulePriorities.HashlinkHook;

        private NativeHook nhook = null!;
        private readonly ConcurrentDictionary<Type, nint> hl2csBridge = [];
        private readonly MethodInfo fromHLObj = typeof(HashlinkObject).FindMethod(nameof(HashlinkObject.FromHashlink))!;
        private readonly MethodInfo toHLObj = typeof(HashlinkObject).FindMethod(nameof(HashlinkObject.ToHashlink))!;

        private static Type GetBridgeType(Type type)
        {
            if(type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(float) ||
                type == typeof(double) 
                )
            {
                return type;
            }
            return typeof(nint);
        }
        //WIP
        private nint Generatehl2csBridge(Type type)
        {
            var invoke = type.FindMethod("Invoke");
            if(invoke == null)
            {
                return 0;
            }
            var ps = invoke.GetParameters();
            var dm = new DynamicMethodDefinition("hl2csBridge+" + type.Name, GetBridgeType(invoke.ReturnType),
                ps.Select(x => GetBridgeType(x.ParameterType)).ToArray());
            dm.Definition.HasThis = false;

            var cur = new ILCursor(new ILContext(dm.Definition));
            cur.Emit(OpCodes.Ldc_I4, dm.Definition.Parameters.Count);
            cur.Emit(OpCodes.Newarr, typeof(object));

            int index = 0;
            foreach (var p in dm.Definition.Parameters)
            {
                cur.Emit(OpCodes.Dup);
                cur.Emit(OpCodes.Ldc_I4, index++);
                cur.Emit(OpCodes.Ldarg, p);

                if (!ps[index].ParameterType.IsPrimitive)
                {
                    cur.Emit(OpCodes.Call, fromHLObj);
                }

                cur.Emit(OpCodes.Box, ps[index].ParameterType);
                cur.EmitStelemAny(typeof(object));
            }
            cur.EmitDelegate((object[] args) =>{

            });
            if(!invoke.ReturnType.IsPrimitive)
            {
                cur.Emit(OpCodes.Call, toHLObj); 
            }
            else
            {
                cur.Emit(OpCodes.Unbox, dm.Definition.ReturnType);
            }
            cur.Emit(OpCodes.Ret);

            var method = dm.Generate();
            return method.MethodHandle.GetFunctionPointer();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate nint mt_logClientInfosHandler();
        private mt_logClientInfosHandler orig_logClientInfos = null!;

        private nint Hook_mt_logClientInfos()
        {
            var stMain = HashlinkUtils.FindTypeFromName("$Main");
            var gData = (HL_vdynamic*)HashlinkUtils.GetGlobalData(stMain);
            Logger.Information("GD: {ptr:x}", (nint)gData);
            var gameVerF = HashlinkNative.hl_dyn_geti(
                gData, HashlinkUtils.HLHash("GAME_VERSION"), HashlinkNative.InternalTypes.hlt_i32
                );
            Logger.Information("AA{a} {b:x}", gameVerF, (nint)gData);
            return orig_logClientInfos();
        }

        public void OnBeforeGameStartup()
        {
            Logger.Information("Initializing");
            nhook = NativeHook.Instance;

            Logger.Information("Hooking Hashlink");

            var plogClientInfos = HashlinkUtils.GetFunctionNativePtr(
                HashlinkUtils.FindFunction(
                    HashlinkUtils.FindTypeFromName("$Boot"), "logClientInfos"
                    )
                );
            orig_logClientInfos = nhook.CreateHook<mt_logClientInfosHandler>((nint)plogClientInfos, Hook_mt_logClientInfos);
        }


    }
}

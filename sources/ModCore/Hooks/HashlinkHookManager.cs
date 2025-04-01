using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
using Hashlink.UnsafeUtilities;
using Hashlink.Wrapper;
using Hashlink.Wrapper.Callbacks;
using ModCore.Modules;
using MonoMod.Utils;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hooks
{
    internal unsafe class HashlinkHookManager
    {
        public readonly NativeHooks.HookHandle hook;
        public readonly HlCallback callback;
        public readonly HashlinkFuncType function;

        private readonly List<Delegate> hooks = [];
        private readonly static MethodInfo MI_HookEntry = typeof(HashlinkHookManager).GetMethod(
            nameof(HookEntry), BindingFlags.NonPublic | BindingFlags.Instance)!;

        public HashlinkHookManager(nint target, HashlinkFunction func)
        {
            function = func.FuncType.BaseFunc;
            callback = HlCallbackFactory.GetHlCallback(
                HlFuncSign.Create(func.FuncType)
                );
            hook = NativeHooks.Instance.CreateHook(target, callback.NativePointer, true);
            callback.RedirectTarget = hook.Original;
            callback.Target = CreateDelegateAdapt().CreateAnonymousDelegate(this);
        }

        private DynamicMethod CreateDelegateAdapt(  )
        {
            var targs = new Type[function.ArgTypes.Length + 1];
            targs[0] = typeof(HashlinkHookManager);
            if (!HashlinkMarshal.PrimitiveTypes.TryGetValue(function.ReturnType.TypeKind, out var retType))
            {
                retType = typeof(object);
            }

            for (int i = 0; i < function.ArgTypes.Length; i++)
            {
                var t = function.ArgTypes[i];
                if (!HashlinkMarshal.PrimitiveTypes.TryGetValue(t.TypeKind, out targs[i + 1]!))
                {
                    targs[i + 1] = typeof(object);
                }
            }
            var dm = new DynamicMethod("<HookAdapt>" + function.ToString(),
                retType,
                targs,
                true
                );

            var ilg = dm.GetILGenerator();

            ilg.Emit(OpCodes.Ldarg_0);

            ilg.Emit(OpCodes.Ldc_I4, targs.Length - 1);
            ilg.Emit(OpCodes.Newarr, typeof(object));

            for (int i = 1; i < targs.Length; i++)
            {
                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Ldc_I4, i - 1);
                ilg.Emit(OpCodes.Ldarg, i);
                ilg.Emit(OpCodes.Box, targs[i]);
                ilg.Emit(OpCodes.Stelem_Ref);
            }
            ilg.Emit(OpCodes.Callvirt, MI_HookEntry);

            if (retType == typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }
            else if (retType.IsValueType)
            {
                ilg.Emit(OpCodes.Unbox_Any, retType);
            }
            ilg.Emit(OpCodes.Ret);

            return dm;
        }

        public void AddHook( Delegate hook )
        {
            ArgumentNullException.ThrowIfNull(hook);
            if (!hooks.Contains(hook))
            {
                hooks.Add(hook);
            }
            callback.RedirectTarget = 0;
        }
        public void RemoveHook( Delegate hook )
        {
            ArgumentNullException.ThrowIfNull(hook);
            hooks.Remove(hook);
            if (hooks.Count == 0)
            {
                callback.RedirectTarget = this.hook.Original;
            }
        }

        private object? HookEntry(object?[] args)
        {
            try
            {
                HashlinkClosure prev = new HashlinkClosure(function, hook.Original, 0);
                for (int i = 0; i < hooks.Count; i++)
                {
                    prev = new HashlinkClosure(function,
                        hooks[i].Bind(prev));
                }
                return prev.DynamicInvoke(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unhandled exception!");
                Environment.FailFast(ex.ToString());
                throw;
            }
        }
    }
}

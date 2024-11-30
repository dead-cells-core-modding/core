using MinHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class NativeHook : CoreModule<NativeHook>
    {
        public override int Priority => ModulePriorities.NativeHook;

        private readonly HookEngine? dskHookEngine;
        private readonly Dictionary<Delegate, HookHandle> delegate2handle = [];

        public class HookHandle(object hook, NativeHook manager)
        {
            internal object hook = hook;

            public nint Original => manager.GetOriginalPtr(this);
            public void Enable() => manager.EnableHook(this);
            public void Disable() => manager.DisableHook(this);
        }

        public NativeHook()
        {
            dskHookEngine = new HookEngine();
        }

        public HookHandle CreateHook(nint target, nint detour)
        {
            HookHandle result;
            if(dskHookEngine != null)
            {
                result = new HookHandle(dskHookEngine.CreateHook(target, detour), this);
            }
            else
            {
                throw new NotSupportedException();
            }
            result.Enable();
            return result;
        }

        public nint GetOriginalPtr(HookHandle hook)
        {
            if (dskHookEngine != null)
            {
                return ((Hook)hook.hook).Original;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public void EnableHook(HookHandle hook)
        {
            if (dskHookEngine != null)
            {
                dskHookEngine.EnableHook((Hook)hook.hook);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public void DisableHook(HookHandle hook)
        {
            if (dskHookEngine != null)
            {
                dskHookEngine.DisableHook((Hook)hook.hook);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public TTarget CreateHook<TTarget>(nint nativeFunc, TTarget target) where TTarget : Delegate
        {
            var handle = CreateHook(nativeFunc, Marshal.GetFunctionPointerForDelegate(target));
            var orig = Marshal.GetDelegateForFunctionPointer<TTarget>(handle.Original);
            delegate2handle[orig] = handle;
            return orig;
        }
        public void DisableHook(Delegate original)
        {
            DisableHook(delegate2handle[original]);
        }
        public void EnableHook(Delegate original)
        {
            EnableHook(delegate2handle[original]);
        }
    }
}

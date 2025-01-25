
using MonoMod.Core;
using System.Runtime.InteropServices;

using HookInfo = MonoMod.Core.ICoreNativeDetour;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class NativeHook : CoreModule<NativeHook>
    {
        public override int Priority => ModulePriorities.NativeHook;

        private readonly Dictionary<Delegate, HookHandle> delegate2handle = [];
        private static readonly IDetourFactory detourFactory = DetourFactory.Current;

        public class HookHandle
        {
            internal readonly HookInfo? hook;
            private readonly NativeHook manager;

            internal HookHandle( HookInfo info, NativeHook manager )
            {
                hook = info;
                this.manager = manager;
            }

            public nint Original => manager.GetOriginalPtr(this);
            public void Enable() => manager.EnableHook(this);
            public void Disable() => manager.DisableHook(this);
        }

        public NativeHook()
        {

        }

        public HookHandle CreateHook( nint target, nint detour )
        {
            HookHandle result;
            result = new HookHandle(detourFactory.CreateNativeDetour(target, detour, true), this);
            result.Enable();
            return result;
        }

        public nint GetOriginalPtr( HookHandle hook )
        {
            return hook.hook?.OrigEntrypoint ?? throw new ObjectDisposedException(nameof(hook));
        }

        public void EnableHook( HookHandle hook )
        {
            ObjectDisposedException.ThrowIf(hook.hook == null, hook);
            if (hook.hook.IsApplied)
            {
                return;
            }
            hook.hook.Apply();
        }
        public void DisableHook( HookHandle hook )
        {
            if (hook.hook == null)
            {
                return;
            }
            hook.hook.Undo();
        }

        public HookHandle GetHook( Delegate del )
        {
            return delegate2handle[del];
        }

        public TTarget CreateHook<TTarget>( nint nativeFunc, TTarget target ) where TTarget : Delegate
        {
            var handle = CreateHook(nativeFunc, Marshal.GetFunctionPointerForDelegate(target));
            var orig = Marshal.GetDelegateForFunctionPointer<TTarget>(handle.Original);
            delegate2handle[orig] = handle;
            delegate2handle[target] = handle;
            return orig;
        }
        public void DisableHook( Delegate original )
        {
            DisableHook(GetHook(original));
        }
        public void EnableHook( Delegate original )
        {
            EnableHook(GetHook(original));
        }
    }
}

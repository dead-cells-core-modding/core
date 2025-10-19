
using MonoMod.Core;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using HookInfo = MonoMod.Core.ICoreNativeDetour;

namespace ModCore.Modules
{
    /// <summary>
    /// 
    /// </summary>
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    public unsafe class NativeHooks : CoreModule<NativeHooks>
    {
        ///<inheritdoc/>
        public override int Priority => ModulePriorities.NativeHook;

        private readonly Dictionary<Delegate, HookHandle> delegate2handle = [];
        private readonly ConcurrentBag<HookHandle> hooks = [];
        private static readonly IDetourFactory detourFactory = DetourFactory.Current;
        /// <summary>
        /// A native hook
        /// </summary>
        public class HookHandle
        {
            internal readonly HookInfo? hook;
            private readonly NativeHooks manager;

            internal HookHandle( HookInfo info, NativeHooks manager )
            {
                hook = info;
                this.manager = manager;
            }

            /// <summary>
            /// Get the original code location
            /// </summary>
            public nint Original => manager.GetOriginalPtr(this);
            /// <summary>
            /// Enable hook
            /// </summary>
            public void Enable() => manager.EnableHook(this);
            /// <summary>
            /// Disable hook
            /// </summary>
            public void Disable() => manager.DisableHook(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public NativeHooks()
        {

        }

        /// <summary>
        /// Create a native hook
        /// </summary>
        /// <param name="target"></param>
        /// <param name="detour"></param>
        /// <param name="applyByDefault"></param>
        /// <returns></returns>
        public HookHandle CreateHook( nint target, nint detour, bool applyByDefault = true )
        {
            HookHandle result;
            result = new HookHandle(detourFactory.CreateNativeDetour(target, detour, applyByDefault), this);
            hooks.Add(result);
            return result;
        }

        /// <summary>
        /// Get the original code address
        /// </summary>
        /// <param name="hook"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">The hook is not enabled or has been destroyed</exception>
        public nint GetOriginalPtr( HookHandle hook )
        {
            return hook.hook?.OrigEntrypoint ?? throw new ObjectDisposedException(nameof(hook));
        }
        /// <summary>
        /// Enabel a hook
        /// </summary>
        /// <param name="hook"></param>
        public void EnableHook( HookHandle hook )
        {
            ObjectDisposedException.ThrowIf(hook.hook == null, hook);
            if (hook.hook.IsApplied)
            {
                return;
            }
            hook.hook.Apply();
        }
        /// <summary>
        /// Disable a hook
        /// </summary>
        /// <param name="hook"></param>
        public void DisableHook( HookHandle hook )
        {
            if (hook.hook == null)
            {
                return;
            }
            hook.hook.Undo();
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="del"></param>
       /// <returns></returns>
        public HookHandle GetHook( Delegate del )
        {
            return delegate2handle[del];
        }

        /// <summary>
        /// Create a native hook
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="nativeFunc"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public TTarget CreateHook<TTarget>( nint nativeFunc, TTarget target ) where TTarget : Delegate
        {
            var handle = CreateHook(nativeFunc, Marshal.GetFunctionPointerForDelegate(target));
            var orig = Marshal.GetDelegateForFunctionPointer<TTarget>(handle.Original);
            delegate2handle[orig] = handle;
            delegate2handle[target] = handle;
            return orig;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="original"></param>
        public void DisableHook( Delegate original )
        {
            DisableHook(GetHook(original));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="original"></param>
        public void EnableHook( Delegate original )
        {
            EnableHook(GetHook(original));
        }
    }
}

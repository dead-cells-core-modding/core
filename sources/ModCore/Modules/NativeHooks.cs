
using MonoMod.Core;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using HookInfo = MonoMod.Core.ICoreNativeDetour;

namespace ModCore.Modules
{
    /// <summary>
    /// Provides native function hooking through runtime detours.
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
        /// Initializes a new instance of the <see cref="NativeHooks"/> class.
        /// </summary>
        public NativeHooks()
        {

        }

        /// <summary>
        /// Create a native hook
        /// </summary>
        /// <param name="target">The address of the target function.</param>
        /// <param name="detour">The address of the detour function.</param>
        /// <param name="applyByDefault">A value indicating whether the hook is applied immediately.</param>
        /// <returns>A handle that can be used to enable or disable the hook.</returns>
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
        /// <param name="hook">The hook handle.</param>
        /// <returns>The address of the original function.</returns>
        /// <exception cref="ObjectDisposedException">The hook is not enabled or has been destroyed</exception>
        public nint GetOriginalPtr( HookHandle hook )
        {
            return hook.hook?.OrigEntrypoint ?? throw new ObjectDisposedException(nameof(hook));
        }
        /// <summary>
        /// Enables a hook
        /// </summary>
        /// <param name="hook">The hook handle to enable.</param>
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
        /// Disables a hook
        /// </summary>
        /// <param name="hook">The hook handle to disable.</param>
        public void DisableHook( HookHandle hook )
        {
            if (hook.hook == null)
            {
                return;
            }
            hook.hook.Undo();
        }

        /// <summary>
        /// Gets the hook handle associated with the specified delegate.
        /// </summary>
        /// <param name="del">The delegate associated with the hook.</param>
        /// <returns>The hook handle for the delegate.</returns>
        public HookHandle GetHook( Delegate del )
        {
            return delegate2handle[del];
        }

        /// <summary>
        /// Create a native hook
        /// </summary>
        /// <typeparam name="TTarget">The delegate type of the hook.</typeparam>
        /// <param name="nativeFunc">The address of the native function to hook.</param>
        /// <param name="target">The detour delegate.</param>
        /// <returns>A delegate that invokes the original function.</returns>
        public TTarget CreateHook<TTarget>( nint nativeFunc, TTarget target ) where TTarget : Delegate
        {
            var handle = CreateHook(nativeFunc, Marshal.GetFunctionPointerForDelegate(target));
            var orig = Marshal.GetDelegateForFunctionPointer<TTarget>(handle.Original);
            delegate2handle[orig] = handle;
            delegate2handle[target] = handle;
            return orig;
        }
        /// <summary>
        /// Disables the hook associated with the specified delegate.
        /// </summary>
        /// <param name="original">The original or detour delegate associated with the hook.</param>
        public void DisableHook( Delegate original )
        {
            DisableHook(GetHook(original));
        }
        /// <summary>
        /// Enables the hook associated with the specified delegate.
        /// </summary>
        /// <param name="original">The original or detour delegate associated with the hook.</param>
        public void EnableHook( Delegate original )
        {
            EnableHook(GetHook(original));
        }
    }
}

using BenchmarkDotNet.Attributes;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using HashlinkNET.Native.Impl;

namespace DCCMBenchmark
{
    /// <summary>
    /// Raw Hashlink invocation cost in both directions: managed code calling
    /// Hashlink closures, and Hashlink calling managed delegates.
    /// HaxeProxy/GameProxy comparisons live in <see cref="HaxeProxyBenchmarks"/>.
    /// </summary>
    [MemoryDiagnoser]
    public unsafe class InvocationBenchmarks
    {
        private HashlinkObject rawPoint = null!;
        private HashlinkClosure normalizeClosure = null!;
        private Action normalizeDelegate = null!;

        // Static so the managed callback and its executable thunk survive
        // for the whole process, regardless of how many benchmark instances
        // BenchmarkDotNet creates.
        private static HashlinkClosure? managedCallbackOwner;
        private static HashlinkClosure? managedCallback;
        private static Action? managedCallbackDelegate;
        private static int counter;

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            rawPoint = (HashlinkObject)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point").CreateInstance();
            rawPoint.SetFieldValue("x", 3d);
            rawPoint.SetFieldValue("y", 4d);

            normalizeClosure = (HashlinkClosure)rawPoint.GetFieldValue("normalize")!;
            normalizeDelegate = normalizeClosure.CreateDelegate<Action>();

            // A Hashlink closure whose body is a managed delegate. Invoking it
            // re-enters the VM, which then routes the call back into .NET.
            // The owner has to stay alive: it holds the managed callback that
            // the native closure jumps to.
            if (managedCallbackOwner == null)
            {
                var funcType = (HashlinkFuncType)HashlinkMarshal.GetHashlinkType(typeof(Action))!;
                managedCallbackOwner = new HashlinkClosure(funcType, (Action)(() => counter++));
                managedCallback = new HashlinkClosure(
                    HashlinkObjPtr.Get(managedCallbackOwner.HashlinkPointer));
                managedCallbackDelegate = managedCallback.CreateDelegate<Action>();
            }
        }

        [Benchmark(Baseline = true)]
        public void Managed_Baseline()
        {
            counter++;
        }

        // ---------------------------------------------------------------------
        // .NET -> Hashlink
        // ---------------------------------------------------------------------

        [Benchmark]
        public void Closure_DynamicInvoke()
        {
            normalizeClosure.DynamicInvoke();
        }

        [Benchmark]
        public void Closure_DelegateInvoke()
        {
            normalizeDelegate();
        }

        [Benchmark]
        public void Native_LibMathSqrt()
        {
            _ = Lib_std.math_sqrt(16);
        }

        // ---------------------------------------------------------------------
        // Hashlink -> .NET
        // ---------------------------------------------------------------------

        [Benchmark]
        public void Callback_DynamicInvoke()
        {
            managedCallback!.DynamicInvoke();
        }

        [Benchmark]
        public void Callback_DelegateInvoke()
        {
            managedCallbackDelegate!();
        }
    }
}

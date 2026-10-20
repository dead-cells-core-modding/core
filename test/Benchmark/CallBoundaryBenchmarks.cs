using BenchmarkDotNet.Attributes;
using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using Hashlink.Wrapper;
using ModCore.Native;

namespace DCCMBenchmark
{
    /// <summary>
    /// Isolates the fixed call-boundary pieces used by the cs_to_hl wrapper:
    /// the Init/UnInitErrorHandler pair, the hl2cs return-pointer stub, and
    /// the complete no-op wrapper (which includes asm_cs_hl_store_context).
    /// </summary>
    [MemoryDiagnoser]
    public unsafe class CallBoundaryBenchmarks
    {
        private WrapperHelper.ErrorHandle errorHandle;
        private Action noOpWrapper = null!;

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            var funcType = (HashlinkFuncType)HashlinkMarshal.GetHashlinkType(typeof(Action))!;
            noOpWrapper = (Action)HashlinkWrapperFactory.GetWrapper(
                funcType,
                Native.Current.asm_empty_method);
        }

        [Benchmark]
        public void ErrorHandler_InitUninit()
        {
            _ = WrapperHelper.InitErrorHandler(0, ref errorHandle);
            WrapperHelper.UnInitErrorHandler(ref errorHandle);
        }

        // Full cs_to_hl wrapper around a native 'ret'. This includes
        // InitErrorHandler, SetBlocking(false), the asm_cs_hl_store_context
        // trampoline, the target call, SetBlocking(true), and UnInit.
        //
        // asm_hl2cs_store_return_ptr is intentionally not called directly:
        // its precode/stack contract is ABI-specific and a direct managed
        // function-pointer call crashes. Its cost is covered by
        // InvocationBenchmarks.Callback_DelegateInvoke.
        [Benchmark]
        public void Wrapper_NoOp()
        {
            noOpWrapper();
        }
    }
}

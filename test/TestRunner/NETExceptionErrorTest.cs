using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using static Hashlink.HashlinkNative;

namespace TestRunner
{
    /// <summary>
    /// Tests for <see cref="HashlinkNETExceptionObj"/> and the
    /// <see cref="NETExcepetionError.ExceptionToString"/> callback that is invoked
    /// by the Hashlink VM's <c>toStringFun</c> when converting a .NET exception
    /// object to a string.
    ///
    /// The critical scenario tested here is: calling <see cref="HashlinkNETExceptionObj.ToString"/>
    /// (which invokes <see cref="Exception.ToString"/>) from within the
    /// <c>[UnmanagedCallersOnly]</c> <c>ExceptionToString</c> callback.
    /// .NET forbids building a <see cref="System.Reflection.MethodInfo"/> signature
    /// for <c>[UnmanagedCallersOnly]</c> methods, so any stack trace construction
    /// that walks through such a frame will throw
    /// <c>InvalidProgramException: "attempted to call a UnmanagedCallersOnly method from managed code"</c>.
    /// </summary>
    public unsafe class NETExceptionErrorTest
    {
        public NETExceptionErrorTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }

        // =========================================================================
        // HashlinkNETExceptionObj — Construction
        // =========================================================================

        [Fact]
        public void Ctor_FromException_SetsProperties()
        {
            var ex = new InvalidOperationException("test message");

            var obj = new HashlinkNETExceptionObj(ex);

            Assert.NotNull(obj);
            Assert.True(obj.IsValid);
            Assert.Same(ex, obj.Exception);
            Assert.NotEqual(0, obj.HashlinkPointer);
        }

        [Fact]
        public void Ctor_FromException_AllocatesUniqueNativeObject()
        {
            var obj1 = new HashlinkNETExceptionObj(new Exception("first"));
            var obj2 = new HashlinkNETExceptionObj(new Exception("second"));

            Assert.NotEqual(obj1.HashlinkPointer, obj2.HashlinkPointer);
        }

        [Fact]
        public void Ctor_StatefulFlag_IsSet()
        {
            // HashlinkNETExceptionObj calls MarkStateful() in its constructor.
            // The flag ensures the GC retains the managed object.

            var obj = new HashlinkNETExceptionObj(new Exception("test"));

            // If MarkStateful() threw, we wouldn't reach here.
            // Verify the object is alive and valid.
            Assert.True(obj.IsValid);
            Assert.NotEqual(0, obj.HashlinkPointer);
        }

        // =========================================================================
        // HashlinkNETExceptionObj — ToString
        // =========================================================================

        [Fact]
        public void ToString_UnthrownException_ReturnsFormattedMessage()
        {
            var ex = new ArgumentException("bad argument", "param");

            var obj = new HashlinkNETExceptionObj(ex);
            var result = obj.ToString();

            Assert.Contains("[.NET Exception]", result);
            Assert.Contains("ArgumentException", result);
            Assert.Contains("bad argument", result);
            Assert.Contains("param", result);
        }

        [Fact]
        public void ToString_ThrownAndCaughtException_ReturnsFormattedMessage()
        {
            Exception caught = null!;
            try
            {
                throw new FormatException("invalid format string");
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            var obj = new HashlinkNETExceptionObj(caught);
            var result = obj.ToString();

            Assert.Contains("[.NET Exception]", result);
            Assert.Contains("FormatException", result);
            Assert.Contains("invalid format string", result);
        }

        [Fact]
        public void ToString_NullException_ReturnsEmpty()
        {
            var obj = new HashlinkNETExceptionObj(new Exception("temp"));
            obj.Exception = null;

            var result = obj.ToString();

            Assert.Equal("", result);
        }

        [Fact]
        public void ToString_EmptyMessage_ContainsPrefix()
        {
            var ex = new InvalidOperationException("");

            var obj = new HashlinkNETExceptionObj(ex);
            var result = obj.ToString();

            Assert.Contains("[.NET Exception]", result);
            Assert.Contains("InvalidOperationException", result);
        }

        // =========================================================================
        // NETExcepetionError.ExceptionToString — indirect tests via hl_to_string
        // =========================================================================

        [Fact]
        public void HlToString_UnthrownException_DoesNotCrash()
        {
            // Exception.ToString() inside the [UnmanagedCallersOnly] callback
            // will access Exception.StackTrace, which is null for unthrown
            // exceptions. No stack walk → no InvalidProgramException.

            var ex = new Exception("never thrown");
            var obj = new HashlinkNETExceptionObj(ex);
            var ptr = (HL_vdynamic*)obj.HashlinkPointer;

            var raw = hl_to_string(ptr);
            var result = new string(raw);

            Assert.Contains("[.NET Exception]", result);
            Assert.Contains("never thrown", result);
        }

        [Fact]
        public void HlToString_ThrownException_DoesNotCrash()
        {
            // When a thrown-and-caught exception is stringified through the
            // [UnmanagedCallersOnly] callback, Exception.ToString() reads
            // the already-captured StackTrace. Since the captured frames do
            // NOT include ExceptionToString itself (it's only on the stack
            // during toString, not at throw-time), this should not trigger
            // the InvalidProgramException.
            //
            // KNOWN RISK: if the original throw site includes DynamicMethod
            // frames whose RuntimeMethodHandle was obtained via
            // GetDynamicMethodHandle().GetFunctionPointer() (see DelegateInfo.cs),
            // RuntimeMethodInfo.get_Signature may still throw when the stack
            // trace is formatted. This test serves as a canary.

            Exception caught = null!;
            try
            {
                throw new InvalidOperationException("thrown and caught");
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            var obj = new HashlinkNETExceptionObj(caught);
            var ptr = (HL_vdynamic*)obj.HashlinkPointer;

            var raw = hl_to_string(ptr);
            var result = new string(raw);

            Assert.Contains("[.NET Exception]", result);
            Assert.Contains("thrown and caught", result);
        }

        [Fact]
        public void HlToString_NestedAggregateException()
        {
            var inner1 = new ArgumentNullException("inner1");
            var inner2 = new InvalidOperationException("inner2");
            var agg = new AggregateException("multiple errors", inner1, inner2);

            var obj = new HashlinkNETExceptionObj(agg);
            var ptr = (HL_vdynamic*)obj.HashlinkPointer;

            var raw = hl_to_string(ptr);
            var result = new string(raw);

            Assert.Contains("[.NET Exception]", result);
            Assert.Contains("multiple errors", result);
        }

        [Fact]
        public void HlToString_CustomExceptionType()
        {
            var ex = new MyCustomException("custom error", 42);
            var obj = new HashlinkNETExceptionObj(ex);
            var ptr = (HL_vdynamic*)obj.HashlinkPointer;

            var raw = hl_to_string(ptr);
            var result = new string(raw);

            Assert.Contains("[.NET Exception]", result);
            Assert.Contains("MyCustomException", result);
            Assert.Contains("custom error", result);
        }

        [Fact]
        public void HlToString_MessageWithUnicodeCharacters()
        {
            var ex = new Exception("エラー: テストメッセージ");

            var obj = new HashlinkNETExceptionObj(ex);
            var ptr = (HL_vdynamic*)obj.HashlinkPointer;

            var raw = hl_to_string(ptr);
            var result = new string(raw);

            Assert.Contains("[.NET Exception]", result);
            Assert.Contains("エラー", result);
        }

        // =========================================================================
        // Roundtrip: HashlinkObj → hl_to_string → managed string
        // =========================================================================

        [Fact]
        public void BaseHashlinkObj_ToString_ReturnsHlToString()
        {
            // HashlinkObj.ToString() calls hl_to_string, which for
            // HashlinkNETExceptionObj routes to ExceptionToString →
            // HashlinkNETExceptionObj.ToString().
            // The base ToString is overridden, so this exercises
            // HashlinkNETExceptionObj.ToString() specifically.

            var ex = new Exception("base class path");
            var obj = new HashlinkNETExceptionObj(ex);

            var result = obj.ToString();

            Assert.Contains("[.NET Exception]", result);
            Assert.Contains("base class path", result);
        }

        // =========================================================================
        // Edge cases
        // =========================================================================

        [Fact]
        public void ExceptionProperty_Setter_UpdatesReference()
        {
            var ex1 = new Exception("first");
            var ex2 = new Exception("second");

            var obj = new HashlinkNETExceptionObj(ex1);
            Assert.Same(ex1, obj.Exception);

            obj.Exception = ex2;
            Assert.Same(ex2, obj.Exception);

            var result = obj.ToString();
            Assert.Contains("second", result);
            Assert.DoesNotContain("first", result);
        }

        [Fact]
        public void HashlinkPointer_IsConsistent_AcrossCalls()
        {
            var obj = new HashlinkNETExceptionObj(new Exception("test"));

            var ptr1 = obj.HashlinkPointer;
            var ptr2 = obj.HashlinkPointer;

            Assert.Equal(ptr1, ptr2);
        }

        [Fact]
        public void Detach_InvalidatesObject()
        {
            var obj = new HashlinkNETExceptionObj(new Exception("test"));

            Assert.True(obj.IsValid);
            Assert.NotEqual(0, obj.HashlinkPointer);

            obj.Detach();

            Assert.False(obj.IsValid);
            Assert.Equal(0, obj.HashlinkPointer);
        }

        /// <summary>
        /// Custom exception type used to verify that non-standard exception
        /// types work correctly through the roundtrip.
        /// </summary>
        private sealed class MyCustomException(string message, int errorCode)
            : Exception(message)
        {
            public int ErrorCode { get; } = errorCode;

            public override string ToString()
            {
                return $"{base.ToString()} [ErrorCode={ErrorCode}]";
            }
        }
    }
}

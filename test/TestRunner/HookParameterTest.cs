using dc;
using dc.h2d.col;
using dc.libs;
using Hashlink.Marshaling;
using ModCore.Modules;
using ModCore.Utilities;

namespace TestRunner
{
    /// <summary>
    /// Verifies that multi-parameter hooks receive arguments in the correct
    /// order and can forward them to the original function.
    /// </summary>
    public unsafe class HookParameterTest
    {
        public HookParameterTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }

        private sealed class HModHook
        {
            public double N;
            public double Mod;
            public double OriginalResult;
            public int Count;

            public double Invoke(Hook__MLib.orig_hMod orig, double n, double mod)
            {
                Count++;
                N = n;
                Mod = mod;
                OriginalResult = orig(n, mod);
                return OriginalResult;
            }
        }

        private sealed class FClampHook
        {
            public double X;
            public double Min;
            public double Max;
            public double OriginalResult;
            public int Count;

            public double Invoke(Hook__MLib.orig_fclamp orig, double x, double min, double max)
            {
                Count++;
                X = x;
                Min = min;
                Max = max;
                OriginalResult = orig(x, min, max);
                return OriginalResult;
            }
        }

        private sealed class PointHook
        {
            public Point? Self;
            public int Count;

            public void Invoke(Hook_Point.orig_normalize orig, Point self)
            {
                Count++;
                Self = self;
                // Deliberately do not call orig. The outer state must remain
                // unchanged, which proves the original normalize did not run.
            }
        }

        private sealed class NullStringHook
        {
            public dc.String? S;
            public dc.String? Sub;
            public dc.String? By;
            public int Count;

            public dc.String Invoke(
                Hook__StringTools.orig_replace orig,
                dc.String s,
                dc.String sub,
                dc.String by)
            {
                Count++;
                S = s;
                Sub = sub;
                By = by;
                // Do not call orig: replace(null, ...) would throw or crash.
                return "hooked".AsHaxeString();
            }
        }

        [Fact]
        public void Hook_TwoParameters_ForwardsValuesInOrder()
        {
            var function = HashlinkMarshal.FindFunction("libs.$MLib", "hMod");
            Assert.NotNull(function);

            var handler = new HModHook();
            var hook = new Hook__MLib.hook_hMod(handler.Invoke);
            var handle = HashlinkHooks.Instance.CreateHook(function, hook);

            try
            {
                var orig = function.CreateClosure().CreateDelegate<Hook__MLib.orig_hMod>();
                var result = orig(7, 3);

                Assert.Equal(1, handler.Count);
                Assert.Equal(7d, handler.N);
                Assert.Equal(3d, handler.Mod);
                Assert.Equal(1d, handler.OriginalResult, 6);
                Assert.Equal(1d, result, 6);
            }
            finally
            {
                handle.Disable();
            }
        }

        [Fact]
        public void Hook_ThreeParameters_ForwardsValuesInOrder()
        {
            var function = HashlinkMarshal.FindFunction("libs.$MLib", "fclamp");
            Assert.NotNull(function);

            var handler = new FClampHook();
            var hook = new Hook__MLib.hook_fclamp(handler.Invoke);
            var handle = HashlinkHooks.Instance.CreateHook(function, hook);

            try
            {
                var orig = function.CreateClosure().CreateDelegate<Hook__MLib.orig_fclamp>();
                var result = orig(15, 0, 10);

                Assert.Equal(1, handler.Count);
                Assert.Equal(15d, handler.X);
                Assert.Equal(0d, handler.Min);
                Assert.Equal(10d, handler.Max);
                Assert.Equal(10d, handler.OriginalResult, 6);
                Assert.Equal(10d, result, 6);
            }
            finally
            {
                handle.Disable();
            }
        }

        [Fact]
        public void Hook_ObjectParameter_DoesNotInvokeOrig()
        {
            double x = 3;
            double y = 4;
            var point = new Point(new(ref x), new(ref y));

            var handler = new PointHook();
            var hook = new Hook_Point.hook_normalize(handler.Invoke);
            Hook_Point.normalize += hook;

            try
            {
                point.normalize();

                Assert.Equal(1, handler.Count);
                Assert.NotNull(handler.Self);
                Assert.Equal(point.HashlinkPointer, handler.Self!.HashlinkPointer);

                // normalize would change (3, 4) to a unit vector. Because the
                // hook does not call orig, the point must stay unchanged.
                Assert.Equal(3d, point.x);
                Assert.Equal(4d, point.y);
            }
            finally
            {
                Hook_Point.normalize -= hook;
            }
        }

        [Fact]
        public void Hook_NullParameter_IsPassedAsNull()
        {
            var function = HashlinkMarshal.FindFunction("$StringTools", "replace");
            Assert.NotNull(function);

            var handler = new NullStringHook();
            var hook = new Hook__StringTools.hook_replace(handler.Invoke);
            var handle = HashlinkHooks.Instance.CreateHook(function, hook);

            try
            {
                var closure = function.CreateClosure();
                var result = closure.DynamicInvoke("abc", null, "x");

                Assert.Equal(1, handler.Count);
                Assert.NotNull(handler.S);
                Assert.Equal("abc", handler.S!.ToString());
                Assert.Null(handler.Sub);
                Assert.NotNull(handler.By);
                Assert.Equal("x", handler.By!.ToString());
                Assert.Equal("hooked", result!.ToString());
            }
            finally
            {
                handle.Disable();
            }
        }

    }
}


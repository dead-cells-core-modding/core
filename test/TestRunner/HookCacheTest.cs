using System.Reflection;
using dc.h2d.col;
using Hashlink.Marshaling;
using ModCore.Hooks;
using ModCore.Modules;

namespace TestRunner
{
    /// <summary>
    /// Verifies that adding/removing hooks invalidates the cached hook chain
    /// in <see cref="HashlinkHookManager"/>, so the next call rebuilds it.
    /// </summary>
    public unsafe class HookCacheTest
    {
        public HookCacheTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }

        private sealed class CountingHook
        {
            public int Count;
            public bool CallOrig;

            public void Invoke(Hook_Point.orig_normalize orig, Point self)
            {
                Count++;
                if (CallOrig)
                {
                    orig(self);
                }
            }
        }

        private static Hook_Point.hook_normalize CreateHook(CountingHook handler)
        {
            return new Hook_Point.hook_normalize(handler.Invoke);
        }

        private static FieldInfo CachedHookChainField =>
            typeof(HashlinkHookManager).GetField(
                "cachedHookChain",
                BindingFlags.Instance | BindingFlags.NonPublic)!;

        [Fact]
        public void HookChain_AddAndRemove_InvalidatesCache()
        {
            double x = 3;
            double y = 4;
            var point = new Point(new(ref x), new(ref y));

            var a = new CountingHook { CallOrig = true };
            var b = new CountingHook { CallOrig = true };
            var c = new CountingHook { CallOrig = true };
            var hookA = CreateHook(a);
            var hookB = CreateHook(b);
            var hookC = CreateHook(c);

            Hook_Point.normalize += hookA;
            try
            {
                point.normalize();
                Assert.Equal(1, a.Count);

                // Cache is now built. Adding a hook must invalidate it.
                Hook_Point.normalize += hookB;
                point.normalize();
                Assert.Equal(2, a.Count);
                Assert.Equal(1, b.Count);

                // Add another hook after the chain was rebuilt.
                Hook_Point.normalize += hookC;
                point.normalize();
                Assert.Equal(3, a.Count);
                Assert.Equal(2, b.Count);
                Assert.Equal(1, c.Count);

                // Removing a hook must invalidate the cache too.
                Hook_Point.normalize -= hookB;
                point.normalize();
                Assert.Equal(4, a.Count);
                Assert.Equal(2, b.Count);
                Assert.Equal(2, c.Count);

                // Removing the last hooks must return to the original function.
                Hook_Point.normalize -= hookA;
                Hook_Point.normalize -= hookC;
                point.normalize();
                Assert.Equal(4, a.Count);
                Assert.Equal(2, b.Count);
                Assert.Equal(2, c.Count);
            }
            finally
            {
                Hook_Point.normalize -= hookA;
                Hook_Point.normalize -= hookB;
                Hook_Point.normalize -= hookC;
            }
        }

        [Fact]
        public void HookChain_EnableDisable_InvalidatesCache()
        {
            double x = 3;
            double y = 4;
            var point = new Point(new(ref x), new(ref y));

            var handler = new CountingHook { CallOrig = true };
            var hook = CreateHook(handler);
            var handle = HashlinkHooks.Instance.CreateHook(
                "h2d.col.Point",
                "normalize",
                hook,
                enableByDefault: false);

            try
            {
                handle.Enable();
                point.normalize();
                Assert.Equal(1, handler.Count);

                handle.Disable();
                point.normalize();
                Assert.Equal(1, handler.Count);

                handle.Enable();
                point.normalize();
                Assert.Equal(2, handler.Count);

                handle.Disable();
                point.normalize();
                Assert.Equal(2, handler.Count);
            }
            finally
            {
                handle.Disable();
            }
        }

        [Fact]
        public void HookChain_CachedChain_IsClearedOnAddAndRemove()
        {
            var function = HashlinkMarshal.FindFunction("h2d.col.Point", "normalize");
            var a = new CountingHook { CallOrig = true };
            var b = new CountingHook { CallOrig = true };
            var hookA = CreateHook(a);
            var hookB = CreateHook(b);

            double x = 3;
            double y = 4;
            var point = new Point(new(ref x), new(ref y));

            Hook_Point.normalize += hookA;
            try
            {
                point.normalize();
                var manager = HashlinkHooks.Instance.GetManager(function);
                var field = CachedHookChainField;

                Assert.NotNull(field.GetValue(manager));

                Hook_Point.normalize += hookB;
                Assert.Null(field.GetValue(manager));

                point.normalize();
                Assert.NotNull(field.GetValue(manager));

                Hook_Point.normalize -= hookB;
                Assert.Null(field.GetValue(manager));

                point.normalize();
                Assert.NotNull(field.GetValue(manager));
            }
            finally
            {
                Hook_Point.normalize -= hookA;
                Hook_Point.normalize -= hookB;
            }
        }
    }
}

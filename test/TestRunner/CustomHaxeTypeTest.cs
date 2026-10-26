using System.Collections.Concurrent;
using System.Threading;
using dc.hl;
using dc.hxd.fs;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.DynamicAccess;
using HaxeProxy.Runtime;

using static Hashlink.HashlinkNative;

namespace TestRunner
{
    /// <summary>
    /// Tests for managed classes that are exposed to Hashlink as custom Haxe
    /// types: instance fields, static fields, dynamic access and overrides.
    /// </summary>
    public unsafe class CustomHaxeTypeTest
    {
        private class CustomFileEntry : FileEntry
        {
            public int INSTANCE_INT = 123;
            public double INSTANCE_DOUBLE = 1.5;
            public CustomFileEntry? CHILD = null;

            public class StaticClass : Class
            {
                public int STATIC_INT = 7;
            }

            public new static StaticClass Class { get; } = new();

            public bool OverrideCalled;

            public override int getSign()
            {
                OverrideCalled = true;
                return 4242;
            }

            public override void load(HlAction onReady)
            {
                onReady();
            }
        }

        public CustomHaxeTypeTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }

        [Fact]
        public void CustomType_InstanceFields_ReadWriteThroughHashlink()
        {
            var instance = new CustomFileEntry();
            var hashlinkObject = (HashlinkObject)instance.HashlinkObj;

            Assert.True(hashlinkObject.HasField("INSTANCE_INT"));
            Assert.True(hashlinkObject.HasField("INSTANCE_DOUBLE"));
            Assert.False(hashlinkObject.HasField("MISSING_FIELD"));

            Assert.Equal(123, Assert.IsType<int>(hashlinkObject.GetFieldValue("INSTANCE_INT")));
            Assert.Equal(1.5, Assert.IsType<double>(hashlinkObject.GetFieldValue("INSTANCE_DOUBLE")));

            hashlinkObject.SetFieldValue("INSTANCE_INT", 321);
            hashlinkObject.SetFieldValue("INSTANCE_DOUBLE", 2.5);

            Assert.Equal(321, instance.INSTANCE_INT);
            Assert.Equal(2.5, instance.INSTANCE_DOUBLE);
            Assert.Equal(321, Assert.IsType<int>(hashlinkObject.GetFieldValue("INSTANCE_INT")));
            Assert.Equal(2.5, Assert.IsType<double>(hashlinkObject.GetFieldValue("INSTANCE_DOUBLE")));

            GC.KeepAlive(instance);
        }

        [Fact]
        public void CustomType_DynamicAccess_ReadWriteInstanceFields()
        {
            var instance = new CustomFileEntry();
            var dynamicObject = instance.HashlinkObj.AsDynamic();

            dynamicObject.INSTANCE_INT = 777;
            dynamicObject.INSTANCE_DOUBLE = 3.25;

            Assert.Equal(777, instance.INSTANCE_INT);
            Assert.Equal(3.25, instance.INSTANCE_DOUBLE);
            Assert.Equal(777, (int)dynamicObject.INSTANCE_INT);
            Assert.Equal(3.25, (double)dynamicObject.INSTANCE_DOUBLE);

            GC.KeepAlive(instance);
        }

        [Fact]
        public void CustomType_StaticFields_ReadWriteThroughClass()
        {
            dynamic classObject = HaxeProxyUtils
                .GetClass<Class>(typeof(CustomFileEntry))
                .HashlinkObj;

            Assert.Equal(7, (int)classObject.STATIC_INT);

            classObject.STATIC_INT = 99;

            Assert.Equal(99, CustomFileEntry.Class.STATIC_INT);
            Assert.Equal(99, (int)classObject.STATIC_INT);

            GC.KeepAlive(classObject);
        }

        [Fact]
        public void CustomType_OverrideMethod_IsUsedFromHashlink()
        {
            var instance = new CustomFileEntry();
            var hashlinkObject = (HashlinkObject)instance.HashlinkObj;

            var dynamicObject = hashlinkObject.AsDynamic();
            Assert.Equal(4242, (int)dynamicObject.getSign());
            Assert.True(instance.OverrideCalled);

            var closure = hashlinkObject.GetFieldValue("getSign") as Hashlink.Proxy.Clousre.HashlinkClosure;
            Assert.NotNull(closure);
            Assert.Equal(4242, (int)closure.DynamicInvoke()!);

            GC.KeepAlive(instance);
        }

        [Fact]
        public void CustomType_ConcurrentInstanceField_ReadWrite_WhileGcRuns()
        {
            const int readerCount = 4;
            const int readRounds = 300;
            const int writeCount = 300;

            var instance = new CustomFileEntry();
            var hashlinkObject = (HashlinkObject)instance.HashlinkObj;
            hashlinkObject.SetFieldValue("INSTANCE_INT", 0);

            using var start = new ManualResetEventSlim(false);
            using var completed = new CountdownEvent(readerCount + 2);
            var errors = new ConcurrentQueue<Exception>();
            var threads = new List<Thread>();

            void Run(Action action)
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        HashlinkMarshal.EnsureThreadRegistered();
                        start.Wait();
                        action();
                    }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                    finally
                    {
                        completed.Signal();
                    }
                })
                {
                    IsBackground = true,
                };

                threads.Add(thread);
                thread.Start();
            }

            for (var reader = 0; reader < readerCount; reader++)
            {
                Run(() =>
                {
                    for (var round = 0; round < readRounds; round++)
                    {
                        var value = hashlinkObject.GetFieldValue("INSTANCE_INT");
                        if (value is not int intValue || intValue < 0 || intValue > writeCount)
                        {
                            throw new InvalidOperationException(
                                $"Custom instance field was corrupted during race: {value}.");
                        }

                        Thread.Yield();
                    }
                });
            }

            Run(() =>
            {
                for (var i = 0; i < writeCount; i++)
                {
                    hashlinkObject.SetFieldValue("INSTANCE_INT", i);
                    Thread.Yield();
                }
            });

            Run(() =>
            {
                for (var i = 0; i < 50; i++)
                {
                    hl_gc_major();
                    Thread.Sleep(1);
                }
            });

            start.Set();
            Assert.True(
                completed.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken),
                "Custom type concurrent field workers timed out.");

            foreach (var thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A worker thread did not stop.");
            }

            Assert.Empty(errors);
            Assert.InRange(instance.INSTANCE_INT, 0, writeCount);

            GC.KeepAlive(instance);
        }

        [Fact]
        public void CustomType_IndirectChildReference_ReadWriteAndSurvivesGc()
        {
            var parent = new CustomFileEntry();
            var child = new CustomFileEntry();
            var grandChild = new CustomFileEntry();

            var parentObject = (HashlinkObject)parent.HashlinkObj;
            var childObject = (HashlinkObject)child.HashlinkObj;

            parentObject.SetFieldValue("CHILD", child);
            childObject.SetFieldValue("CHILD", grandChild);

            Assert.True(parentObject.HasField("CHILD"));
            Assert.Same(child, parent.CHILD);
            Assert.Same(grandChild, child.CHILD);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            hl_gc_major();

            Assert.Same(child, parent.CHILD);
            Assert.Same(grandChild, child.CHILD);
            Assert.Equal(child.HashlinkPointer, parent.CHILD!.HashlinkPointer);
            Assert.Equal(grandChild.HashlinkPointer, child.CHILD!.HashlinkPointer);

            GC.KeepAlive(parent);
            GC.KeepAlive(child);
            GC.KeepAlive(grandChild);
        }

        [Fact]
        public void CustomType_AsHaxe_ReturnsManagedInstance()
        {
            var instance = new CustomFileEntry();
            var proxy = instance.HashlinkObj.AsHaxe<CustomFileEntry>();

            Assert.Same(instance, proxy);
            Assert.Equal(instance.HashlinkPointer, proxy.HashlinkPointer);

            GC.KeepAlive(instance);
        }

        [Fact]
        public void CustomType_ConcurrentGlobalValue_ReturnsConsistentClass()
        {
            const int workerCount = 4;
            const int iterations = 500;

            using var start = new ManualResetEventSlim(false);
            using var completed = new CountdownEvent(workerCount + 1);
            var errors = new ConcurrentQueue<Exception>();
            var results = new ConcurrentBag<Class>();
            var threads = new List<Thread>();

            void Run(Action action)
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        HashlinkMarshal.EnsureThreadRegistered();
                        start.Wait();
                        action();
                    }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                    finally
                    {
                        completed.Signal();
                    }
                })
                {
                    IsBackground = true,
                };

                threads.Add(thread);
                thread.Start();
            }

            for (var worker = 0; worker < workerCount; worker++)
            {
                Run(() =>
                {
                    for (var i = 0; i < iterations; i++)
                    {
                        var classValue = HaxeProxyUtils.GetClass<Class>(typeof(CustomFileEntry));

                        if (classValue.HashlinkPointer == 0)
                        {
                            throw new InvalidOperationException(
                                "Custom type global value returned a null pointer.");
                        }

                        results.Add(classValue);
                    }
                });
            }

            Run(() =>
            {
                for (var i = 0; i < 50; i++)
                {
                    hl_gc_major();
                    Thread.Sleep(1);
                }
            });

            start.Set();
            Assert.True(
                completed.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken),
                "Custom global-value workers timed out.");

            foreach (var thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A worker thread did not stop.");
            }

            Assert.Empty(errors);

            var first = results.First();
            Assert.All(results, value => Assert.Same(first, value));
            Assert.NotEqual(0, first.HashlinkPointer);

            GC.KeepAlive(first);
        }
    }
}




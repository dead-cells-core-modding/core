using System.Collections.Concurrent;
using System.Threading;
using dc.h2d.col;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using HaxeProxy.Runtime;

using static Hashlink.HashlinkNative;

namespace TestRunner
{
    /// <summary>
    /// Verifies that AsHaxe/AsHaxeString proxy creation is safe when multiple
    /// threads race on the same Hashlink object and the HL GC runs concurrently.
    /// </summary>
    public unsafe class AsHaxeRaceTest
    {
        public AsHaxeRaceTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }

        private const int WorkerCount = 4;
        private const int IterationsPerWorker = 500;

        private static void RunConcurrent(
            Action worker,
            bool includeGc = true)
        {
            using var start = new ManualResetEventSlim(false);
            using var completed = new CountdownEvent(WorkerCount + (includeGc ? 1 : 0));
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

            for (var i = 0; i < WorkerCount; i++)
            {
                Run(worker);
            }

            if (includeGc)
            {
                Run(() =>
                {
                    for (var i = 0; i < 50; i++)
                    {
                        hl_gc_major();
                        Thread.Sleep(1);
                    }
                });
            }

            start.Set();
            Assert.True(
                completed.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken),
                "AsHaxe race workers timed out.");

            foreach (var thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A worker thread did not stop.");
            }

            Assert.Empty(errors);
        }

        [Fact]
        public void AsHaxe_SameObject_ConcurrentCalls_ReturnSameProxy()
        {
            var pointType = (Hashlink.Reflection.Types.HashlinkObjectType)
                HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");
            var rawPoint = (HashlinkObject)pointType.CreateInstance();
            var results = new ConcurrentBag<Point>();

            RunConcurrent(() =>
            {
                for (var i = 0; i < IterationsPerWorker; i++)
                {
                    var proxy = rawPoint.AsHaxe<Point>();

                    if (proxy.HashlinkPointer != rawPoint.HashlinkPointer)
                    {
                        throw new InvalidOperationException(
                            "AsHaxe returned a proxy with a different Hashlink pointer.");
                    }

                    results.Add(proxy);
                }
            });

            var first = results.First();
            Assert.All(results, proxy => Assert.Same(first, proxy));
            Assert.Equal(rawPoint.HashlinkPointer, first.HashlinkPointer);

            GC.KeepAlive(rawPoint);
        }

        [Fact]
        public void AsHaxeString_SameObject_ConcurrentCalls_ReturnSameProxy()
        {
            var raw = new HashlinkString("as-haxe-race");
            var results = new ConcurrentBag<dc.String>();

            RunConcurrent(() =>
            {
                for (var i = 0; i < IterationsPerWorker; i++)
                {
                    var proxy = raw.AsHaxe<dc.String>();

                    if (proxy.HashlinkPointer != raw.HashlinkPointer)
                    {
                        throw new InvalidOperationException(
                            "AsHaxeString returned a proxy with a different Hashlink pointer.");
                    }

                    results.Add(proxy);
                }
            });

            var first = results.First();
            Assert.All(results, proxy => Assert.Same(first, proxy));
            Assert.Equal("as-haxe-race", first.ToString());

            GC.KeepAlive(raw);
        }
    }
}

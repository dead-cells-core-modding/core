using System.Collections.Concurrent;
using System.Threading;
using dc.h2d.col;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;

using static Hashlink.HashlinkNative;

namespace TestRunner
{
    /// <summary>
    /// Verifies primitive field marshaling and generated-proxy field access,
    /// including read/write races while the Hashlink GC is running.
    /// </summary>
    public unsafe class PrimitiveFieldTest
    {
        public PrimitiveFieldTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }

        [Fact]
        public void PrimitiveFields_RoundTrip()
        {
            var obj = new HashlinkDynObj();

            obj.SetFieldValue("i32", 42);
            obj.SetFieldValue("i64", 1234567890123L);
            obj.SetFieldValue("f32", 3.5f);
            obj.SetFieldValue("f64", 4.25d);
            obj.SetFieldValue("flag", true);
            obj.SetFieldValue("u8", (byte)7);
            obj.SetFieldValue("u16", (ushort)65530);

            Assert.Equal(42, Assert.IsType<int>(obj.GetFieldValue("i32")));
            Assert.Equal(1234567890123L, Assert.IsType<long>(obj.GetFieldValue("i64")));
            Assert.Equal(3.5f, Assert.IsType<float>(obj.GetFieldValue("f32")));
            Assert.Equal(4.25d, Assert.IsType<double>(obj.GetFieldValue("f64")));
            Assert.True(Assert.IsType<bool>(obj.GetFieldValue("flag")));
            Assert.Equal((byte)7, Assert.IsType<byte>(obj.GetFieldValue("u8")));
            Assert.Equal((ushort)65530, Assert.IsType<ushort>(obj.GetFieldValue("u16")));

            GC.KeepAlive(obj);
        }

        [Fact]
        public void ProxyPrimitiveFields_ConcurrentReadWrite_WhileHashlinkGcRuns()
        {
            const int readerCount = 4;
            const int readRounds = 500;
            const int writeCount = 500;

            double x = 0;
            double y = 1;
            var point = new Point(new(ref x), new(ref y));

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
                        var px = point.x;
                        var py = point.y;

                        if (!double.IsFinite(px) || !double.IsFinite(py))
                        {
                            throw new InvalidOperationException(
                                "Primitive proxy field became non-finite during race.");
                        }

                        if (px < 0 || px > writeCount || py < 0 || py > writeCount + 1)
                        {
                            throw new InvalidOperationException(
                                $"Primitive proxy field out of range: x={px}, y={py}.");
                        }

                        Thread.Yield();
                    }
                });
            }

            Run(() =>
            {
                for (var i = 0; i < writeCount; i++)
                {
                    point.x = i;
                    point.y = i + 1;
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
                "Concurrent primitive-field workers timed out.");

            foreach (var thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A worker thread did not stop.");
            }

            Assert.Empty(errors);
            Assert.True(point.HashlinkPointer != 0);
            Assert.InRange(point.x, 0, writeCount);
            Assert.InRange(point.y, 0, writeCount + 1);

            GC.KeepAlive(point);
        }
    }
}

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;

using static Hashlink.HashlinkNative;

namespace TestRunner
{
    /// <summary>
    /// GC tests for Hashlink wrappers. These tests allow a small amount of
    /// retention because the HL GC and the CLR finalizer/weak-reference
    /// machinery are not guaranteed to collect every object synchronously.
    /// </summary>
    public unsafe class GcStressTest
    {
        public GcStressTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }

        private const int ObjectCount = 2_000;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] CreateUnrootedStrings()
        {
            var refs = new WeakReference[ObjectCount];
            for (var i = 0; i < refs.Length; i++)
            {
                var value = new HashlinkString($"gc-stress-{i}");
                refs[i] = new WeakReference(value);
            }

            return refs;
        }

        private static void ForceCollection()
        {
            for (var i = 0; i < 3; i++)
            {
                GC.Collect(
                    GC.MaxGeneration,
                    GCCollectionMode.Forced,
                    blocking: true,
                    compacting: true);
                GC.WaitForPendingFinalizers();
            }

            hl_gc_major();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (HashlinkDynObj Parent, nint Child, nint GrandChild) CreateIndirectChain()
        {
            var parent = new HashlinkDynObj();
            var child = new HashlinkDynObj();
            var grandChild = new HashlinkString("gc-indirect-grandchild");

            var childPtr = child.HashlinkPointer;
            var grandChildPtr = grandChild.HashlinkPointer;

            child.SetFieldValue("child", grandChild);
            parent.SetFieldValue("child", child);

            // child and grandChild wrappers leave scope here. Only the parent
            // is rooted, so the child objects must survive through indirect
            // native references.
            return (parent, childPtr, grandChildPtr);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static HashlinkDynObj CreateIndirectValueRoot()
        {
            var parent = new HashlinkDynObj();
            var child = new HashlinkDynObj();

            child.SetFieldValue("value", new HashlinkString("indirect-rw-initial"));
            parent.SetFieldValue("child", child);

            return parent;
        }
        [Fact]
        public void UnrootedWrappers_AreMostlyCollected()
        {
            var refs = CreateUnrootedStrings();

            ForceCollection();

            var alive = refs.Count(reference => reference.IsAlive);
            var allowed = ObjectCount / 20; // tolerate up to 5% delayed collection

            Assert.True(
                alive <= allowed,
                $"Expected at most {allowed} retained wrappers, but {alive}/{ObjectCount} are still alive.");
        }

        [Fact]
        public void RepeatedAllocation_DoesNotGrowManagedMemoryUnbounded()
        {
            ForceCollection();
            var baseline = GC.GetTotalMemory(forceFullCollection: true);

            for (var round = 0; round < 5; round++)
            {
                _ = CreateUnrootedStrings();
                ForceCollection();
            }

            var after = GC.GetTotalMemory(forceFullCollection: true);
            var growth = after - baseline;
            const long allowedGrowth = 32 * 1024 * 1024;

            Assert.True(
                growth <= allowedGrowth,
                $"Managed memory grew by {growth / (1024 * 1024)} MiB, " +
                $"which exceeds the allowed {allowedGrowth / (1024 * 1024)} MiB.");
        }

        [Fact]
        public void RootedWrapper_SurvivesHashlinkGc()
        {
            var value = new HashlinkString("gc-rooted-value");

            hl_gc_major();

            Assert.True(value.IsValid);
            Assert.Equal("gc-rooted-value", value.ToString());

            GC.KeepAlive(value);
        }

        [Fact]
        public void IndirectReferences_KeepChildObjectsAlive()
        {
            var (parent, childPtr, grandChildPtr) = CreateIndirectChain();

            ForceCollection();

            Assert.True(
                hl_gc_get_memsize((void*)childPtr) > 0,
                "The child object was collected even though the parent references it.");
            Assert.True(
                hl_gc_get_memsize((void*)grandChildPtr) > 0,
                "The grandchild object was collected even though the child references it.");

            var grandChild = HashlinkMarshal.ConvertHashlinkObject(
                HashlinkObjPtr.Get(grandChildPtr)) as HashlinkString;

            Assert.NotNull(grandChild);
            Assert.Equal("gc-indirect-grandchild", grandChild.ToString());

            GC.KeepAlive(parent);
        }

        [Fact]
        public void ConcurrentRootedReads_WhileHashlinkGcRuns_StayValid()
        {
            const int objectCount = 64;
            const int readerCount = 4;
            const int readRounds = 200;

            var values = new HashlinkString[objectCount];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = new HashlinkString($"gc-concurrent-root-{i}");
            }

            using var start = new ManualResetEventSlim(false);
            using var completed = new CountdownEvent(readerCount + 1);
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
                        for (var i = 0; i < values.Length; i++)
                        {
                            if (!values[i].IsValid)
                            {
                                throw new InvalidOperationException(
                                    $"Rooted object {i} became invalid during concurrent GC.");
                            }
                        }

                        Thread.Yield();
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
            Assert.True(completed.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken), "Concurrent readers timed out.");

            foreach (var thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A worker thread did not stop.");
            }

            Assert.Empty(errors);

            for (var i = 0; i < values.Length; i++)
            {
                Assert.True(values[i].IsValid);
                Assert.Equal($"gc-concurrent-root-{i}", values[i].ToString());
            }

            GC.KeepAlive(values);
        }

        [Fact]
        public void ConcurrentAllocation_WhileHashlinkGcRuns_DoesNotCrash()
        {
            const int workerCount = 4;
            const int objectsPerWorker = 500;

            using var start = new ManualResetEventSlim(false);
            using var completed = new CountdownEvent(workerCount + 1);
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

            for (var worker = 0; worker < workerCount; worker++)
            {
                var workerIndex = worker;
                Run(() =>
                {
                    for (var i = 0; i < objectsPerWorker; i++)
                    {
                        var text = $"gc-concurrent-alloc-{workerIndex}-{i}";
                        var value = new HashlinkString(text);

                        if (!value.IsValid)
                        {
                            throw new InvalidOperationException("New object became invalid immediately.");
                        }

                        if (value.ToString() != text)
                        {
                            throw new InvalidOperationException("Concurrent allocation corrupted string content.");
                        }
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
            Assert.True(completed.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken), "Concurrent allocation timed out.");

            foreach (var thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A worker thread did not stop.");
            }

            Assert.Empty(errors);
        }

        [Fact]
        public void ConcurrentIndirectReferences_WhileHashlinkGcRuns_StayValid()
        {
            const int readerCount = 4;
            const int readRounds = 200;

            var (parent, _, _) = CreateIndirectChain();

            using var start = new ManualResetEventSlim(false);
            using var completed = new CountdownEvent(readerCount + 1);
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
                        var child = parent.GetFieldValue("child") as HashlinkDynObj;
                        if (child is null || !child.IsValid)
                        {
                            throw new InvalidOperationException(
                                "Indirect child became invalid during concurrent GC.");
                        }

                        var grandChild = child.GetFieldValue("child") as HashlinkString;
                        if (grandChild is null || !grandChild.IsValid)
                        {
                            throw new InvalidOperationException(
                                "Indirect grandchild became invalid during concurrent GC.");
                        }

                        if (grandChild.ToString() != "gc-indirect-grandchild")
                        {
                            throw new InvalidOperationException(
                                "Indirect grandchild content was corrupted during concurrent GC.");
                        }

                        Thread.Yield();
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
                "Concurrent indirect-reference readers timed out.");

            foreach (var thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A worker thread did not stop.");
            }

            Assert.Empty(errors);
            Assert.True(parent.IsValid);

            var finalChild = parent.GetFieldValue("child") as HashlinkDynObj;
            Assert.NotNull(finalChild);
            Assert.True(finalChild.IsValid);

            var finalGrandChild = finalChild.GetFieldValue("child") as HashlinkString;
            Assert.NotNull(finalGrandChild);
            Assert.True(finalGrandChild.IsValid);
            Assert.Equal("gc-indirect-grandchild", finalGrandChild.ToString());

            GC.KeepAlive(parent);
        }

        [Fact]
        public void ConcurrentReadWrite_WhileHashlinkGcRuns_DoesNotCorruptValues()
        {
            const int readerCount = 4;
            const int readRounds = 300;
            const int writeCount = 300;

            var parent = new HashlinkDynObj();
            parent.SetFieldValue("value", new HashlinkString("rw-race-initial"));

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
                        var value = parent.GetFieldValue("value") as HashlinkString;
                        if (value is null || !value.IsValid)
                        {
                            throw new InvalidOperationException(
                                "Field value became null/invalid during read/write race.");
                        }

                        if (!value.ToString().StartsWith("rw-race-", StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                "Field value was corrupted during read/write race.");
                        }

                        Thread.Yield();
                    }
                });
            }

            Run(() =>
            {
                for (var i = 0; i < writeCount; i++)
                {
                    parent.SetFieldValue("value", new HashlinkString($"rw-race-{i}"));
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
                "Concurrent read/write readers timed out.");

            foreach (var thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A worker thread did not stop.");
            }

            Assert.Empty(errors);
            Assert.True(parent.IsValid);

            var finalValue = parent.GetFieldValue("value") as HashlinkString;
            Assert.NotNull(finalValue);
            Assert.True(finalValue.IsValid);
            Assert.StartsWith("rw-race-", finalValue.ToString());

            GC.KeepAlive(parent);
        }

        [Fact]
        public void ConcurrentIndirectReadWrite_WhileHashlinkGcRuns_DoesNotCorruptValues()
        {
            const int readerCount = 4;
            const int readRounds = 300;
            const int writeCount = 300;

            var parent = CreateIndirectValueRoot();

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
                        var child = parent.GetFieldValue("child") as HashlinkDynObj;
                        if (child is null || !child.IsValid)
                        {
                            throw new InvalidOperationException(
                                "Indirect child became invalid during indirect read/write race.");
                        }

                        var value = child.GetFieldValue("value") as HashlinkString;
                        if (value is null || !value.IsValid)
                        {
                            throw new InvalidOperationException(
                                "Indirect value became invalid during indirect read/write race.");
                        }

                        if (!value.ToString().StartsWith("indirect-rw-", StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                "Indirect value was corrupted during indirect read/write race.");
                        }

                        Thread.Yield();
                    }
                });
            }

            Run(() =>
            {
                for (var i = 0; i < writeCount; i++)
                {
                    var child = parent.GetFieldValue("child") as HashlinkDynObj;
                    if (child is null || !child.IsValid)
                    {
                        throw new InvalidOperationException(
                            "Writer could not resolve indirect child.");
                    }

                    child.SetFieldValue("value", new HashlinkString($"indirect-rw-{i}"));
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
                "Concurrent indirect read/write workers timed out.");

            foreach (var thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A worker thread did not stop.");
            }

            Assert.Empty(errors);
            Assert.True(parent.IsValid);

            var finalChild = parent.GetFieldValue("child") as HashlinkDynObj;
            Assert.NotNull(finalChild);
            Assert.True(finalChild.IsValid);

            var finalValue = finalChild.GetFieldValue("value") as HashlinkString;
            Assert.NotNull(finalValue);
            Assert.True(finalValue.IsValid);
            Assert.StartsWith("indirect-rw-", finalValue.ToString());

            GC.KeepAlive(parent);
        }
    }
}









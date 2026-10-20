using System.Collections.Concurrent;
using System.Threading;
using Hashlink.Marshaling;
using ModCore.Native;

namespace TestRunner
{
    /// <summary>
    /// Stress-style tests for <see cref="ExecutableMemoryManager{T}"/>:
    /// many allocations, freeing across pages, and reuse of released cells.
    /// </summary>
    public unsafe class ExecutableMemoryManagerTest
    {
        public ExecutableMemoryManagerTest()
        {
            HashlinkMarshal.EnsureThreadRegistered();
        }

        [Fact]
        public void AllocFree_ManyCells_ReusesExistingCells()
        {
            var manager = new ExecutableMemoryManager<int>();
            const int count = 10_000;

            var first = new nint[count];
            for (var i = 0; i < count; i++)
            {
                var cell = manager.Alloc();
                Assert.True(cell != null);

                cell->value = i;
                Assert.Equal(i, cell->value);
                first[i] = (nint)cell;
            }

            var firstSet = first.ToHashSet();
            Assert.Equal(count, firstSet.Count);

            foreach (var ptr in first)
            {
                manager.Free((ExecutableMemoryManager<int>.Cell*)ptr);
            }

            var second = new nint[count];
            for (var i = 0; i < count; i++)
            {
                var cell = manager.Alloc();
                Assert.True(cell != null);

                cell->value = count + i;
                Assert.Equal(count + i, cell->value);
                second[i] = (nint)cell;
            }

            Assert.Equal(count, second.Distinct().Count());
            Assert.All(second, ptr => Assert.Contains(ptr, firstSet));

            foreach (var ptr in second)
            {
                manager.Free((ExecutableMemoryManager<int>.Cell*)ptr);
            }
        }

        [Fact]
        public void AllocFree_ManyCells_SpansPagesAndReuses()
        {
            var manager = new ExecutableMemoryManager<int>();
            const int count = 50_000;

            var allocated = new HashSet<nint>();
            for (var i = 0; i < count; i++)
            {
                var cell = manager.Alloc();
                Assert.True(cell != null);
                Assert.True(allocated.Add((nint)cell));

                cell->value = i;
            }

            Assert.Equal(count, allocated.Count);

            foreach (var ptr in allocated)
            {
                manager.Free((ExecutableMemoryManager<int>.Cell*)ptr);
            }

            var reused = new HashSet<nint>();
            for (var i = 0; i < count; i++)
            {
                var cell = manager.Alloc();
                Assert.True(cell != null);

                var ptr = (nint)cell;
                Assert.Contains(ptr, allocated);
                Assert.True(reused.Add(ptr));

                cell->value = count + i;
            }

            Assert.Equal(count, reused.Count);

            foreach (var ptr in reused)
            {
                manager.Free((ExecutableMemoryManager<int>.Cell*)ptr);
            }
        }

        [Fact]
        public void AllocFree_Alternating_ReleasesOwnership()
        {
            var manager = new ExecutableMemoryManager<int>();
            const int count = 10_000;

            for (var i = 0; i < count; i++)
            {
                var cell = manager.Alloc();
                Assert.True(cell != null);

                cell->value = i;
                Assert.Equal(i, cell->value);

                manager.Free(cell);
                Assert.Equal(-1, cell->ownerThread);
            }
        }

        [Fact]
        public void AllocFree_Concurrent_AllocatesUniqueCells()
        {
            var manager = new ExecutableMemoryManager<int>();
            const int threadCount = 8;
            const int perThread = 2_000;
            const int total = threadCount * perThread;

            using var start = new ManualResetEventSlim(false);
            using var allAllocated = new CountdownEvent(threadCount);
            var allHeld = new ConcurrentDictionary<nint, byte>();
            var errors = new ConcurrentQueue<Exception>();
            var threads = new Thread[threadCount];

            for (var t = 0; t < threadCount; t++)
            {
                var threadIndex = t;
                threads[t] = new Thread(() =>
                {
                    var cells = new nint[perThread];
                    var allocatedCount = 0;

                    try
                    {
                        start.Wait();

                        for (var i = 0; i < perThread; i++)
                        {
                            var cell = manager.Alloc();
                            if (cell == null)
                            {
                                throw new InvalidOperationException("Alloc returned null.");
                            }

                            var value = (threadIndex * perThread) + i;
                            cell->value = value;
                            if (cell->value != value)
                            {
                                throw new InvalidOperationException("Cell value round-trip failed.");
                            }

                            var ptr = (nint)cell;
                            cells[allocatedCount++] = ptr;

                            if (!allHeld.TryAdd(ptr, 0))
                            {
                                throw new InvalidOperationException(
                                    $"Duplicate cell pointer 0x{ptr:X} while all threads are holding cells.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                    finally
                    {
                        allAllocated.Signal();
                    }

                    // Keep every cell alive until all threads have allocated.
                    // Otherwise a freed cell could legitimately be reused and
                    // would look like a duplicate.
                    allAllocated.Wait();

                    try
                    {
                        for (var i = 0; i < allocatedCount; i++)
                        {
                            manager.Free((ExecutableMemoryManager<int>.Cell*)cells[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Enqueue(ex);
                    }
                })
                {
                    IsBackground = true,
                    Name = $"ExecutableMemoryManagerTest-{t}",
                };

                threads[t].Start();
            }

            start.Set();

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Assert.Empty(errors);
            Assert.Equal(total, allHeld.Count);
        }
    }
}

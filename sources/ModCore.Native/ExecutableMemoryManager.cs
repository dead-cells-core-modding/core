using Hashlink;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Native
{
    internal unsafe class ExecutableMemoryManager<T> where T : unmanaged
    {
        private static readonly int PAGE_SIZE = 4096;
        public struct Cell
        {
            internal volatile int ownerThread;
            public T value;
        }

        private struct Page
        {
            public fixed int usedBits[64];
            public int pageIndex;
            public int groupsCount;
            [InlineArray(32)]
            public struct Group
            {
                public Cell item0;
            }
            [InlineArray(1)]
            public struct GroupCollection
            {
                public Group item0;
            }
            public GroupCollection groups;

            public Span<Group> Groups => MemoryMarshal.CreateSpan(ref groups[0], groupsCount);
        }

        private readonly Lock allocPageLock = new();
        private ImmutableArray<nint> pages = [];

        private void AllocNewPage()
        {
            var pg = (Page*) HashlinkNative.hl_alloc_executable_memory(PAGE_SIZE);

            pg->pageIndex = pages.Length;
            pg->groupsCount = (PAGE_SIZE - sizeof(Page)) / sizeof(Page.Group);

            pages = pages.Add((nint)pg);
            for (int i = 0; i < pg->groupsCount; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    pg->Groups[i][j].ownerThread = -1;
                }
            }
        }

        private void FreeCell( Cell* cell )
        {
            cell->ownerThread = -1;
            Page* page = (Page*)((nint)cell & (PAGE_SIZE - 1));
            int cellIndex = (int)(((nint)cell - (nint)(&page->groups[0])) / sizeof(Cell));
            Interlocked.And(ref page->usedBits[cellIndex / 32], ~(1 << (cellIndex & 31)));
        }
        private Cell* AllocCell()
        {
            _RE_TRY:
            //Fast Path
            var pageCount = pages.Length;
            foreach (var p in pages)
            {
                var pg = (Page*)p;
                var groups = pg->Groups;

                var i = 0;
                for (; i < pg->groupsCount; i++)
                {
                    if ((uint)pg->usedBits[i] != 0xffffffff)
                    {
                        break;
                    }
                }
                if (i == pg->groupsCount)
                {
                    continue;
                }
                ref var group = ref pg->Groups[i];

                for (int j = 0; j < 32; j++)
                {
                    if (Interlocked.CompareExchange(ref group[j].ownerThread, Environment.CurrentManagedThreadId, -1)
                        == -1)
                    {
                        Interlocked.Or(ref pg->usedBits[i], ~(1 << j));
                        return (Cell*)Unsafe.AsPointer(ref group[j]);
                    }
                }
            }

            lock (allocPageLock)
            {
                if (pageCount == pages.Length)
                {
                    AllocNewPage();
                }
            }
            goto _RE_TRY;
        }

        public Cell* Alloc() => AllocCell();
        public void Free(Cell* cell) => FreeCell(cell);
    }
}

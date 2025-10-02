using Hashlink;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using static Hashlink.HashlinkNative;

namespace ModCore.Native
{
    internal unsafe partial class Native
    {
        private HL_gc_pheader*** phl_gc_page_map;
        private HL_gc_mstack* pglobal_mark_stack;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HL_gc_pheader* GC_GET_PAGE( nint ptr )
        {
            var a = ((ulong)(ptr) & 0x0000000FFFFFFFFF) >> (16 + 10);
            var b = (((ulong)(ptr) & 0x0000000FFFFFFFFF) >> 16) & ((1 << 10) - 1);
            return (phl_gc_page_map)[a][b];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GC_IN_PAGE( HL_gc_pheader* page, nint ptr )
        {
            return ptr >= (nint)page->@base && ptr < (nint)(page)->@base + page->page_size;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GC_IS_ALIVE( HL_gc_pheader* page, int bid )
        {
            return (page->bmp[bid >> 3] & (1 << (bid & 7))) == 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GC_SET_ALIVE( HL_gc_pheader* page, int bid )
        {
            //Ensure single-threaded operation
            page->bmp[bid >> 3] |= (byte)(1 << (bid & 7));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GC_PUSH_GEN( HL_gc_mstack* st, nint ptr, HL_gc_pheader* page )
        {
            if (((page->page_kind) & 2) == 2)
            {
                if (st->cur == st->end)
                {
                    hl_gc_mark_grow(st);
                }
                *(st->cur++) = (void*)ptr;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GC_STACK_COUNT( HL_gc_mstack* st )
        {
            return (int)((st)->size - ((nint)(st)->end - (nint)(st)->cur)/ sizeof(nint) - 1);
        }
        private void GcScanManagedRef(Span<nint> roots)
        {
            if (roots.IsEmpty)
            {
                return;
            }

            bool needRemark = false;

            foreach (var ptr in roots)
            {
                if (ptr == 0)
                {
                    continue;
                }
                var page = GC_GET_PAGE(ptr);
                if (page == null || !GC_IN_PAGE(page, ptr))
                {
                    continue;
                }
                var bid = gc_allocator_get_block_id(page, (void*)ptr);
                if (bid >= 0 && (page->page_kind & 2) == 2 && !GC_IS_ALIVE(page, bid))
                {
                    needRemark = true;

                    GC_PUSH_GEN(pglobal_mark_stack, ptr, page);

                    Debug.Assert(GC_STACK_COUNT(pglobal_mark_stack) > 0);
                }
            }

            if (!needRemark)
            {
                return;
            }

            var totalRequest = GC_STACK_COUNT(pglobal_mark_stack);
            Debug.Assert(totalRequest > 0);

            //Remark
            var total = gc_flush_mark(pglobal_mark_stack, true);
            Debug.Assert(totalRequest <= total);

            Debug.Assert(GC_STACK_COUNT(pglobal_mark_stack) == 0);

            for (int i = 0; i < roots.Length; i++)
            {
                var ptr = roots[i];
                if (ptr == 0)
                {
                    continue;
                }
                var page = GC_GET_PAGE(ptr);
                if (page == null || !GC_IN_PAGE(page, ptr))
                {
                    continue;
                }
                var bid = gc_allocator_get_block_id(page, (void*)ptr);
                if (bid >= 0 && !GC_IS_ALIVE(page, bid))
                {
                    roots[i] = 0;
                    GC_SET_ALIVE(page, bid);
                }
            }

            total = total;
        }
    }
}

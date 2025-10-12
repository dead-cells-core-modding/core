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
        private volatile byte* pmark_threads_active;
        private volatile void** pmark_threads_done;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HL_gc_pheader* GC_GET_PAGE( nint ptr )
        {
            return hl_gc_get_page((void*)ptr);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GC_IN_PAGE( HL_gc_pheader* page, nint ptr )
        {
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GC_IS_ALIVE( HL_gc_pheader* page, int bid )
        {
            return (page->bmp[bid >> 3] & (1 << (bid & 7))) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GC_IS_ALIVE(nint ptr )
        {
            var page = GC_GET_PAGE(ptr);
            var bid = gc_allocator_get_block_id(page, (void*)ptr);
            return GC_IS_ALIVE(page, bid);
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
            if (((page->page_kind) & 2) != 2)
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

        private void VerifyGCValidity(ReadOnlySpan<nint> roots)
        {
            foreach (var v in roots)
            {
                var size = hl_gc_get_memsize((void*)v);
                for (int i = 0; i < size / sizeof(nint); i++)
                {
                    var p = ((nint*)v)[i];
                    var page = GC_GET_PAGE(p);
                    if (page == null)
                    {
                        continue;
                    }
                    var bid = gc_allocator_get_block_id(page, (void*)p);
                    if (bid < 0)
                    {
                        continue;
                    }
                    
                    Debug.Assert(GC_IS_ALIVE(page, bid));
                }
            }
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
                if (bid < 0)
                {
                    continue;
                }

                var s = hl_is_gc_ptr((void*)ptr);
                var alive = GC_IS_ALIVE(page, bid);
                Debug.Assert(s == alive);

                if (bid >= 0 && (page->page_kind & 2) != 2 && !GC_IS_ALIVE(page, bid))
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

            Debug.Assert(GC_STACK_COUNT(pglobal_mark_stack) > 0);
            var c = GC_STACK_COUNT(pglobal_mark_stack);

            //Remark
            gc_dispatch_mark(pglobal_mark_stack, true);

            Debug.Assert(GC_STACK_COUNT(pglobal_mark_stack) == 0);

            while (*pmark_threads_active != 0)
            {
                hl_semaphore_acquire(*pmark_threads_done);
            }

#if DEBUG
            VerifyGCValidity(roots);
#endif

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
                if (bid < 0)
                {
                    continue;
                }

                
                if (bid >= 0 && !GC_IS_ALIVE(page, bid))
                {
                    roots[i] = 0;
                    GC_SET_ALIVE(page, bid);
                }
            }
        }

    }
}

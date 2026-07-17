using Hashlink;
using System.Diagnostics;

using static Hashlink.HashlinkNative;

namespace ModCore.Native
{
    internal unsafe partial class Native
    {

        private HL_gc_pheader*** phl_gc_page_map;
        private HL_gc_mstack* pglobal_mark_stack;
        private volatile byte* pmark_threads_active;
        private volatile void** pmark_threads_done;


        private HL_gc_pheader* GC_GET_PAGE( nint ptr )
        {
            return hl_gc_get_page((void*)ptr);
        }

        private bool GC_IN_PAGE( HL_gc_pheader* page, nint ptr )
        {
            return true;
        }

        private bool GC_IS_ALIVE( HL_gc_pheader* page, int bid )
        {
            return (page->bmp[bid >> 3] & (1 << (bid & 7))) != 0;
        }

        private bool GC_IS_ALIVE( nint ptr )
        {
            var page = GC_GET_PAGE(ptr);
            var bid = gc_allocator_get_block_id(page, (void*)ptr);
            return GC_IS_ALIVE(page, bid);
        }

        private void GC_SET_ALIVE( HL_gc_pheader* page, int bid )
        {
            //Ensure single-threaded operation
            page->bmp[bid >> 3] |= (byte)(1 << (bid & 7));
        }

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

        private int GC_STACK_COUNT( HL_gc_mstack* st )
        {
            return (int)((st)->size - ((nint)(st)->end - (nint)(st)->cur) / sizeof(nint) - 1);
        }

        private void VerifyGCValidity( ReadOnlySpan<nint> roots )
        {
            foreach (var v in roots)
            {
                var size = hl_gc_get_memsize((void*)v);
                var pg = GC_GET_PAGE(v);

                if (pg == null || !GC_IS_ALIVE(v))
                {
                    continue;
                }
                if ((pg->page_kind & 2) == 2)
                {
                    continue;
                }
                    
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

        private void GcScanManagedRef( Span<nint> roots )
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

                if (bid >= 0 && (page->page_kind & 2) != 2)
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

#if DEBUG
            //VerifyGCValidity(roots);
#endif

        }

    }
}

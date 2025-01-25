using ModCore;

namespace Hashlink.Trace
{
    public static unsafe class MixTrace
    {
        internal class EdgeTransitionInfo
        {
            public EdgeTransitionInfo? prev;
            public EdgeTransitionInfo? next;
            public nint esp;
            public nint ebp;
        }

        [ThreadStatic]
        internal static EdgeTransitionInfo? current;


        public static void MarkEnteringHL()
        {
            var esp = (nint)Native.mcn_get_esp();
            var ebp = (nint)Native.mcn_get_ebp();
            var cur = current;
            while (cur != null)
            {
                if (cur.esp >= esp)
                {
                    break;
                }
                cur.esp = 0;
                cur.ebp = 0;
                cur = cur.prev;
            }
            cur ??= new();

            if (cur.esp != 0)
            {
                var next = cur.next;
                if (next == null)
                {
                    next = new()
                    {
                        prev = cur,
                    };
                    cur.next = next;
                }
                cur = next;
            }
            cur.esp = esp;

            current = cur;
        }

    }
}

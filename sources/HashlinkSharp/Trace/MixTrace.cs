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
            
        }

    }
}

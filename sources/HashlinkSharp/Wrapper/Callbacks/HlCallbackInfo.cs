using Hashlink.UnsafeUtilities;

#nullable disable

namespace Hashlink.Wrapper.Callbacks
{
    internal class HlCallbackInfo
    {
        public nint directRoute;
        public DelegateInfo entry;
        public HlCallback callback;
    }
}

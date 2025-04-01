using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Wrapper.Callbacks
{
    public class HlCallback
    {
        private readonly nint routerPtr;
        private readonly Delegate callback;
        private readonly HlCallbackInfo info;
        internal HlCallback(Delegate callback, HlCallbackInfo info)
        {
            this.callback = callback;
            this.info = info;
            routerPtr = Marshal.GetFunctionPointerForDelegate(callback);
        }

        public nint RedirectTarget
        {
            get => info.directRoute;
            set => info.directRoute = value;
        }

        public Delegate? Target
        {
            get => info.entry?.self;
            set => info.entry = new(value);
        }
        public nint NativePointer => routerPtr;
        public Delegate CallbackDelegate => callback;
    }
}

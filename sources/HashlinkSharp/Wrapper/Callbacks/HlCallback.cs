using Hashlink.UnsafeUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Wrapper.Callbacks
{
    public class HlCallback
    {
        private nint routerPtr;
        private Delegate? callback;
        private readonly MethodInfo callbackMI;
        private readonly HlCallbackInfo info = new();
        internal HlCallback(MethodInfo callbackMI)
        {
            this.callbackMI = callbackMI;
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
        public nint NativePointer
        {
            get
            {
                if (routerPtr == 0)
                {
                    callback = callbackMI.CreateAnonymousDelegate(info, true);
                    routerPtr = Marshal.GetFunctionPointerForDelegate(callback);
                }
                return routerPtr;
            }
        }
    }
}

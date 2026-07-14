using Hashlink.Proxy;

namespace Hashlink.Marshaling.ObjHandle
{
    public class HashlinkObjHandle
    {
        private HashlinkObj? obj;
        private bool isStateful = true;

        internal int handleIndex = 0;
        internal readonly nint nativeHLPtr;
        internal bool dontCollect;
        internal HashlinkObjHandle( nint objPtr, int index )
        {
            nativeHLPtr = objPtr;
            handleIndex = index;
        }

        public bool IsStateful {
            get => isStateful;
            set {
                if (isStateful != value)
                {
                    if (isStateful)
                    {
                        throw new InvalidOperationException();
                    }
                    isStateful = value;
                    _ = HashlinkObjManager.GetHandle(nativeHLPtr);
                }
            }
        }
        public HashlinkObj? Target {
            get => obj;
            set {
                obj = value;
            }
        }
        public void SetGCTrap()
        {
            dontCollect = true;
        }
    }
}

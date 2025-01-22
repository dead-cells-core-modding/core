using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy
{
    public unsafe abstract class HashlinkTypedObj<T>(void* objPtr) : HashlinkObj(objPtr) where T : unmanaged
    {
        public T* TypedRef => (T*)HashlinkPointer;
    }
}

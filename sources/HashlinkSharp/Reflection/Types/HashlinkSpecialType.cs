using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Types
{
    public unsafe class HashlinkSpecialType<T>( HashlinkModule module, HL_type* type) : HashlinkType(module, type)
        where T : unmanaged
    {
        public T* TypeData => (T*)NativeType->data.obj;
    }
}

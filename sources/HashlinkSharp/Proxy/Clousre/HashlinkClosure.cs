using ModCore.Hashlink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Clousre
{
    public unsafe class HashlinkClosure(HashlinkObjPtr objPtr) : HashlinkTypedObj<HL_vclosure>(objPtr)
    {
        private HashlinkFunc? cachedFunc;

        public HashlinkClosure(HL_type* funcType, void* funcPtr, void* self) : 
            this(HashlinkObjPtr.GetUnsafe(hl_alloc_closure_ptr(funcType, funcPtr, self)))
        {

        }

        public HashlinkFunc Function
        {
            get
            {
                
                cachedFunc ??= new(TypedRef->type->data.func, TypedRef->fun)
                {
                    BindingThis = TypedRef->hasValue > 0 ? (nint) TypedRef->value : null
                };
                return cachedFunc;
            }
        }

        public nint? BindingThis
        {
            get
            {
                return TypedRef->hasValue > 0 ? (nint)TypedRef->value : null;
            }
            set
            {
                TypedRef->hasValue = value is null ? 0 : 1;
                if(value is not null)
                {
                    TypedRef->value = (void*) value.Value;
                }
                if(cachedFunc != null)
                {
                    cachedFunc.BindingThis = value;
                }
            }
        }
    }
}

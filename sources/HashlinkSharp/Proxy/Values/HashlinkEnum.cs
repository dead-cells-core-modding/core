using Hashlink.Marshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkEnum(HashlinkObjPtr objPtr) : HashlinkTypedObj<HL_enum>(objPtr)
    {
        public HashlinkEnum( HL_type* type, int index ) : this(HashlinkObjPtr.GetUnsafe(hl_alloc_enum(type, index)))
        {
            
        }
        public HL_type_enum* EnumType => NativeType->data.tenum;
        public HL_enum_construct* CurrentConstruct => EnumType->constructs + Index;

        public int ParamCount => CurrentConstruct->nparams;

        public byte* ParamsData => (byte*)(TypedRef + 1);

        public object? this[int paramId]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(paramId, ParamCount);
                return HashlinkMarshal.ReadData(ParamsData + CurrentConstruct->offsets[paramId],
                    CurrentConstruct->@params[paramId]->kind);
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(paramId, ParamCount);
                HashlinkMarshal.WriteData(ParamsData + CurrentConstruct->offsets[paramId],
                    value,
                    CurrentConstruct->@params[paramId]->kind);
            }
        }
        public int Index
        {
            get => TypedRef->index;
        }
    }
}

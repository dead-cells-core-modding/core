using Hashlink.Marshaling;
using Hashlink.Reflection.Members.Enum;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkEnum(HashlinkObjPtr objPtr) : HashlinkTypedObj<HL_enum>(objPtr)
    {
        public HashlinkEnum( HashlinkEnumType type, int index ) : 
            this(HashlinkObjPtr.GetUnsafe(hl_alloc_enum(type.NativeType, index)))
        {
            
        }
        public HashlinkEnumType EnumType => (HashlinkEnumType)Type;
        public HashlinkEnumConstruct CurrentConstruct => EnumType.Constructs[Index];

        public byte* ParamsData => (byte*)(TypedRef + 1);

        public object? this[int paramId]
        {
            get
            {
                CheckValidity();
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(paramId, CurrentConstruct.ParamsCount);
                return HashlinkMarshal.ReadData(ParamsData + CurrentConstruct.ParamOffsets[paramId],
                     CurrentConstruct.Params[paramId]);
            }
            set
            {
                CheckValidity();
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(paramId, CurrentConstruct.ParamsCount);
                HashlinkMarshal.WriteData(ParamsData + CurrentConstruct.ParamOffsets[paramId],
                    value,
                    CurrentConstruct.Params[paramId]);
            }
        }
        public int Index
        {
            get => TypedRef->index;
        }
    }
}

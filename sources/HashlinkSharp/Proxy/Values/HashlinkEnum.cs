using Hashlink.Marshaling;
using Hashlink.Reflection.Members.Enum;
using Hashlink.Reflection.Types;
using System.Diagnostics;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkEnum( HashlinkObjPtr objPtr ) : HashlinkTypedObj<HL_enum>(objPtr)
    {
        public HashlinkEnum( HashlinkEnumType type, int index ) :
            this(HashlinkObjPtr.Get(hl_alloc_enum(EnsureThreadRegistered(type.NativeType), index)))
        {
            Debug.Assert(Handle != null);
        }
        public HashlinkEnumType EnumType => (HashlinkEnumType)Type;
        public HashlinkEnumConstruct CurrentConstruct => EnumType.Constructs[Index];

        public byte* ParamsData => (byte*)(TypedRef);

        public object? this[int paramId] {
            get {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(paramId, CurrentConstruct.ParamsCount);
                return HashlinkMarshal.ReadData(ParamsData + CurrentConstruct.ParamOffsets[paramId],
                     CurrentConstruct.Params[paramId]);
            }
            set {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(paramId, CurrentConstruct.ParamsCount);
                HashlinkMarshal.WriteData(ParamsData + CurrentConstruct.ParamOffsets[paramId],
                    value,
                    CurrentConstruct.Params[paramId]);
            }
        }
        public int Index {
            get => TypedRef->index;
        }
    }
}

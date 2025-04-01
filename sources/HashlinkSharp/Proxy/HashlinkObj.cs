using Hashlink.Marshaling;
using Hashlink.Reflection.Types;

namespace Hashlink.Proxy
{
    public abstract unsafe class HashlinkObj : IHashlinkPointer, IExtendData
    {
        private object? extendData;
        private class ManyExtendData
        {
            public List<object> data = [];
        }

        public HashlinkObj( HashlinkObjPtr objPtr )
        {
            var ptr = objPtr.Pointer;
            Handle = HashlinkObjHandle.GetHandle((void*)ptr);
            if (Handle != null)
            {
                Handle.Target = this;
            }
            if (!HashlinkMarshal.IsHashlinkObject((void*)ptr))
            {
                throw new InvalidOperationException();
            }
            HashlinkPointer = ptr;
            NativeType = *(HL_type**)ptr;
            Type = HashlinkMarshal.Module.GetMemberFrom<HashlinkType>(NativeType);
            TypeKind = NativeType->kind;
        }
        public override string? ToString()
        {
            return new(hl_to_string((HL_vdynamic*)HashlinkPointer));
        }

        void IExtendData.AddData( object data )
        {
            if (extendData == null)
            {
                extendData = data;
                return;
            }

            if (extendData is not ManyExtendData med)
            {
                med = new();
                med.data.Add(extendData);
                extendData = med;
            }

            med.data.Add(data);
        }

        T IExtendData.GetData<T>()
        {
            if (this is T)
            {
                return (T)(object)this;
            }
            if (extendData is not ManyExtendData med)
            {
                return (T)extendData! ?? throw new InvalidCastException();
            }
            return med.data.OfType<T>().First();
        }

        public HashlinkObjHandle? Handle
        {
            get;
        }
        public TypeKind TypeKind
        {
            get;
        }
        public HashlinkType Type
        {
            get;
        }
        public HL_type* NativeType
        {
            get;
        }
        public nint HashlinkPointer
        {
            get;
        }
    }
}

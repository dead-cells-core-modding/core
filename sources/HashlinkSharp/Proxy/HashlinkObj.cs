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

        internal void SetDestroyed()
        {
            if (IsDestroyed)
            {
                return;
            }
            IsDestroyed = true;
            Handle = null;
            HashlinkPointer = -1;
            Type = null!;
            NativeType = null;
            extendData = null!;
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
        }
        public override string? ToString()
        {
            CheckValidity();
            return new(hl_to_string((HL_vdynamic*)HashlinkPointer));
        }
        internal protected void CheckValidity()
        {
            if (IsDestroyed)
            {
                throw new ObjectDisposedException(GetType().FullName, 
                    "The address pointed to by this object is duplicated on the Hashlink side, which should not happen.");
            }
        }
        void IExtendData.AddData( object data )
        {
            CheckValidity();
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
            CheckValidity();
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
            get; private set;
        }
        public TypeKind TypeKind => Type.TypeKind;
        public HashlinkType Type
        {
            get; private set;
        }
        public HL_type* NativeType
        {
            get; private set;
        }
        public nint HashlinkPointer
        {
            get; private set;
        }
        public bool IsDestroyed
        {
            get; private set;
        } = false;
    }
}

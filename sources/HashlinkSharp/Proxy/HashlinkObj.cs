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

            HashlinkPointer = ptr;
            NativeType = *(HL_type**)ptr;
            Type = HashlinkMarshal.Module.GetMemberFrom<HashlinkType>(NativeType);
        }
        public override string? ToString()
        {
            return new(hl_to_string((HL_vdynamic*)HashlinkPointer));
        }

        T IExtendData.GetData<T>()
        {
            if (this is T)
            {
                return (T)(object)this;
            }
            if (extendData is T t)
            {
                return t;
            }
            T? result = null;
            if (extendData is ManyExtendData med)
            {
                result = med.data.OfType<T>().FirstOrDefault();
            }

            if (result == null)
            {
                lock (this)
                {
                    result = (T)T.Create(this);

                    if (extendData == null)
                    {
                        extendData = result;
                    }
                    else
                    {
                        if (extendData is not ManyExtendData med2)
                        {
                            med2 = new();
                            med2.data.Add(extendData);
                            extendData = med2;
                        }

                        med2.data.Add(result);
                    }
                }
            }
            return result;
        }

        public HashlinkObjHandle? Handle
        {
            get; 
        }
        public TypeKind TypeKind => Type.TypeKind;
        public HashlinkType Type
        {
            get; 
        }
        public HL_type* NativeType
        {
            get; 
        }
        public virtual nint HashlinkPointer
        {
            get; 
        }
    }
}

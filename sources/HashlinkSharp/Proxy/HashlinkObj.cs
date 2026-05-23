using Hashlink.Marshaling;
using Hashlink.Marshaling.ObjHandle;
using Hashlink.Reflection.Types;
using System.Diagnostics.CodeAnalysis;

namespace Hashlink.Proxy
{
    public abstract unsafe partial class HashlinkObj :
        IHashlinkPointer
    {
        //They have no special meaning; they are simply there to execute `HashlinkMarshal.EnsureThreadRegistered` in the constructor.
        protected static HL_type* EnsureThreadRegistered( HL_type* obj )
        {
            HashlinkMarshal.EnsureThreadRegistered();
            return obj;
        }
        protected static T EnsureThreadRegistered<T>( Func<T> factory )
        {
            HashlinkMarshal.EnsureThreadRegistered();
            return factory();
        }

        [MemberNotNull(nameof(nativeType))]
        [MemberNotNull(nameof(type))]
        internal void RefreshTypeInfo( HL_type* ptr, bool clearExtraData )
        {
            *(nint*)HashlinkPointer = (nint)ptr;
            nativeType = ptr;
            type = HashlinkMarshal.Module.GetMemberFrom<HashlinkType>(nativeType);
            isChangedTypeInfo = true;
            if (clearExtraData)
            {
                ClearExtraData();
            }
        }

        internal bool isChangedTypeInfo = false;

        public HashlinkObj( HashlinkObjPtr objPtr )
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ptr = objPtr.Pointer;
            Handle = HashlinkObjManager.GetHandle(ptr);
            if (Handle != null)
            {
                Handle.Target = this;
            }

            HashlinkPointer = ptr;
            nativeType = *(HL_type**)ptr;
            type = HashlinkMarshal.Module.GetMemberFrom<HashlinkType>(nativeType);
        }
        public override string ToString()
        {
            return new string(hl_to_string((HL_vdynamic*)HashlinkPointer)) ?? Type.Name ?? "";
        }

        public void MarkStateful()
        {
            ArgumentNullException.ThrowIfNull(Handle, nameof(Handle));
            Handle.IsStateless = false;
        }


        public HashlinkObjHandle? Handle {
            get; private set;
        }
        public TypeKind TypeKind => Type.TypeKind;
        public bool IsValid => nativeType != null && type != null &&
            HashlinkPointer != 0 && HashlinkObjPtr.Get(HashlinkPointer).GetMemSize() > 0;
        public void Detach()
        {
            nativeType = null;
            type = null;
            if (Handle != null)
            {
                Handle.Target = null;
                Handle = null;
            }
            HashlinkPointer = 0;

        }
        private HL_type* nativeType;
        private HashlinkType? type;

        public HashlinkType Type => type!;
        public HL_type* NativeType => nativeType;
        public virtual nint HashlinkPointer {
            get; private set;
        }
    }
}
